;*******************************************************************************
(defun layer_e_gec (layer_name color line_type / layer_name color line_type)
  (if (= line_type "")
      (setq line_type "continuous")
  )
  (if (= (tblsearch "layer" layer_name) nil)
      (command "layer" "new" layer_name "")
  )
      (command "layer" "lt" line_type layer_name "c" color layer_name "" )
;
  (setq layer_check (cdr (assoc 70 (tblsearch "layer" layer_name))))
  (cond
    ((OR (= layer_check 4)(= layer_check 5))(command "LAYER" "UNLOCK" layer_name "" ))
    ((OR (= layer_check 1)(= layer_check 5))(command "LAYER" "THAW"   layer_name "" ))
  )
(setvar "clayer" layer_name)
)
;*******************************************************************************
(defun num (layer_name / b)
 (if (= (tblsearch "layer" layer_name) nil)
         (command  "layer" "new" layer_name
                           "c"   layer_name layer_name "" )
           (progn
             (setq layer_check (cdr (assoc 70 (tblsearch "layer" layer_name))))
             (if (OR (= layer_check 4)(= layer_check 5))
                         (command "LAYER" "UNLOCK" layer_name "" ))
             (if (OR (= layer_check 1)(= layer_check 5))
                         (command "LAYER" "THAW"   layer_name "" ))
           )
)
     (setq B (SSGET))
     (command "CHPROP" B "" "C" "BYLAYER"  "LA" layer_name "")
)

;;; Layer Oluþturma ;;;
(defun lay_ekle (layer tip renk /)

      (cond
      ((= tip  "hidden2")
          (entmake
          (list
               (cons 0 "LTYPE")
               (cons 100 "AcDbSymbolTableRecord")
               (cons 100 "AcDbLinetypeTableRecord")
               (cons   2 "HIDDEN2")
               (cons  70 0)
               (cons   3 "Hidden (.5x) _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _")
               (cons  72 65)
               (cons  73 2)
               (cons  40 0.1875)
               (cons  49 0.125)
               (cons  74 0 )
               (cons  49 -0.0625)
               (cons  74 0)
          )
          ))
      ((= tip  "hidden")
          (entmake
          (list
               (cons 0 "LTYPE")
               (cons 100 "AcDbSymbolTableRecord")
               (cons 100 "AcDbLinetypeTableRecord")
               (cons   2 "HIDDEN2")
               (cons  70 0)
               (cons   3 "Hidden __ __ __ __ __ __ __ __ __ __ __ __ __ _")
               (cons  72 65)
               (cons  73 2)
               (cons  40 0.375)
               (cons  49 0.25)
               (cons  74 0 )
               (cons  49 -0.125)
               (cons  74 0)
          )
          ))
      ((= tip  "dot")
          (entmake
          (list
               (cons 0 "LTYPE")
               (cons 100 "AcDbSymbolTableRecord")
               (cons 100 "AcDbLinetypeTableRecord")
               (cons   2 "DOT")
               (cons  70 0)
               (cons   3 "Dot . . . . . . . . . . . . . . . . . . . . . .")
               (cons  72 65)
               (cons  73 2)
               (cons  40 0.25)
               (cons  49 0.0)
               (cons  74 0 )
               (cons  49 -0.25)
               (cons  74 0)
          )
          ))
       ((= tip  "dot2")
          (entmake
          (list
               (cons 0 "LTYPE")
               (cons 100 "AcDbSymbolTableRecord")
               (cons 100 "AcDbLinetypeTableRecord")
               (cons   2 "DOT2")
               (cons  70 0)
               (cons   3 "Dot (.5x) .....................................")
               (cons  72 65)
               (cons  73 2)
               (cons  40 0.125)
               (cons  49 0.0)
               (cons  74 0 )
               (cons  49 -0.125)
               (cons  74 0)
          )
          ))
       )
          (entmake
          (list
             (cons 0 "Layer")
             (cons 100 "AcDbSymbolTableRecord")
             (cons 100 "AcDbLayerTableRecord")
             (cons 2 layer)
             (cons 6 tip)
             (cons 62 renk)
             (cons 70 0)
             (cons 290 1)
             (cons 370 0)
           ))

)


;;
AcRxObject
    AcGiDrawable
    AcHeapOperators
        AcDbObject
            AcDbSymbolTableRecord
                AcDbAbstractViewTableRecord
                AcDbBlockTableRecord

                AcDbLayerTableRecord
                AcDbLinetypeTableRecord
                AcDbRegAppTableRecord
                AcDbTextStyleTableRecord
                AcDbUCSTableRecord
