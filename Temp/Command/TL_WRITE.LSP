(defun c:RQTL ( / )
(setq dosya (open (strcat client_path "Standard/tablo_dili.txt") "W"))
(setq sayi (getint "\n Tablo dili 1->Rus 2->Eng 3->Rus&Eng :  " ))
(cond
 ((= sayi 2) (setq dil "eng" ))
 ((= sayi 3) (setq dil "re"  ))
 ( T         (setq dil "rus" ))
)
(princ dil dosya) (princ "\n" dosya)
(close dosya)
(princ)
)
;-------------------------------------------------------------------------------
(DEFUN TABLO_DILI (/ dosya okunan )
(setq dosya (open (strcat client_path "Standard/TABLO_DILI.TXT") "R"))
(setq okunan (read-line dosya))
(close dosya)
okunan
)
;-------------------------------------------------------------------------------


