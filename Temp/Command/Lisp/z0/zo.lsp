(vl-load-com)
(setq acad_application (vlax-get-acad-object))
(setq active_document (vla-get-ActiveDocument acad_application))
(setq model_space (vla-get-ModelSpace active_document))

;----------------Программа для вывода объектов на ноль------------------
(defun c:z0(/ kol ss ssl n a sp ep obj temp nab ename elist etype pr item1 pr_arx ListArx pr)
;|(defun *error* (msg)
      ;(setq obj (vlax-ename->vla-object (ssname KOL n)))
      ;(vla-put-Height obj 50000)
      ;(princ (cdr (assoc 0 (entget (ssname KOL n)))))
      ;(princ (ssname KOL n))
      (alert "                Dikkat!!! \nProgram Hata!")
);defun *error* 
|; 
  	 
(defun usc1->usc2 (point nor) ; point-точка(x,y,z), nor-нормаль.
(Vlax-safearray->list (vlax-variant-value (vla-translatecoordinates (vla-get-utility active_document)(vlax-3d-point point)
4 0 :vlax-false nor))));defun usc1->usc2

(defun urlw (obj / t1 t2 ss i)
(setq i 0)
(setq ss (length (Vlax-safearray->list (vlax-variant-value (vla-get-coordinates obj)))))
(setq t1 (length (Vlax-safearray->list (vlax-variant-value (vla-get-coordinate obj 0)))))
(setq ss (/ ss t1))
(while (< i ss)
(setq t1 (Vlax-safearray->list (vlax-variant-value (vla-get-coordinate obj i))))
(setq t2 (usc1->usc2 (list (car t1) (cadr t1) (vla-get-Elevation obj)) (vla-get-Normal obj)))
(vla-put-coordinate obj i (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 1)) (list (car t2) (cadr t2) )))
(setq i (1+ i))
);while
(vla-put-Normal obj (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 2)) '(0.0 0.0 1.0)))
(vla-put-Elevation obj 0.0)
(vla-update obj)
(setq kol (1+ kol))
);defun

(defun ursol (obj / t1 t2 ss i)
(setq i 0)
(setq ss (length (Vlax-safearray->list (vlax-variant-value (vla-get-coordinates obj)))))
(setq t1 (length (Vlax-safearray->list (vlax-variant-value (vla-get-coordinate obj 0)))))
(setq ss (/ ss t1))
(while (< i ss)
(setq t1 (Vlax-safearray->list (vlax-variant-value (vla-get-coordinate obj i))))
(setq t2 (usc1->usc2 t1 (vla-get-Normal obj)))
(if (not (equal (caddr t2) 0.0)) (progn
(vla-put-coordinate obj i (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 2)) (list (car t2) (cadr t2) 0.0)))
(vla-update obj)
(setq pr t));progn
);if
(setq i (1+ i))
);while
(if pr (setq kol (1+ kol)))
);defun

   (setq ss (ssget '((-4 . "<or")
		     (0 . "LINE")
		     (0 . "LWPOLYLINE")
		     (0 . "HATCH")
		     (0 . "CIRCLE")
		     (0 . "ELLIPSE")
		     (0 . "TEXT")
		     (0 . "MTEXT")
		     (0 . "INSERT")
		     (0 . "ARC")
		     (0 . "POINT")
		     (0 . "SOLID")
		     (0 . "POLYLINE")
		     (0 . "DIMENSION")
	             (0 . "LEADER")
	             (0 . "TOLERANCE")
		     (0 . "SPLINE") (-4 . "or>"))))
  
   (setq nab (ssadd))
   (setq ssl (sslength ss))
   (setq n 0 kol 0)

   (setq pr_arx nil)
   (setq ListArx (mapcar 'strcase (arx)))
   (if (or (member "DOSLIB17.ARX" ListArx)
	   (member "DOSLIB18.ARX" ListArx)
           (member "DOSLIB17x64.ARX" ListArx)
	   (member "DOSLIB18x64.ARX" ListArx))
   (setq pr_arx T));if
  
   (if (and (> ssl 50) pr_arx)
   (dos_getprogress "Kontrol..."  "Lutfen, bekleyin..." ssl));if

   (WHILE (< n ssl)
   (if (and (> ssl 50) pr_arx) (dos_getprogress -1))

   (setq ename (ssname ss n)	     ;имя объекта
	 elist (entget ename)	     ;его код
	 etype (cdr (assoc 0 elist)));его тип
      
(COND
   ((equal etype "LINE") 
   (setq obj (vlax-ename->vla-object ename))
   (setq sp (Vlax-safearray->list (vlax-variant-value (vla-get-StartPoint obj))))
   (setq ep (Vlax-safearray->list (vlax-variant-value (vla-get-EndPoint obj))))
   (setq temp (Vlax-safearray->list (vlax-variant-value (vla-get-normal obj))))
   (if (and
	 (and (>= (- (abs (car temp)) 0.0) 0.0) (<= (- (abs (car temp)) 0.0) 0.0001))
	 (and (>= (- (abs (cadr temp)) 0.0) 0.0) (<= (- (abs (cadr temp)) 0.0) 0.0001))
	 (and (>= (- (abs (caddr temp)) 0.0) 0.0) (<= (- (abs (caddr temp)) 1.0) 0.0001))
	 );and
     (PROGN
   (if (not (and (= (caddr sp) 0.0) (= (caddr ep) 0.0))) (progn									   
   (setq sp (vlax-3d-point (list (car sp) (cadr sp) 0.0)))
   (setq ep (vlax-3d-point (list (car ep) (cadr ep) 0.0)))
   (vla-put-StartPoint obj sp)
   (vla-put-EndPoint obj ep)
   (vla-update obj)
   (setq kol (1+ kol))
   ));progn,if
   );PROGN
   (PROGN
   ;(setq sp (usc1->usc2 sp (vla-get-Normal obj)))
   ;(setq ep (usc1->usc2 ep (vla-get-Normal obj))) 
   ;(vla-put-Normal obj (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 2)) '(0.0 0.0 1.0)))
   ;(vla-put-StartPoint obj (vlax-3d-point (list (car sp) (cadr sp) 0.0)))
   ;(vla-put-EndPoint obj (vlax-3d-point (list (car ep) (cadr ep) 0.0)))
     (ssadd (ssname KOL n) nab)
   ));PROGN,IF
   ;(princ)
   );;;;;;;;;(0 . "LINE")


   ((or (equal etype "LWPOLYLINE")
	(equal etype "POLYLINE"))
   (setq obj (vlax-ename->vla-object ename))
   (setq temp (Vlax-safearray->list (vlax-variant-value (vla-get-normal obj))))
   (if (and
	 (and (>= (- (abs (car temp)) 0.0) 0.0) (<= (- (abs (car temp)) 0.0) 0.0001))
	 (and (>= (- (abs (cadr temp)) 0.0) 0.0) (<= (- (abs (cadr temp)) 0.0) 0.0001))
	 (and (>= (- (abs (caddr temp)) 0.0) 0.0) (<= (- (abs (caddr temp)) 1.0) 0.0001))
	 );and 
   (if (not (equal (vla-get-Elevation obj) 0.0)) (progn								     
   (vla-put-Elevation obj 0.0)
   (setq kol (1+ kol))
   (vla-update obj)
   ));progn,if
   (urlw obj));if
   );;;;;;;;(0 . "LWPOLYLINE")

   ((equal etype "SOLID")
   (setq obj (vlax-ename->vla-object ename))
   (ursol obj)
   );;;;;;;;(0 . "SOLID")
   
   ((or (equal etype "DIMENSION")
	(equal etype "LEADER")
	(equal etype "TOLERANCE"))
	(setq pr nil)
	  (foreach item	elist
	    (if	(equal (car item) 10)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(10) (trans (cdr item) 0 1)))
		    (setq newlist (append '(10) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t)))))
	    (if	(equal (car item) 11)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(11) (trans (cdr item) 0 1)))
		    (setq newlist (append '(11) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t)))))
	    (if	(equal (car item) 13)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(13) (trans (cdr item) 0 1)))
		    (setq newlist (append '(13) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t)))))
	    (if	(equal (car item) 14)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(14) (trans (cdr item) 0 1)))
		    (setq newlist (append '(14) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t)))))
	    (if	(equal (car item) 15)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(15) (trans (cdr item) 0 1)))
		    (setq newlist (append '(15) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t)))))
	    (if	(equal (car item) 16)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(16) (trans (cdr item) 0 1)))
		    (setq newlist (append '(16) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t))))));foreach
    (if pr (setq kol (1+ kol)))
	  (entmod elist)
	  (setq pr nil)
   );;;;;;;;(0 . "LEADER") (0 . "DIMENSION") (0 . "TOLERANCE")

   ((equal etype "HATCH")
   (setq obj (vlax-ename->vla-object ename))
   (setq temp (Vlax-safearray->list (vlax-variant-value (vla-get-normal obj))))
   (if (and
	 (and (>= (- (abs (car temp)) 0.0) 0.0) (<= (- (abs (car temp)) 0.0) 0.0001))
	 (and (>= (- (abs (cadr temp)) 0.0) 0.0) (<= (- (abs (cadr temp)) 0.0) 0.0001))
	 (and (>= (- (abs (caddr temp)) 0.0) 0.0) (<= (- (abs (caddr temp)) 1.0) 0.0001))
	 );and
     (progn
     (if (not (equal (vla-get-Elevation obj) 0.0))
       (progn
   (vla-put-Elevation obj 0.0)
   (vla-update obj)
   (setq kol (1+ kol))));progn,if
     );progn
   (ssadd ename nab));if
   (princ)
   );;;;;;;;;(0 . "HATCH")


   ((equal etype "POINT")
   (setq obj (vlax-ename->vla-object ename))
   (setq sp (Vlax-safearray->list (vlax-variant-value (vla-get-Coordinates obj))))
   (setq temp (Vlax-safearray->list (vlax-variant-value (vla-get-normal obj))))
   (if (and
	 (and (>= (- (abs (car temp)) 0.0) 0.0) (<= (- (abs (car temp)) 0.0) 0.0001))
	 (and (>= (- (abs (cadr temp)) 0.0) 0.0) (<= (- (abs (cadr temp)) 0.0) 0.0001))
	 (and (>= (- (abs (caddr temp)) 0.0) 0.0) (<= (- (abs (caddr temp)) 1.0) 0.0001))
	 );and
   (if (not (equal (caddr sp) 0.0)) (progn
     (vla-put-Coordinates obj (vlax-3d-point (list (car sp) (cadr sp) 0.0)))
     (setq kol (1+ kol))
     ));if
     
   (PROGN
   ;(setq sp (usc1->usc2 sp (vla-get-Normal obj))) 
   ;(vla-put-Normal obj (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 2)) '(0.0 0.0 1.0)))
   ;(vla-put-Coordinates obj (vlax-3d-point sp))
   (ssadd ename nab)  
   ));PROGN, IF
   (princ)
   );;;;;;;;;;;(0 . "POINT")

   ((or (equal etype "CIRCLE")
	(equal etype "ARC")
	(equal etype "ELLIPSE"))
   (setq obj (vlax-ename->vla-object ename))
   (setq sp (Vlax-safearray->list (vlax-variant-value (vla-get-Center obj))))
   (setq temp (Vlax-safearray->list (vlax-variant-value (vla-get-normal obj))))
   (if (and
	 (and (>= (- (abs (car temp)) 0.0) 0.0) (<= (- (abs (car temp)) 0.0) 0.0001))
	 (and (>= (- (abs (cadr temp)) 0.0) 0.0) (<= (- (abs (cadr temp)) 0.0) 0.0001))
	 (and (>= (- (abs (caddr temp)) 0.0) 0.0) (<= (- (abs (caddr temp)) 1.0) 0.0001))
	 );and
   (if (not (equal (caddr sp) 0.0)) (progn
     (vla-put-Center obj (vlax-3d-point (list (car sp) (cadr sp) 0.0)))
     (setq kol (1+ kol))
     ));progn,if									   
   (PROGN
   ;(setq sp (usc1->usc2 sp (vla-get-Normal obj))) 
   ;(vla-put-Normal obj (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 2)) '(0.0 0.0 1.0)))
   ;(vla-put-Center obj (vlax-3d-point sp))
   (ssadd ename nab)
   ));PROGN, IF
   (princ)
   );;;;;;;;;;;(0 . "CIRCLE")(0 . "ARC")


   ((or (equal etype "TEXT")
	(equal etype "MTEXT")
	(equal etype "INSERT")) 
   (setq obj (vlax-ename->vla-object ename))

    (setq sp (Vlax-safearray->list (vlax-variant-value (vla-get-InsertionPoint obj))))
   (setq temp (Vlax-safearray->list (vlax-variant-value (vla-get-normal obj))))
   (if (and
	 (and (>= (- (abs (car temp)) 0.0) 0.0) (<= (- (abs (car temp)) 0.0) 0.001))
	 (and (>= (- (abs (cadr temp)) 0.0) 0.0) (<= (- (abs (cadr temp)) 0.0) 0.001))
	 (and (>= (- (abs (caddr temp)) 0.0) 0.0) (<= (- (abs (caddr temp)) 1.0) 0.001))
	 );and
   (if (not (equal (caddr sp) 0.0))(progn
     (vla-put-InsertionPoint obj (vlax-3d-point (list (car sp) (cadr sp) 0.0)))
     (setq kol (1+ kol))
     ));progn,if									   
   (PROGN
   ;(setq sp (usc1->usc2 sp (vla-get-Normal obj)))
   ;(vla-put-Normal obj (vlax-safearray-fill (vlax-make-safearray vlax-vbDouble '(0 . 2)) '(0.0 0.0 1.0)))
   ;(vla-put-InsertionPoint obj (vlax-3d-point (list (car sp) (cadr sp) 0.0)))
   (ssadd ename nab)
   ));PROGN, IF
   (princ) 
   );;;;;;;;;;;(0 . "TEXT")(0 . "MTEXT")(0 . "INSERT")

   ((equal etype "SPLINE")
   	  (setq pr nil)
	  (foreach item	elist
	    (if	(equal (car item) 10)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(10) (trans (cdr item) 0 1)))
		    (setq newlist (append '(10) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t)))))
	    (if	(equal (car item) 11)
	      (progn
		(if (not (equal (caddr (trans (cdr item) 0 1)) 0.0))
		  (progn
		    (setq item1 (append '(11) (trans (cdr item) 0 1)))
		    (setq newlist (append '(11) (cdr (subst 0.0 (cadddr item1) item1))))
		    (setq elist (subst newlist item elist))
		    (setq pr t))))));foreach
	    (if pr (setq kol (1+ kol)))
	  (entmod elist)
	  (setq pr nil)
   );;;;;;;;;;;;;;(0 . "SPLINE")
   (t nil)   
);COND
(setq n (1+ n))
(if (not pr_arx) (princ (strcat "\nIslem yapilan <" (rtos n 2 0) "> Kactanesinde islem yapilacak "(rtos ssl 2 0))))
);WHILE

 (if (and (> ssl 50) pr_arx)
      (dos_getprogress t))
  
;(sssetfirst nil KOL)
(cond
((not (equal (sslength nab) 0)) 
(alert (strcat "                                        Dikkat!!!"
	       "\nZ0 koordinati cevrilmedi < "
	       (itoa (sslength nab)) " > Hedef!!!\n                               Elle duzeltmeyi deneyin\n"
	       "\n                          Degistirildi < " (itoa kol)" > Hedef!!!"))
(sssetfirst nil nab))
((equal (sslength nab) 0) 
(alert (strcat "                Dikkat!!!"
	       "\nDegistirildi < " (itoa kol)" > Hedef!!!")))

(t nil)
);cond
(princ)
);defun z0

;-------------------------программа для конвертации сплайна в полилинию----------------
(Defun c:z01 (/ ss pt# cmdecho osmode clayer count ent lay lng pt-list cnt)
   (setq ss	(ssget '((0 . "spline")))
	pt#	(getint "Enter number of segments <100>:")
	cmdecho	(getvar "cmdecho")
	osmode	(getvar "osmode")
	clayer	(getvar "clayer")
	count 	0					;spline counter
  );end setq
  (if(null pt#)(setq pt# 100))
  (setvar "cmdecho" 0)
  (command "_.undo" "_begin")				;begin undo group
  (setvar "osmode" 0)
  (repeat(sslength ss)					;repeat for each spline
    (setq ent	(vlax-ename->vla-object (ssname ss count));change spline to vla-object
	  lay	(vlax-get-property ent "layer")		;spline's layer
	  lng	(vlax-curve-getDistAtPoint ent(vlax-curve-getEndPoint ent));length of spline
	  pt-list(list(vlax-curve-getStartPoint ent))	;coords for start of spline
	  cnt 	1.0					;segment counter
    );end setq
    (repeat pt#						;repeat for each segment
      (setq pt-list(cons(vlax-curve-getPointAtDist ent (* lng(/ cnt pt#)))pt-list));add segment's point to pt-list
      (setq cnt(1+ cnt))				;counter to next segment
    );end segment repeat
    (setq cnt 0)					;pline counter
    (setvar "clayer" lay)				;match spline's layer
    (command "_.pline"					;start "pline" command
	     (repeat(length pt-list)			;repeat for each point
	       (command(nth cnt pt-list))		;enter current point
	       (setq cnt(1+ cnt))			;counter to next point
	       ""					;return value to close "pline" command
	     );end point repeat
    );end command
    (setq count(1+ count))				;counter to next spline
  );end spline repeat
  (command "_.erase" ss "")
  (setvar "osmode" osmode)
  (setvar "clayer" clayer)
  (command "_.undo" "_end")				;end of undo group
  (setvar "cmdecho" cmdecho)
  (princ)						;exit quietly
);end C:S2P
