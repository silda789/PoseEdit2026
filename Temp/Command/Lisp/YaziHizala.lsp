(vl-load-com)
(setq acad_application (vlax-get-acad-object))
(setq active_document (vla-get-ActiveDocument acad_application))
(setq model_space (vla-get-ModelSpace active_document))
;*-*-*-***-*-*-*----*--*--*-*-*-*-*-*****--*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
(defun cher->polili (lis lay )
		(setq temp (vla-AddPolyline model_space (vlax-safearray-fill 
                (vlax-make-safearray vlax-vbDouble (cons 0 (1- (vl-list-length lis)))) lis)))
                (vla-put-Layer temp lay)
        );defun cher->polili

(defun cher->lin-raz (x1 x2 k lay / loca x tep tep1 kol i)
  (setq loca '(7 7.5 5.5 300))
  (setq loca (mapcar '(lambda (x) (* x k)) loca))
(defun local (tv / tep)
(foreach p (list (setq x (polar tv (angle x2 x1) (* 0.5 (nth 1 loca))))
        (polar x (+ (angle x2 x1) (* 110 (/ pi 180))) (nth 2 loca))(polar (setq x (polar tv (angle x1 x2) (* 0.5 (nth 1 loca)))) (+ (angle x1 x2) (* 110 (/ pi 180))) (nth 2 loca)) x)
        (setq tep (append tep p)))
);defun local  

(setq kol (atoi (rtos (/ (distance x1 x2) (nth 3 loca)) 2 0))) 
      (setq i 1)

(if (and (/= kol 0) (/= kol 1) (/= kol 2))
(while (< i kol)
(setq tep1 (append tep1 (local (polar x1 (angle x1 x2) (* (/ (distance x1 x2) kol) i)))))
(setq i (1+ i))
);while
(setq tep1 (local (polar x1 (angle x1 x2) (* 0.5 (distance x1 x2)))))
);if
;(if (>= (distance x1 x2) (nth 3 loca))
;(foreach p (list (polar x1 (angle x1 x2) (* 0.25 (distance x1 x2))) (polar x2 (angle x2 x1) (* 0.25 (distance x1 x2))))
;(setq tep1 (append tep1 (local p))));foreach
;(setq tep1 (local (polar x1 (angle x1 x2) (* 0.5 (distance x1 x2)))))
;);if
(cher->polili (append (polar x1 (angle x2 x1) (nth 0 loca)) tep1
         (polar x2 (angle x1 x2) (nth 0 loca)))
	 lay)
  );defun cher->lin-raz
;*-*-*-*-*-*-**-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*




;////////*******************************Выравнивание текста *************************************
(defun c:vt(/ bt1 bt2 bt t1 t2 x y ss n)
(setq ss (ssget (list '(-4 . "<OR")  '(0 . "TEXT") '(0 . "MTEXT") '(0 . "INSERT") '(-4 . "OR>"))))
(setq bt1 (setq bt (getpoint)) bt2 (getpoint bt) n 0)
(setq bt (polar bt1 (angle bt1 bt2) (* 0.5 (distance bt1 bt2))))
(while (< n (sslength ss))  
(setq obj (vlax-ename->vla-object (ssname ss n)))
(vla-GetBoundingBox obj 't1 't2)
(setq t1 (vlax-safearray->list t1))
(setq t2 (vlax-safearray->list t2))
(setq x (* 0.5 (- (car t2) (car t1))))
(setq y (* 0.5 (- (cadr t2) (cadr t1))))
(setq x (+ (car t1) x)) 
(setq y (+ (cadr t1) y))
(if (or (and (<= (angle bt1 bt2) 0.78) (>= (angle bt1 bt2) 0.0))
        (and (>= (angle bt1 bt2) 2.36) (<= (angle bt1 bt2) pi))
        (and (>= (angle bt1 bt2) pi) (<= (angle bt1 bt2) 3.93))
	(and (>= (angle bt1 bt2) 5.5) (<= (angle bt1 bt2) (* 2 pi))))
(vla-move obj (vlax-3d-point bt) (vlax-3d-point (list (- (car bt) (- x (car bt))) (cadr bt) (caddr bt))))
(vla-move obj (vlax-3d-point bt) (vlax-3d-point (list (car bt) (- (cadr bt) (- y (cadr bt))) (caddr bt)))));if
  (setq n (1+ n))
);while
  (princ)
);defun vt
