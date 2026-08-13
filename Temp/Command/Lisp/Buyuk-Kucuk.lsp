; ------------------------------------------------------------------------------------------------------------------
; --- büyük harf
; ------------------------------------------------------------------------------------------------------------------
(DEFUN C:BU ( / b hgyuk n l bb hgyuk1 e )
     (TERPRI)
(PRINC "Degistirilecek Yazilari Seciniz....:")
(setq B (SSGET '((0 . "TEXT"))))
  (if (/= B "")
       (progn
        (setq N (SSLENGTH B) L 0)
          (WHILE (< L N)
            (setq BB (ENTGET (SSNAME B L)))
             (IF  (= "TEXT" (CDR (ASSOC 0 BB)))
                    (PROGN  (setq HGYUK1 (ASSOC 1 BB))
                            (SETQ HGYUK (STRCASE (CDR HGYUK1)))
                            (setq BB (SUBST (CONS 1 HGYUK) HGYUK1 BB))
                            (ENTMOD BB)))(setq L (+ L 1)))
       )
  )
)


; ------------------------------------------------------------------------------------------------------------------
; --- küçük harf
; ------------------------------------------------------------------------------------------------------------------
(DEFUN C:KU ( / b hgyuk n l bb hgyuk1 e )
     (TERPRI)
(PRINC "Degistirilecek Yazilari Seciniz....:")
(setq B (SSGET '((0 . "TEXT"))))
  (if (/= B "")
       (progn
        (setq N (SSLENGTH B) L 0)
          (WHILE (< L N)
            (setq BB (ENTGET (SSNAME B L)))
             (IF  (= "TEXT" (CDR (ASSOC 0 BB)))
                    (PROGN  (setq HGYUK1 (ASSOC 1 BB))
                            (SETQ HGYUK (STRCASE (CDR HGYUK1) T))
                            (setq BB (SUBST (CONS 1 HGYUK) HGYUK1 BB))
                            (ENTMOD BB)))(setq L (+ L 1)))
       )
  )
)
