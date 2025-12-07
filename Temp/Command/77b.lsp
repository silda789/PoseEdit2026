;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
(defun _GFO ( a / )
 (apply 'append
  (mapcar
   (function
    (lambda ( a )
     (if (= 360 (car a))
      (_GFO (cdr a))
      (if (= 331 (car a)) (list (cdr a)))
     )
    )
   )
   (entget a)
  )
 )
)
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
(defun GFO ( en /  )
 (if
  (and
   (wcmatch (cdr (assoc 0 (setq en (entget en)))) "TEXT,MTEXT,ATTRIB")
   (setq en (cdr (assoc 360 en)))
   (setq en (dictsearch en "ACAD_FIELD"))
   (setq en (dictsearch (cdr (assoc -1 en)) "TEXT"))
   (setq en (cdr (assoc 360 en)))
  )
  (_GFO en)
 )
)
;-------------------------------------------------------------------------------
; (foreach ent lst (command "zoom" "o" ent "" ) (redraw ent 3))                -
;-------------------------------------------------------------------------------
(defun c:77b ( / en lst )
 (while
  (progn (setvar 'ERRNO 0) (setq en (car (nentsel "\nField sec...")))
   (cond
    ((= 7 (getvar 'ERRNO)) (princ "\nOlmadi, tekrar dene..."))
    ((eq 'ENAME (type en)) (if (setq lst (GFO en))
                               (progn
                                (foreach ent lst (command "zoom" "o" ent "" ))
                                (if (= (assoc 0 ent) nil) (princ "\nKaynak Eleman Silinmis..."))
                               )
                               (princ "\nBu obje gecerli field bilgisi icermiyor...")
                            )
    )
   )
  )
 )
(princ)
)
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------





