;*******************************************************************************
;                                                                              *
;     expblock                                                                 *
;                                                                              *
;*******************************************************************************
(defun c:expblock ( / )
(setq BLOCK_GRUP (ssget '((0 . "INSERT")) ) )
(if (/= BLOCK_GRUP nil)
  (progn
      (setq toplam_block_adet (sslength BLOCK_GRUP))
      (setq i 0 )
      (while (< i toplam_block_adet)
        (command "explode" BLOCK_GRUP )
        (setq i (+ i 1))
      )
  )
    (princ "\n Block bulunamadi")
)
)
;*******************************************************************************
(defun c:silblock ( / )
(setq e (entsel "\n Silinecek bloklardan birini sec.....!!!"))
(setq blok_adi  (cdr (assoc 2 (entget (car e)))))
(setq grup (ssget (list  (cons 0 "INSERT") (cons 2 blok_adi))))
;(setq grup (ssget "X"  (list  (cons 0 "INSERT") (cons 2 blok_adi))))
(command "ERASE" grup "")
)
;*******************************************************************************


