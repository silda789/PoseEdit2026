;*******************************************************************************
(DEFUN BIRIM_OKU ( / dosya okunan )
(setq dosya (open (strcat client_path "Standard/UNIT.txt") "R"))
(setq okunan (read-line dosya))
(close dosya)
(setq okunan (atof okunan))
okunan
)
;******************************************************************************
(DEFUN OLCEK_OKU ( / dosya okunan )
(setq dosya (open (strcat client_path "Standard/UNIT.txt") "R"))
(setq okunan (read-line dosya))
(setq okunan (read-line dosya))
(close dosya)
(setq okunan (atof okunan))
okunan
)
;******************************************************************************
(DEFUN PROJE_OKU (/ dosya okunan )
(setq dosya (open (strcat client_path "Standard/UNIT.TXT") "R"))
(setq okunan (read-line dosya))
(setq okunan (read-line dosya))
(setq okunan (read-line dosya))
(setq okunan (read-line dosya))
(close dosya)
okunan
)
