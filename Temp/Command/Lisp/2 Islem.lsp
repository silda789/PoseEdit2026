; **** TY ve CY in alt programi ****
(defun yazdeg (son obje / eslis eski delis)
     (setq eslis (entget obje))
     (setq eski (assoc 1 (entget obje)))
     (setq delis (subst son eski eslis))
     (entmod delis)
     (princ)
)

; **** secilen sayilarin TOPLAMINI, secilen yaziya yazar ***
(defun c:ty (/ son obje ss n son lll na say te)
   (setq ss (ssget) n 0 son 0 lll nil)
   (setq na (ssname ss n))
   (setq old_zin (getvar "DIMZIN"))
   (setvar "DIMZIN" 8)
   (while na
       (if (= (cdr(assoc 0(entget na))) "TEXT") (progn
        (setq say (cdr(assoc 1(entget na))))
        (setq son (+ son (atof say)))
        (setq lll (cons (atof say) lll))
       ));for if and progn
       (setq n (+ 1 n))
       (setq na (ssname ss n))
   ) ;while
   (princ (strcat "\nToplam: " (rtos son 2 3))) (princ)
   (princ)
   (setq obje (car (entsel "\nYazilacak yaziyi secin :")))
   (setq te (cdr (assoc 0 (entget obje))))
   (setq son (cons 1 (rtos son 2 1)))
   (if (= te "TEXT") (yazdeg son obje) (print "\nTEXT degil"))
   (setvar "DIMZIN" old_zin)
   (princ)
) ;defun
; **** secilen sayilarin CARPIMINI, secilen yaziya yazar ***
(defun c:CY (/ son obje ss n son lll na say te)
   (setq ss (ssget) n 0 son 1 lll nil)
   (setq na (ssname ss n))
   (setq old_zin (getvar "DIMZIN"))
   (setvar "DIMZIN" 8)
    (while na
       (if (= (cdr(assoc 0(entget na))) "TEXT") (progn
        (setq say (cdr(assoc 1(entget na))))
        (setq son (* son (atof say)))
        (setq lll (cons (atof say) lll))
       ));for if  and progn
       (setq n (+ 1 n))
       (setq na (ssname ss n))
   ) ;while
   (princ (strcat "\nToplam: " (rtos son 2 3))) (princ)
   (princ)
   (setq obje (car (entsel "\nYazilacak yaziyi secin :")))
   (setq te (cdr (assoc 0 (entget obje))))
   (setq son (cons 1 (rtos son 2 1)))
   (if (= te "TEXT") (yazdeg son obje) (print "\nTEXT degil"))
   (setvar "DIMZIN" old_zin)
   (princ)
) ;defun

(defun c:CY1 (/ son obje ss n son lll na say te)
   (setq ss (ssget) n 0 son 1 lll nil)
   (setq na (ssname ss n))
   (setq old_zin (getvar "DIMZIN"))
   (setvar "DIMZIN" 8)
    (while na
       (if (= (cdr(assoc 0(entget na))) "TEXT") (progn
        (setq say (cdr(assoc 1(entget na))))
        (setq son (* son (atof say)))
        (setq lll (cons (atof say) lll))
       ));for if  and progn
       (setq n (+ 1 n))
       (setq na (ssname ss n))
   ) ;while
   (setq son (/ son 1000))
   (princ (strcat "\nToplam: " (rtos son 2 2))) (princ)
   (princ)
   (setq obje (car (entsel "\nYazilacak yaziyi secin :")))
   (setq te (cdr (assoc 0 (entget obje))))
   (setq son (cons 1 (rtos son 2 1)))
   (if (= te "TEXT") (yazdeg son obje) (print "\nTEXT degil"))
   (setvar "DIMZIN" old_zin)
   (princ)
) ;defun
