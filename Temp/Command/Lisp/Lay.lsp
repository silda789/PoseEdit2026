; Current Layer----------------------------------------------------------
(DEFUN C:ss ( / e)
       (setvar "cmdecho" 0)
       (setq e (entsel "\nLutfen LAYER-STYLE'la ilgili bir entity seciniz : "))
       (setq e (entget (car e)))
       (command "LAYER" "S" (cdr (assoc 8 e)) "")
       (PRINC (STRCAT "\nLayer set to " (CDR (ASSOC 8 E))))
       (IF (= "TEXT" (CDR (ASSOC 0 E)))
       (PROGN
       (command "TEXT" "S" (cdr (assoc 7 e)) ) (COMMAND)
       (PRINC (STRCAT "\nStyle set to " (cdr (assoc 7 e))))))
       (PRINC)
)

;;;;;;;Seçilen Layeri Gizler;;;;;;;;;;;;;;;
(defun c:g ( / p l n cizgi )
   (setq layer_gecerli   (getvar "clayer"  ))
   (setq p (ssget))
   (setq l 0 n (sslength p))
   (if (/= p nil)
     (progn
       (while (< l n)
          (setq layer_cizgiden (cdr (assoc 8 (entget (ssname p l)))))
             (if (= layer_cizgiden layer_gecerli)
               (progn
                   (if (= layer_cizgiden "0")
                             ( layer_e_gec  "255" "255" "")
                             ( layer_e_gec  "0" "255" "" )
                   )
               )
             )
        (command "layer" "f" layer_cizgiden "")
        (setq l (+ l 1))
       )
     )
   )
)

;Current layeri objelere atar.,,,
(DEFUN C:cl ( / katman b )
       (setq KATMAN (getvar "clayer"))
       (setq B (SSGET))
       (COMMAND "CHPROP" B "" "C" "BYLAYER" "LT" "BYLAYER" "LA" KATMAN "S" "1" "")
)
