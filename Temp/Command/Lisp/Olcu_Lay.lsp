(defun  olcu_kod_degis  (
                     entiti_list
                     new_feature
                     list_no
                                    /  entiti_list new_feature list_no old_feature
                    )
(setq old_feature (cdr (assoc list_no entiti_list)))
(setq entiti_list (subst (cons list_no new_feature) (assoc list_no entiti_list) entiti_list))
(entmod entiti_list)
(princ)
)
(defun c:ola (/)
(vl-load-com)
(setvar "qaflags" 0)
(setq dim_dimension "ren.dimension")
(setq dim_arrow "ren.arrow")
(setq all_layer (ssget '((-4 . "<OR") (0 . "DIMENSION") (0 . "LEADER") (-4 . "OR>") )))

(if (/= all_layer nil)
  (progn
   (setq n (sslength all_layer) l 0)
    (while (< l n)
        (setq eleman (entget (ssname all_layer l)))
        (if (= (vla-get-Arrowhead1Block (vlax-ename->vla-object (ssname all_layer l))) "OBLIQUE")
        (progn
        (olcu_kod_degis eleman dim_dimension 8))
        (progn
        (olcu_kod_degis eleman dim_arrow 8)))
        (setq l (+ l 1))
     )
  )
)
(setq l1 0)
(if (/= all_layer nil)
   (setq n1 (sslength all_layer) l1 0)
    (while (< l1 n1)
        (if (= (vla-get-ArrowheadType (vlax-ename->vla-object (ssname all_layer l))) "0")
        (setq eleman (entget (ssname all_layer l1)))
        (olcu_kod_degis eleman dim_arrow 8))
        (setq l1 (+ l1 1))
     )
  )

(princ)
)
