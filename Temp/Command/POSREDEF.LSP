;-------------------------------------------------------------------------------
(defun ReDef ( block / *error* oc spc doc block )
  (vl-load-com)
  ;; © Lee Mac 2010
  (defun *error* ( msg )
    (and oc (setvar 'CMDECHO oc))
    (or (wcmatch (strcase msg) "*BREAK,*CANCEL*,*EXIT*")
        (princ (strcat "\n** Error: " msg " **")))
    (princ)
  )
  (setq spc
    (if
      (or
        (eq AcModelSpace
          (vla-get-ActiveSpace
            (setq doc
              (vla-get-ActiveDocument
                (vlax-get-acad-object)
              )
            )
          )
        )
        (eq :vlax-true (vla-get-MSpace doc))
      )
      (vla-get-ModelSpace doc)
      (vla-get-PaperSpace doc)
    )
  )
  (setq oc (getvar 'CMDECHO)) (setvar 'CMDECHO 0)
  (if (setq block (findfile block))
    (progn
      (vla-delete
        (vla-insertblock spc
          (vlax-3D-point '(0. 0. 0.)) block 1. 1. 1. 0.
        )
      )
      (vl-cmdf "_.attsync" "_N" (vl-filename-base block))
    )
  )
  (setvar 'CMDECHO oc)
  (princ)
)
;-------------------------------------------------------------------------------
(DEFUN C:PZREDEF (/)
(metraj_layer_olustur)
(PZ_TUM_coz)
(SETQ I 0)
(REPEAT (LENGTH tip_liste)
(SETQ POZ_ADI (STRCAT source_path "Standard/PZ_" (NTH I tip_liste) ".DWG"))
(Redef POZ_ADI)
(SETQ I (1+ I)))
(PRINC)
)
;-------------------------------------------------------------------------------

