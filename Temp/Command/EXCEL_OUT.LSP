;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun ren_metraj_tablo_dosya_yaz (cap_liste toplam_bilgi / en_son_toplam_agirlik anlik_tarih fname template_file cll csht rang sht nwb awb aexc)
(setq anlik_tarih (RTOS (GETVAR "CDATE") 2 6))
(setq template_file (strcat source_path "Standard/RL-POS.xls"))
(setq fname (strcat (getvar "dwgprefix") (substr (getvar "dwgname") 1 (- (strlen (getvar "dwgname")) 4 )) "-" anlik_tarih ".xls"))
(vl-file-copy template_file fname)
(setq aexc  (vlax-get-or-create-object "Excel.Application")) (if (null aexc) (progn (alert "Excel baslatilamadi.") (exit)))
(setq nwb (vlax-invoke-method (vlax-get-property aexc 'WorkBooks) 'Open fname))
(setq sht   (vlax-get-property  nwb   "Sheets"    ))
(setq csht  (vlax-get-property  sht   "Item" 1    ))
(setq cll   (vlax-get-property  csht  "Cells"     ))
(vla-put-visible aexc :vlax-false)
    (setq  row  9 ) ; Metrajin eklenmeye baslayacagi satir.
    (setq  row_num (length toplam_bilgi))
    (setq  i    0 )
    (repeat row_num
      (vlax-put-property  cll "Item" row  1 (vl-princ-to-string (cdr (assoc  1 (nth i toplam_bilgi)))))
      (vlax-put-property  cll "Item" row  2 (vl-princ-to-string (cdr (assoc  3 (nth i toplam_bilgi)))))
      (vlax-put-property  cll "Item" row  3 (vl-princ-to-string (cdr (assoc  2 (nth i toplam_bilgi)))))
      (vlax-put-property  cll "Item" row  4 (vl-princ-to-string (rtos (* olcu_carp (cdr (assoc 14 (nth i toplam_bilgi)))) 2 3)))
      (setq row (1+ row))
      (setq i   (1+   i))
    )
  (vlax-invoke-method nwb  "Save" )
  (vlax-invoke-method aexc "Quit" )
  (vlax-release-object cll)
  (vlax-release-object csht)
  (vlax-release-object sht)
  (vlax-release-object nwb)
  (vlax-release-object aexc)
  (gc)
(princ (strcat "\n Metraj tablosu, " fname " adiyla diske yazildi" ))
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:RQTE ( / )
(vl-load-com)
(setq osmode_old (getvar "osmode"))
(setq dimzin_old (getvar "dimzin"))
(setq clayer_old (getvar "clayer"))
(setvar "cmdecho" 0 )
(setvar "osmode"  0 )
(setvar "dimzin"  1 )
(setvar "attreq"  0 )
(setq olcek     (* 0.0100 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq olcu_carp (/ 1 (BIRIM_OKU)))
(setq eset (ssget (list (cons 0 "INSERT") (cons 2  "RL-POS")))) ; Metraji cikarilacak elemanlar seciliyor.
(if (= eset nil) (progn (terpri) (princ "Hic bir eleman secilmedi...") (exit)))
(setq toplam_bilgi (RENAISSANCE_toplam_bilgi_olustur eset )) ; Bilgiler toplaniyor
(hata_kontrol toplam_bilgi) ; Hata kontrolu yapýliyor
(setq toplam_bilgi (RENAISSANCE_toplam_bilgi_derle toplam_bilgi )) ; Bilgiler derleniyor
(setq cap_liste    (cap_bilgi_olustur  toplam_bilgi )) ; Donati caplari ve toplam boylari derleniyor
(ren_metraj_tablo_dosya_yaz cap_liste toplam_bilgi )
(setvar "osmode" osmode_old)
(setvar "clayer" clayer_old)
(setvar "dimzin" dimzin_old)
(setvar "cmdecho" 1)
(princ)
)

;-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

