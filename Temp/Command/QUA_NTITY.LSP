;
(defun RENAISSANCE_eski_boy_oku_isle (boy / )
(setq boy_isle_1 (vl-string-translate "lL=" "   " boy))
(setq boy_isle_2 (vl-string-translate "()-~" "    " boy_isle_1))
  (if (= boy_isle_1 boy_isle_2)
    (setq boy boy_isle_1)
    (setq boy (rtos (eval (read (strcat "(/ (+ " boy_isle_2 ") 3 )"))) 2 0))
  )
boy
)

;
;
;
;------------------------------------------------------------------------------------------------------------------------------
;
;
(defun RENAISSANCE_metraj_layer_olustur (/)
(setvar "cmdecho" 0)
(Create_layer (strcat source_path "Standard/REN_QT_layer.txt"))
(donati_metraj_yazi_sitili_yap "ren Gost.common" "GOST Common.ttf" 0.70 )
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun REN_cizgi (nokta1 yon mesafe cizgi_tipi / nokta1 nokta2 cizgi_tipi cizgi_layer)
(setq nokta2 (polar nokta1 yon mesafe ))
(cond
((= cizgi_tipi  1 ) (setq cizgi_layer "ren.mtr.layer_l1" ))
(    T              (setq cizgi_layer "Defpoints" ))
)
(entmake (list (cons   0 "LINE")
               (cons 100 "AcDbEntity")
               (cons 100 "AcDbLine")
               (cons  10 nokta1)
               (cons  11 nokta2)
               (cons   8 cizgi_layer)
         )
  )
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;(REN_Y pt1 text height yon1 mesafe1 yon2 mesafe2 D_72 D_73 )
; 72  0 = Left      1 = Center  2 = Right
; 73  0 = Baseline  1 = Bottom  2 = Middle    3 = Top
(defun REN_Y (pt1 text height yon1 mesafe1 yon2 mesafe2 D_72 D_73 / pt1 text height D_72 D_73)
(if (= width nil) (setq width 0.8))
(setq pt1 (polar pt1 yon1 mesafe1 ))
(setq pt1 (polar pt1 yon2 mesafe2 ))
(entmake (list (cons 0 "TEXT")
		  (cons 100 "AcDbEntity" )
		  (cons 100 "AcDbText" )
		  (cons   7 "ren Gost.common")
		  (cons  10 pt1 )
		  (cons  11 pt1 )
		  (cons   1 text )
		  (cons  40 height )
		  (cons  41 0.8 )
		  (cons  50 0 )
 		  (cons  72 D_72 )
		  (cons  73 D_73 )
		  (cons   8 "ren.mtr.layer_t1" )
         )
)
(princ)
)
;-------------------------------------------------------------------------------
;- REN_Y2 komutu width factor girilmesi istenirse diye -------------------------
;-------------------------------------------------------------------------------
(defun REN_Y2 (pt1 text height width yon1 mesafe1 yon2 mesafe2 D_72 D_73 / pt1 text height D_72 D_73)
(if (= width nil) (setq width 0.8))
(setq pt1 (polar pt1 yon1 mesafe1 ))
(setq pt1 (polar pt1 yon2 mesafe2 ))
(entmake (list (cons 0 "TEXT")
		  (cons 100 "AcDbEntity" )
		  (cons 100 "AcDbText" )
		  (cons   7 "ren Gost.common")
		  (cons  10 pt1 )
		  (cons  11 pt1 )
		  (cons   1 text )
		  (cons  40 height )
		  (cons  41 width )
		  (cons  50 0 )
 		  (cons  72 D_72 )
		  (cons  73 D_73 )
		  (cons   8 "ren.mtr.layer_t1" )
         )
)
(princ)
)
(defun REN_Y3 (pt1 text height yon1 mesafe1 yon2 mesafe2 D_72 D_73 / pt1 text height D_72 D_73)
(if (= width nil) (setq width 0.8))
(setq per (* 3.75 (OLCEK_OKU)))
(setq pt1 (polar pt1 yon1 mesafe1 ))
(setq pt1 (polar pt1 yon2 mesafe2 ))
(setq pt1 (polar pt1 1.5708 per ))
(entmake (list (cons 0 "MTEXT")
		  (cons 100 "AcDbEntity" )
		  (cons 100 "AcDbMText" )
		  (cons   7 "ren Gost.common")
		  (cons  10 pt1 )
		  (cons  11 pt1 )
		  (cons   1 text )
		  (cons  40 height )
		  (cons  41 0.8 )
		  (cons  50 0 )
 		  (cons  72 D_72 )
		  (cons  73 D_73 )
		  (cons   8 "ren.mtr.layer_t1" )
         )
)
(princ)
)
;-------------------------------------------------------------------------------
;- REN_Y2 komutu width factor girilmesi istenirse diye -------------------------
;-------------------------------------------------------------------------------
(defun REN_Y4 (pt1 text height width yon1 mesafe1 yon2 mesafe2 D_72 D_73 / pt1 text height D_72 D_73)
(if (= width nil) (setq width 0.8))
(setq per (* 3.75 (OLCEK_OKU)))
(setq pt1 (polar pt1 yon1 mesafe1 ))
(setq pt1 (polar pt1 yon2 mesafe2 ))
(setq pt1 (polar pt1 1.5708 per ))
(entmake (list (cons 0 "MTEXT")
		  (cons 100 "AcDbEntity" )
		  (cons 100 "AcDbMText" )
		  (cons   7 "ren Gost.common")
		  (cons  10 pt1 )
		  (cons  11 pt1 )
		  (cons   1 text )
		  (cons  40 height )
		  (cons  41 width )
		  (cons  50 0 )
 		  (cons  72 D_72 )
		  (cons  73 D_73 )
		  (cons   8 "ren.mtr.layer_t1" )
         )
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun RENAISSANCE_metraj_tablo_ciz_rus (yerlesim cap_liste toplam_bilgi olcek / yerlesim cap_liste toplam_bilgi olcek )
;
(command "_.Undo" "_BE")
(setq cap_adet (length cap_liste    ))
(setq poz_adet (length toplam_bilgi ))
(setq tip_bilgi_adet 0)
(setq dosya (open (strcat client_path "Standard/Tablo_Tipi.TXT") "r"))
(setq Yerlesim_1 (read-line dosya) )
(setq Yerlesim_2 (read-line dosya) )
(setq Yerlesim_3 (read-line dosya) )
(close dosya)
(setq i 0)
(while (< i poz_adet)
(if (= "00" (cdr (assoc 6 (nth i toplam_bilgi)))) (progn (setq tip_bilgi_adet (+ tip_bilgi_adet 1)) ))
(setq i (+ i 1))
)
(setq poz_tip_adet 0)
(setq i 0)
(while (< i poz_adet)
(if (/= "00" (cdr (assoc 6 (nth i toplam_bilgi)))) (setq poz_tip_adet (+ poz_tip_adet 1)))
(setq i (+ i 1))
)
(setq Sut-1-01 ( * olcek 1.70 ))
(setq Sut-1-02 ( * olcek 6.00 ))

(setq Sat-1-01 ( * olcek 0.80 ))
(setq Sat-1-02 ( * olcek 1.50 ))
;
(setq Sut-1-2  ( * olcek 0.80 ))
;
(setq Sut-2-01 ( * olcek 1.50 ))
(setq Sut-2-02 ( * olcek 6.00 ))
(setq Sut-2-03 ( * olcek 6.00 ))
(setq Sut-2-04 ( * olcek 1.50 ))
(setq Sut-2-05 ( * olcek 1.50 ))
(setq Sut-2-06 ( * olcek 2.00 ))

(setq Sat-2-01 ( * olcek 0.80 ))
(setq Sat-2-02 ( * olcek 1.50 ))
(setq Sat-2-03 ( * olcek 0.80 ))
;
(setq Sut-2-3  ( * olcek 0.80 ))
;
(setq Sut-3-01 ( * olcek 4.00 ))
(setq Sut-3-02 ( * olcek 1.50 ))
(setq Sut-3-03 ( * olcek 2.00 ))

(setq Sat-3-01 ( * olcek 0.80 ))
;
(setq yzy1     ( * olcek 0.30 ))
(setq yzy2     ( * olcek 0.30 ))
;
(setq cl_1  3  )                     ;Baslik yazilari rengi
(setq cl_2  3  )                     ;Baslik disinda kalan yazilarin rengi
(setq cl_3  3  )                     ;Ana cizgilerin rengi
(setq cl_4  8  )                     ;Diger cizgilerin rengi
;

;
(setq sag    (* 0.0 pi ) )
(setq sol    (* 1.0 pi ) )
(setq yukari (* 0.5 pi ) )
(setq asagi  (* 1.5 pi ) )
(if (= poz_adet tip_bilgi_adet)
    (progn (setq duseyadet 6) (setq yatayadet 7))
    (progn (setq duseyadet 5) (setq yatayadet 6))
)
(cond ((= Yerlesim_1 "1")
         (setq P_001 yerlesim)
         (setq P_003 (polar P_001 sag   (+ Sut-1-01 Sut-1-02 Sut-1-2 )))
         (setq P_006 (polar P_001 sag (+ Sut-1-01 Sut-1-02 Sut-1-2 Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06 Sut-2-3)))
      )
      ((= Yerlesim_2 "1")
         (setq P_003 yerlesim)
         (setq P_001 (polar P_003 asagi   (+ Sat-2-02 (/ Sat-1-01 2) (* Sat-2-03 (+ poz_adet duseyadet 1 )))))
         (setq P_006 (polar P_001 sag (+ Sut-1-01 Sut-1-02 (/ Sat-1-01 2)))))

      ((= Yerlesim_3 "1")
         (setq P_001 yerlesim)
         (setq P_003 (polar P_001 sag   (+ Sut-1-01 Sut-1-02 Sut-1-2 )))
         (setq P_0061 (polar P_003 asagi   (+ Sat-2-02 (/ Sat-1-01 2) (* Sat-2-03 (+ poz_adet duseyadet 1 )))))
         (setq P_006 (polar P_0061 sag   (- (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)))))
)
;
; Tablo - 1  ;
;
(setq P_002 (polar P_001 asagi Sat-1-01))
(if (= tip_bilgi_adet 0)
    (progn (setq Sutadet 2)(setq Satadet 1)) (progn (setq Sutadet 3)(setq Satadet 2)) )
;
(REN_cizgi P_001                                   sag   (+ Sut-1-01 Sut-1-02) 2 )
(REN_cizgi P_001                                   asagi    Sat-1-01           2 )
(REN_cizgi (polar P_001 sag (+ Sut-1-01 Sut-1-02)) asagi    Sat-1-01           2 )
(REN_cizgi P_002                                   sag   (+ Sut-1-01 Sut-1-02) 1 )
(REN_cizgi P_002                                   asagi (* Sat-1-02 (+ poz_tip_adet Satadet ))  1 )
(REN_cizgi (polar P_002 sag Sut-1-01)              asagi (* Sat-1-02 (+ poz_tip_adet 1 ))  1 )
(REN_cizgi (polar P_002 sag (+ Sut-1-01 Sut-1-02)) asagi (* Sat-1-02 (+ poz_tip_adet Satadet ))  1 )
(setq i 1)
(while (< i (+ poz_tip_adet Sutadet))
(REN_cizgi (polar P_002 asagi (* i Sat-1-02))      sag   (+ Sut-1-01 Sut-1-02) 1 )
(setq i (+ i 1))
)
;(REN_Y pt1 text height yon1 mesafe1 yon2 mesafe2 D_72 D_73 )
; 72  0 = Left      1 = Center  2 = Right
; 73  0 = Baseline  1 = Bottom  2 = Middle    3 = Top
; 0 cntr
; 1 POZ
; 2 adet
; 3 cap
; 4 aralik
; 5 BOY
; 6 TIP
; 7 A
; 8 B
; 9 C
;10 D
;11 E
;12 F
;13 R
;14 boy
;15 MALZEME
;
;    Detail list	\\U+0412\\U+0435\\U+0434\\U+043E\\U+043C\\U+043E\\U+0441\\U+0442\\U+044C \\U+0434\\U+0435\\U+0442\\U+0430\\U+043B\\U+0435\\U+0439
(REN_Y P_001 "\\U+0412\\U+0435\\U+0434\\U+043E\\U+043C\\U+043E\\U+0441\\U+0442\\U+044C \\U+0434\\U+0435\\U+0442\\U+0430\\U+043B\\U+0435\\U+0439" yzy2 sag (* 0.5 (+ Sut-1-01 Sut-1-02)) asagi (* 0.5 Sat-1-01) 1 2)
;
;    Pos.	\\U+041F\\U+043E\\U+0437
(REN_Y P_002 "\\U+041F\\U+043E\\U+0437."  yzy2 sag (* 0.5 Sut-1-01) asagi (* 0.5 Sat-1-02) 1 2)
;
;    Draft	\\U+042D\\U+0441\\U+043A\\U+0438\\U+0437
(REN_Y P_002 "\\U+042D\\U+0441\\U+043A\\U+0438\\U+0437" yzy2 sag (+ Sut-1-01 (* 0.5 Sut-1-02)) asagi (* 0.5 Sat-1-02) 1 2)
(setq i 0)
(setq j 0)
(while (< i poz_adet)
(if (/= "00" (cdr (assoc 6 (nth i toplam_bilgi))))
  (progn
   (REN_Y P_002 (cdr (assoc 1 (nth i toplam_bilgi))) yzy2 sag (* 0.5 Sut-1-01) asagi (* (+ j 1.5) Sat-1-02) 1 2)
   (setq dwgname (strcat source_path "Standard/PZ_" (cdr (assoc 6 (nth i toplam_bilgi))) ".dwg" ))
   (if (not (findfile dwgname)) (setq dwgname (strcat source_path "Standard/PZ_99")))
   (setq dwgname_insert_point (polar P_002 sag (+ Sut-1-01 (* 0.5 Sut-1-02))))
   (setq dwgname_insert_point (polar dwgname_insert_point asagi (* (+ j 1.5) Sat-1-02)))
   (command ".-insert" dwgname dwgname_insert_point (/ Sut-1-02 450 ) (/ Sat-1-02 100 ) 0 )
   (setq bilgi_isle (entlast))
   (att_yaz bilgi_isle "A" (cdr (assoc   7 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "B" (cdr (assoc   8 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "C" (cdr (assoc   9 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "D" (cdr (assoc  10 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "E" (cdr (assoc  11 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "F" (cdr (assoc  12 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "R" (cdr (assoc  13 (nth i toplam_bilgi))))
   (setq j (+ j 1))
  )
)
   (setq i (+ i 1))
)
(if (and (/= poz_adet tip_bilgi_adet) (/= tip_bilgi_adet 0))
    (progn
(REN_Y4 dwgname_insert_point "\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043F\\U+043E\\U+0437.\\U+0020\\1-2\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+0432\\U+043D\\U+0443\\U+0442\\U+0440.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C.\\U+000D\\U+000A\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043E\\U+0441\\U+0442\\U+0430\\U+043B\\U+044C\\U+043D\\U+044B\\U+0445\\U+0020\\U+043F\\U+043E\\U+0437\\U+0438\\U+0446\\U+0438\\U+0439\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+043D\\U+0430\\U+0440\\U+0443\\U+0436.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C."  yzy1 0.65 sag (- (* 0.1 Sut-1-01) (+ Sut-1-01 (* 0.5 Sut-1-02))) asagi (* 1.00 Sat-1-02) 0 2)
))
;
; Tablo - 2  ;
;
(setq P_004 (polar P_003 asagi    Sat-2-01))
(setq P_004 (polar P_003 asagi    Sat-2-01))
;
(setq P_005 (polar P_004 asagi    Sat-2-02))
(setq P_106 (polar P_005 asagi  (* 2 Sat-2-03)))
(setq P_107 (polar P_106 sag (+ Sut-2-01 Sut-2-02 )))
(setq P_108 (polar P_106 sag (+ Sut-2-01 Sut-2-02 (* 0.30 Sut-2-03))))
(setq P_109 (polar P_106 sag (+ Sut-2-01 Sut-2-02 (* 0.60 Sut-2-03))))
(REN_cizgi P_003  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 2 )
(REN_cizgi P_003                                   asagi    Sat-2-01            2 )
(REN_cizgi (polar P_003 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi Sat-2-01 2 )
(REN_cizgi P_004  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(REN_cizgi P_005  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(setq i 1)
(while (< i (+ poz_adet yatayadet))
(REN_cizgi (polar P_005 asagi (* i Sat-2-03)) sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(setq i (+ i 1))
)
(REN_cizgi P_004                                                                       asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet duseyadet )))  1 )
(REN_cizgi (polar P_004 sag    Sut-2-01)                                               asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02))                                     asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03))                            asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi P_108  asagi (* Sat-2-03 poz_adet)  1 )
(REN_cizgi P_109  asagi (* Sat-2-03 poz_adet)  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04))                   asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05))          asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet duseyadet )))  1 )
;Reinforcement details specification \\U+0421\\U+043F\\U+0435\\U+0446\\U+0438\\U+0444\\U+0438\\U+043A\\U+0430\\U+0446\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0445 \\U+0438\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+0439
(REN_Y P_003 "\\U+0421\\U+043F\\U+0435\\U+0446\\U+0438\\U+0444\\U+0438\\U+043A\\U+0430\\U+0446\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0445 \\U+0438\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+0439" yzy2 sag (* 0.5 (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (* 0.5 Sat-2-01) 1 2)
;
;    Pos.	\\U+041F\\U+043E\\U+0437
(REN_Y P_004 "\\U+041F\\U+043E\\U+0437."           yzy2 sag (* 0.5 Sut-2-01) asagi (* 0.5 Sat-2-02) 1 2)
;
;    Designation	\\U+041E\\U+0431\\U+043E\\U+0437\\U+043D\\U+0430\\U+0447\\U+0435\\U+043D\\U+0438\\U+0435
(REN_Y P_004 "\\U+041E\\U+0431\\U+043E\\U+0437\\U+043D\\U+0430\\U+0447\\U+0435\\U+043D\\U+0438\\U+0435"    yzy2 sag (+ Sut-2-01 (* 0.5 Sut-2-02)) asagi (* 0.5 Sat-2-02) 1 2)
;
;    Name	\\U+041D\\U+0430\\U+0438\\U+043C\\U+0435\\U+043D\\U+043E\\U+0432\\U+0430\\U+043D\\U+0438\\U+0435
(REN_Y P_004 "\\U+041D\\U+0430\\U+0438\\U+043C\\U+0435\\U+043D\\U+043E\\U+0432\\U+0430\\U+043D\\U+0438\\U+0435"           yzy2 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (* 0.5 Sat-2-02) 1 2)
;
;    Q-ty	\\U+041A\\U+043E\\U+043B.
(REN_Y P_004 "\\U+041A\\U+043E\\U+043B."           yzy2 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (* 0.5 Sat-2-02) 1 2)
;
;    W. of pc.	\\U+041C\\U+0430\\U+0441\\U+0441\\U+0430 \\U+0435\\U+0434., \\U+043A\\U+0433
(REN_Y2 P_004 "\\U+041C\\U+0430\\U+0441\\U+0441\\U+0430 \\U+0435\\U+0434.,"      yzy1 0.6 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (* 0.3 Sat-2-02) 1 2)
(REN_Y P_004 "\\U+043A\\U+0433"                                            yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (* 0.7 Sat-2-02) 1 2)
;
;    Notes	\\U+041F\\U+0440\\U+0438\\U+043C\\U+0435\\U+0447\\U+0430\\U+043D\\U+0438\\U+0435
(REN_Y P_004 "\\U+041F\\U+0440\\U+0438\\U+043C\\U+0435\\U+0447\\U+0430\\U+043D\\U+0438\\U+0435"          yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (* 0.5 Sat-2-02) 1 2)
;
;    Columns	%%U\\U+041F\\U+043B\\U+0438\\U+0442\\U+0430 \\U+041F\\U+043C
(REN_Y P_004 "..."        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 0.5 Sat-2-03)) 1 2)
;
;    Details	%%U\\U+0414\\U+0435\\U+0442\\U+0430\\U+043B\\U+0438
(REN_Y P_004 "%%U\\U+0414\\U+0435\\U+0442\\U+0430\\U+043B\\U+0438"        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 1.5 Sat-2-03)) 1 2)
;
;    Material	\\U+041C\\U+0430\\U+0442\\U+0435\\U+0440\\U+0438\\U+0430\\U+043B
(REN_Y P_004 "\\U+041C\\U+0430\\U+0442\\U+0435\\U+0440\\U+0438\\U+0430\\U+043B"       yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 3.5 ))) 1 2)
;
;    Concrete B25, F75, W6	\\U+0411\\U+0435\\U+0442\\U+043E\\U+043D  B25,W12,F50
(REN_Y P_004 "\\U+0411\\U+0435\\U+0442\\U+043E\\U+043D"  yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "M\U+00B3"   yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "..."  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "..."  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
;
(if (= poz_adet tip_bilgi_adet)
    (progn
;Straight bars are conditionaly not presented in details list  \U+041F\U+0440\U+044F\U+043C\U+044B\U+0435 \U+0441\U+0442\U+0435\U+0440\U+0436\U+043D\U+0438 \U+0443\U+0441\U+043B\U+043E\U+0432\U+043D\U+043E \U+043D\U+0435 \U+043F\U+043E\U+043A\U+0430\U+0437\U+0430\U+043D\U+044B \U+0432 \U+043F\U+0435\U+0440\U+0435\U+0447\U+043D\U+0435 \U+0434\U+0435\U+0442\U+0430\U+043B\U+0435\U+0439
(REN_Y3 P_004 "\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043F\\U+043E\\U+0437.\\U+0020\\1-2\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+0432\\U+043D\\U+0443\\U+0442\\U+0440.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C.\\U+000D\\U+000A\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043E\\U+0441\\U+0442\\U+0430\\U+043B\\U+044C\\U+043D\\U+044B\\U+0445\\U+0020\\U+043F\\U+043E\\U+0437\\U+0438\\U+0446\\U+0438\\U+0439\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+043D\\U+0430\\U+0440\\U+0443\\U+0436.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C."  yzy1 sag (* 0.1 Sut-2-01) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5.5 ))) 0 2)
))
(setq i 0)
(while (< i poz_adet)
(REN_Y P_005 (cdr (assoc 1 (nth i toplam_bilgi)))  yzy1 sag (* 0.5 Sut-2-01) asagi (* (+ i 2.5) Sat-2-03) 1 2)
;
;    GOST P 52544-2006	\\U+0413\\U+041E\\U+0421\\U+0422 \\U+0420  52544-2006
(REN_Y P_005 "\\U+0413\\U+041E\\U+0421\\U+0422 \\U+0420  52544-2006"  yzy1 sag (+ Sut-2-01 (* 0.1 Sut-2-02)) asagi (* (+ i 2.5) Sat-2-03) 0 2)
;
; Ø \\U+0428
(REN_Y P_107 (strcat "\\U+2300" (cdr (assoc  3 (nth i toplam_bilgi)))) yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_108             (cdr (assoc 15 (nth i toplam_bilgi)))  yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_109             (cdr (assoc  5 (nth i toplam_bilgi)))  yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_005 (cdr (assoc 2 (nth i toplam_bilgi)))  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.9 Sut-2-04)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(setq birim_agirlik (* 0.001 (fix (+ 0.5 (* 1000 (cp_ag (atof (cdr (assoc 3 (nth i toplam_bilgi))))))))))
(setq eleman_agirligi   (* olcu_carp (cdr (assoc 14 (nth i toplam_bilgi))) birim_agirlik))
(setq t_eleman_agirligi (* (atof (cdr (assoc  2 (nth i toplam_bilgi)))) eleman_agirligi))
(REN_Y P_005 (rtos eleman_agirligi   2 2) yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.9 Sut-2-05)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(REN_Y P_005 (rtos t_eleman_agirligi 2 2) yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.9 Sut-2-06)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(setq i (+ i 1))
)
;
; Tablo - 3  ;
;
(setq P_007 (polar P_006 asagi Sat-3-01))
(setq P_008 (polar P_007 asagi (* 5 Sat-3-01)))
(setq P_009 (polar P_008 asagi Sat-3-01))
(setq P_010 (polar P_007 sag   Sut-3-01))
(setq P_011 (polar P_010 asagi Sat-3-01))
(setq P_012 (polar P_011 asagi Sat-3-01))
(setq P_013 (polar P_012 asagi Sat-3-01))
(setq P_014 (polar P_013 asagi Sat-3-01))
(setq P_015 (polar P_014 asagi Sat-3-01))
(setq P_016 (polar P_015 asagi Sat-3-01))
(setq P_026 (polar P_011 sag   (* (+ 1 cap_adet) Sut-3-02) ))
(setq P_032 (polar P_006 sag   (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)))
(setq P_033 (polar P_032 asagi Sat-3-01))
(REN_cizgi P_006  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 2 )
(REN_cizgi P_006  asagi  Sat-3-01  2 )
(REN_cizgi P_032  asagi  Sat-3-01  2 )
(REN_cizgi P_007  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_007  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_010  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_033  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_008  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_009  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_011  sag (+          (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_012  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_013  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_014  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_026  asagi (* 5 Sat-3-01)  1 )
(setq i 1)
(while (< i (+ 1 cap_adet))
(setq P_014 (polar P_014 sag Sut-3-02))
(REN_cizgi P_014 asagi (* 2 Sat-3-01) 1)
(setq i (+ i 1))
)
;
;    Steel Flow Specification, kg	\\U+0412\\U+0435\\U+0434\\U+043E\\U+043C\\U+043E\\U+0441\\U+0442\\U+044C \\U+0440\\U+0430\\U+0441\\U+0445\\U+043E\\U+0434\\U+0430 \\U+0441\\U+0442\\U+0430\\U+043B\\U+0438
(REN_Y P_006 "\\U+0412\\U+0435\\U+0434\\U+043E\\U+043C\\U+043E\\U+0441\\U+0442\\U+044C \\U+0440\\U+0430\\U+0441\\U+0445\\U+043E\\U+0434\\U+0430 \\U+0441\\U+0442\\U+0430\\U+043B\\U+0438\\U+002C \\U+043A\\U+0433" yzy2 sag (* 0.5 (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.5 Sat-3-01) 1 2)
;
;    Mark of Element	\\U+041C\\U+0430\\U+0440\\U+043A\\U+0430 \\U+044D\\U+043B\\U+0435\\U+043C\\U+0435\\U+043D\\U+0442\\U+0430
(REN_Y P_007 "\\U+041C\\U+0430\\U+0440\\U+043A\\U+0430 \\U+044D\\U+043B\\U+0435\\U+043C\\U+0435\\U+043D\\U+0442\\U+0430"              yzy1 sag (* 0.5 Sut-3-01) asagi (* 2.5 Sat-3-01) 1 2)
;
;    Column 	\\U+041F\\U+043B\\U+0438\\U+0442\\U+0430 \\U+041F\\U+043C
;(REN_Y P_008 "\\U+041F\\U+043B\\U+0438\\U+0442\\U+0430 \\U+041F\\U+043C"                      yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_008 "..." yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.5 Sat-3-01) 1 2)
;
;    Reinforcement Goods	\\U+0418\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0435
(REN_Y P_010 "\\U+0418\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0435"          yzy1 sag (* 0.5 (+ (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.5 Sat-3-01) 1 2)
;
;    Class of reinforcement	\\U+0410\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+0430 \\U+043A\\U+043B\\U+0430\\U+0441\\U+0441\\U+0430
(REN_Y P_011 "\\U+0410\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+0430 \\U+043A\\U+043B\\U+0430\\U+0441\\U+0441\\U+0430"       yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_012 (cdr (assoc 15 (nth 0 toplam_bilgi))) yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
;
;    GOST P 52544-2006	\\U+0413\\U+041E\\U+0421\\U+0422 \\U+0420  52544-2006
(REN_Y P_013 "\\U+0413\\U+041E\\U+0421\\U+0422 \\U+0420  52544-2006"            yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
;
;    Total	\\U+0412\\U+0441\\U+0435\\U+0433\\U+043E
(REN_Y P_026 "\\U+0412\\U+0441\\U+0435\\U+0433\\U+043E"                        yzy1 sag (* 0.5 Sut-3-03 ) asagi (* 2.0 Sat-3-01) 1 2)
(setq en_son_toplam_agirlik 0 )
(setq i 0)
(while (< i cap_adet)
;
; Ø \\U+0428
(REN_Y P_013 (strcat "\\U+2300" (cdr (assoc 1 (nth i cap_liste)))) yzy1 sag (* (+ 0.5 i) Sut-3-02 ) asagi (* 1.5 Sat-3-01) 1 2)
(setq birim_agirlik (* 0.001 (fix (+ 0.5 (* 1000 (cp_ag (atof (cdr (assoc 1 (nth i cap_liste))))))))))
(REN_Y P_013 (rtos (* (* olcu_carp (atof (cdr (assoc 2 (nth i cap_liste))))) birim_agirlik) 2 2) yzy1 sag (* (+ 0.5 i) Sut-3-02 ) asagi (* 2.5 Sat-3-01) 1 2)
(setq en_son_toplam_agirlik (+ en_son_toplam_agirlik (* (* olcu_carp (atof (cdr (assoc 2 (nth i cap_liste))))) birim_agirlik)))
(setq i (+ i 1))
)
;
;    Total	\\U+0418\\U+0442\\U+043E\\U+0433
(REN_Y P_013 "\\U+0418\\U+0442\\U+043E\\U+0433\\U+043E" yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 1.5 Sat-3-01) 1 2)
(REN_Y P_013 (rtos en_son_toplam_agirlik 2 2) yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 2.5 Sat-3-01) 1 2)
(REN_Y P_013 (rtos en_son_toplam_agirlik 2 2) yzy1 sag (+ (* (+ 0.5 cap_adet) Sut-3-02) Sut-3-03 ) asagi (* 2.5 Sat-3-01) 1 2)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun RENAISSANCE_metraj_tablo_ciz_eng (yerlesim cap_liste toplam_bilgi olcek / yerlesim cap_liste toplam_bilgi olcek )
;
(command "_.Undo" "_BE")
(setq cap_adet (length cap_liste    ))
(setq poz_adet (length toplam_bilgi ))
(setq tip_bilgi_adet 0)
(setq dosya (open (strcat client_path "Standard/Tablo_Tipi.TXT") "r"))
(setq Yerlesim_1 (read-line dosya) )
(setq Yerlesim_2 (read-line dosya) )
(setq Yerlesim_3 (read-line dosya) )
(close dosya)
(setq i 0)
(while (< i poz_adet)
(if (= "00" (cdr (assoc 6 (nth i toplam_bilgi)))) (progn (setq tip_bilgi_adet (+ tip_bilgi_adet 1)) ))
(setq i (+ i 1))
)
(setq poz_tip_adet 0)
(setq i 0)
(while (< i poz_adet)
(if (/= "00" (cdr (assoc 6 (nth i toplam_bilgi)))) (setq poz_tip_adet (+ poz_tip_adet 1)))
(setq i (+ i 1))
)
;
(setq Sut-1-01 ( * olcek 1.70 ))
(setq Sut-1-02 ( * olcek 6.00 ))

(setq Sat-1-01 ( * olcek 0.80 ))
(setq Sat-1-02 ( * olcek 1.50 ))
;
(setq Sut-1-2  ( * olcek 0.80 ))
;
(setq Sut-2-01 ( * olcek 1.50 ))
(setq Sut-2-02 ( * olcek 6.00 ))
(setq Sut-2-03 ( * olcek 6.00 ))
(setq Sut-2-04 ( * olcek 1.50 ))
(setq Sut-2-05 ( * olcek 1.50 ))
(setq Sut-2-06 ( * olcek 2.00 ))

(setq Sat-2-01 ( * olcek 0.80 ))
(setq Sat-2-02 ( * olcek 1.50 ))
(setq Sat-2-03 ( * olcek 0.80 ))
;
(setq Sut-2-3  ( * olcek 0.80 ))
;
(setq Sut-3-01 ( * olcek 4.00 ))
(setq Sut-3-02 ( * olcek 1.50 ))
(setq Sut-3-03 ( * olcek 2.00 ))
;
(setq Sat-3-01 ( * olcek 0.80 ))
;
(setq yzy1     ( * olcek 0.30 ))
(setq yzy2     ( * olcek 0.30 ))
;
(setq cl_1  3  )                     ;Baslik yazilari rengi
(setq cl_2  3  )                     ;Baslik disinda kalan yazilarin rengi
(setq cl_3  3  )                     ;Ana cizgilerin rengi
(setq cl_4  8  )                     ;Diger cizgilerin rengi
;

;
(setq sag    (* 0.0 pi ) )
(setq sol    (* 1.0 pi ) )
(setq yukari (* 0.5 pi ) )
(setq asagi  (* 1.5 pi ) )
(if (= poz_adet tip_bilgi_adet)
    (progn (setq duseyadet 6) (setq yatayadet 7))
    (progn (setq duseyadet 5) (setq yatayadet 6))
)
(cond ((= Yerlesim_1 "1")
         (setq P_001 yerlesim)
         (setq P_003 (polar P_001 sag   (+ Sut-1-01 Sut-1-02 Sut-1-2 )))
         (setq P_006 (polar P_001 sag (+ Sut-1-01 Sut-1-02 Sut-1-2 Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06 Sut-2-3)))
      )
      ((= Yerlesim_2 "1")
         (setq P_003 yerlesim)
         (setq P_001 (polar P_003 asagi   (+ Sat-2-02 (/ Sat-1-01 2) (* Sat-2-03 (+ poz_adet duseyadet 1 )))))
         (setq P_006 (polar P_001 sag (+ Sut-1-01 Sut-1-02 (/ Sat-1-01 2)))))

      ((= Yerlesim_3 "1")
         (setq P_001 yerlesim)
         (setq P_003 (polar P_001 sag   (+ Sut-1-01 Sut-1-02 Sut-1-2 )))
         (setq P_0061 (polar P_003 asagi   (+ Sat-2-02 (/ Sat-1-01 2) (* Sat-2-03 (+ poz_adet duseyadet 1 )))))
         (setq P_006 (polar P_0061 sag   (- (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)))))
)
;
; Tablo - 1  ;
;
(setq P_002 (polar P_001 asagi Sat-1-01))
(if (= tip_bilgi_adet 0)
    (progn (setq Sutadet 2)(setq Satadet 1)) (progn (setq Sutadet 3)(setq Satadet 2)) )
;
(REN_cizgi P_001                                   sag   (+ Sut-1-01 Sut-1-02) 2 )
(REN_cizgi P_001                                   asagi    Sat-1-01           2 )
(REN_cizgi (polar P_001 sag (+ Sut-1-01 Sut-1-02)) asagi    Sat-1-01           2 )
(REN_cizgi P_002                                   sag   (+ Sut-1-01 Sut-1-02) 1 )
(REN_cizgi P_002                                   asagi (* Sat-1-02 (+ poz_tip_adet Satadet ))  1 )
(REN_cizgi (polar P_002 sag Sut-1-01)              asagi (* Sat-1-02 (+ poz_tip_adet 1 ))  1 )
(REN_cizgi (polar P_002 sag (+ Sut-1-01 Sut-1-02)) asagi (* Sat-1-02 (+ poz_tip_adet Satadet ))  1 )
(setq i 1)
(while (< i (+ poz_tip_adet Sutadet))
(REN_cizgi (polar P_002 asagi (* i Sat-1-02))      sag   (+ Sut-1-01 Sut-1-02) 1 )
(setq i (+ i 1))
)
;(REN_Y pt1 text height yon1 mesafe1 yon2 mesafe2 D_72 D_73 )
; 72  0 = Left      1 = Center  2 = Right
; 73  0 = Baseline  1 = Bottom  2 = Middle    3 = Top
; 0 cntr
; 1 POZ
; 2 adet
; 3 cap
; 4 aralik
; 5 BOY
; 6 TIP
; 7 A
; 8 B
; 9 C
;10 D
;11 E
;12 F
;13 R
;14 boy
;15 MALZEME
(REN_Y P_001 "Detail list" yzy2 sag (* 0.5 (+ Sut-1-01 Sut-1-02)) asagi (* 0.5 Sat-1-01) 1 2)
(REN_Y P_002 "Pos."  yzy2 sag (* 0.5 Sut-1-01) asagi (* 0.5 Sat-1-02) 1 2)
(REN_Y P_002 "Draft" yzy2 sag (+ Sut-1-01 (* 0.5 Sut-1-02)) asagi (* 0.5 Sat-1-02) 1 2)
(setq i 0)
(setq j 0)
(while (< i poz_adet)
(if (/= "00" (cdr (assoc 6 (nth i toplam_bilgi))))
  (progn
   (REN_Y P_002 (cdr (assoc 1 (nth i toplam_bilgi))) yzy2 sag (* 0.5 Sut-1-01) asagi (* (+ j 1.5) Sat-1-02) 1 2)
   (setq dwgname (strcat source_path "Standard/PZ_" (cdr (assoc 6 (nth i toplam_bilgi))) ".dwg" ))
   (if (not (findfile dwgname)) (setq dwgname (strcat source_path "Standard/PZ_99")))
   (setq dwgname_insert_point (polar P_002 sag (+ Sut-1-01 (* 0.5 Sut-1-02))))
   (setq dwgname_insert_point (polar dwgname_insert_point asagi (* (+ j 1.5) Sat-1-02)))
   (command ".-insert" dwgname dwgname_insert_point (/ Sut-1-02 450 ) (/ Sat-1-02 100 ) 0 )
   (setq bilgi_isle (entlast))
   (att_yaz bilgi_isle "A" (cdr (assoc   7 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "B" (cdr (assoc   8 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "C" (cdr (assoc   9 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "D" (cdr (assoc  10 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "E" (cdr (assoc  11 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "F" (cdr (assoc  12 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "R" (cdr (assoc  13 (nth i toplam_bilgi))))
   (setq j (+ j 1))
  )
)
   (setq i (+ i 1))
)
(if (and (/= poz_adet tip_bilgi_adet) (/= tip_bilgi_adet 0))
    (progn
(REN_Y2 dwgname_insert_point "Straight bars are conditionaly not presented in details list"  yzy1 0.65 sag (- (* 0.1 Sut-1-01) (+ Sut-1-01 (* 0.5 Sut-1-02))) asagi (* 1.00 Sat-1-02) 0 2)
))
;
; Tablo - 2  ;
;
(setq P_004 (polar P_003 asagi   Sat-2-01))
(setq P_004 (polar P_003 asagi   Sat-2-01))
;
(setq P_005 (polar P_004 asagi    Sat-2-02))
(setq P_106 (polar P_005 asagi  (* 2 Sat-2-03)))
(setq P_107 (polar P_106 sag (+ Sut-2-01 Sut-2-02 )))
(setq P_108 (polar P_106 sag (+ Sut-2-01 Sut-2-02 (* 0.30 Sut-2-03))))
(setq P_109 (polar P_106 sag (+ Sut-2-01 Sut-2-02 (* 0.60 Sut-2-03))))
(REN_cizgi P_003  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 2 )
(REN_cizgi P_003                                   asagi    Sat-2-01            2 )
(REN_cizgi (polar P_003 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi Sat-2-01 2 )
(REN_cizgi P_004  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(REN_cizgi P_005  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(setq i 1)
(while (< i (+ poz_adet yatayadet))
(REN_cizgi (polar P_005 asagi (* i Sat-2-03)) sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(setq i (+ i 1))
)
(REN_cizgi P_004                                                                       asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet duseyadet )))  1 )
(REN_cizgi (polar P_004 sag    Sut-2-01)                                               asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02))                                     asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03))                            asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi P_108  asagi (* Sat-2-03 poz_adet)  1 )
(REN_cizgi P_109  asagi (* Sat-2-03 poz_adet)  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04))                   asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05))          asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet duseyadet )))  1 )
(REN_Y P_003 "Reinforcement details specification" yzy2 sag (* 0.5 (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (* 0.5 Sat-2-01) 1 2)
(REN_Y P_004 "Pos."           yzy2 sag (* 0.5 Sut-2-01) asagi (* 0.5 Sat-2-02) 1 2)
(REN_Y P_004 "Designation"    yzy2 sag (+ Sut-2-01 (* 0.5 Sut-2-02)) asagi (* 0.5 Sat-2-02) 1 2)
(REN_Y P_004 "Name"           yzy2 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (* 0.5 Sat-2-02) 1 2)
(REN_Y P_004 "Q-ty"           yzy2 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (* 0.5 Sat-2-02) 1 2)
(REN_Y P_004 "W. of pc."      yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (* 0.5 Sat-2-02) 1 2)
(REN_Y P_004 "Notes"          yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (* 0.5 Sat-2-02) 1 2)
(REN_Y P_004 "..."        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 0.5 Sat-2-03)) 1 2)
(REN_Y P_004 "Details"        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 1.5 Sat-2-03)) 1 2)
(REN_Y P_004 "Material"       yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 3.5 ))) 1 2)
(REN_Y P_004 "Concrete"  yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "M\U+00B3"   yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "..."  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "..."  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
;
(if (= poz_adet tip_bilgi_adet)
    (progn
(REN_Y P_004 "Straight bars are conditionaly not presented in details list"  yzy1 sag (* 0.1 Sut-2-01) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5.5 ))) 0 2)
))
;
(setq i 0)
(while (< i poz_adet)
(REN_Y P_005 (cdr (assoc 1 (nth i toplam_bilgi)))  yzy1 sag (* 0.5 Sut-2-01) asagi (* (+ i 2.5) Sat-2-03) 1 2)
(REN_Y P_005 "GOST P 52544-2006"  yzy1 sag (+ Sut-2-01 (* 0.1 Sut-2-02)) asagi (* (+ i 2.5) Sat-2-03) 0 2)
(REN_Y P_107 (strcat "\\U+2300" (cdr (assoc  3 (nth i toplam_bilgi)))) yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_108             (cdr (assoc 15 (nth i toplam_bilgi)))  yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_109             (cdr (assoc  5 (nth i toplam_bilgi)))  yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_005 (cdr (assoc 2 (nth i toplam_bilgi)))  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.9 Sut-2-04)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(setq birim_agirlik (* 0.001 (fix (+ 0.5 (* 1000 (cp_ag (atof (cdr (assoc 3 (nth i toplam_bilgi))))))))))
(setq eleman_agirligi   (* olcu_carp (cdr (assoc 14 (nth i toplam_bilgi))) birim_agirlik))
(setq t_eleman_agirligi (* (atof (cdr (assoc  2 (nth i toplam_bilgi)))) eleman_agirligi))
(REN_Y P_005 (rtos eleman_agirligi   2 2) yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.9 Sut-2-05)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(REN_Y P_005 (rtos t_eleman_agirligi 2 2) yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.9 Sut-2-06)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(setq i (+ i 1))
)
;
; Tablo - 3  ;
;
(setq P_007 (polar P_006 asagi Sat-3-01))
(setq P_008 (polar P_007 asagi (* 5 Sat-3-01)))
(setq P_009 (polar P_008 asagi Sat-3-01))
(setq P_010 (polar P_007 sag   Sut-3-01))
(setq P_011 (polar P_010 asagi Sat-3-01))
(setq P_012 (polar P_011 asagi Sat-3-01))
(setq P_013 (polar P_012 asagi Sat-3-01))
(setq P_014 (polar P_013 asagi Sat-3-01))
(setq P_015 (polar P_014 asagi Sat-3-01))
(setq P_016 (polar P_015 asagi Sat-3-01))
(setq P_026 (polar P_011 sag   (* (+ 1 cap_adet) Sut-3-02) ))
(setq P_032 (polar P_006 sag   (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)))
(setq P_033 (polar P_032 asagi Sat-3-01))
(REN_cizgi P_006  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 2 )
(REN_cizgi P_006  asagi  Sat-3-01  2 )
(REN_cizgi P_032  asagi  Sat-3-01  2 )
(REN_cizgi P_007  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_007  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_010  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_033  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_008  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_009  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_011  sag (+          (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_012  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_013  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_014  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_026  asagi (* 5 Sat-3-01)  1 )
(setq i 1)
(while (< i (+ 1 cap_adet))
(setq P_014 (polar P_014 sag Sut-3-02))
(REN_cizgi P_014 asagi (* 2 Sat-3-01) 1)
(setq i (+ i 1))
)
(REN_Y P_006 "Steel Flow Specification, kg" yzy2 sag (* 0.5 (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_007 "Mark of Element"              yzy1 sag (* 0.5 Sut-3-01) asagi (* 2.5 Sat-3-01) 1 2)
(REN_Y P_008 "..."                          yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_010 "Reinforcement Goods"          yzy1 sag (* 0.5 (+ (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y2 P_011 "Class of reinforcement"       yzy1 0.73 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_012 (cdr (assoc 15 (nth 0 toplam_bilgi))) yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_013 "GOST P 52544-2006"            yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
(REN_Y P_026 "Total"                        yzy1 sag (* 0.5 Sut-3-03 ) asagi (* 2.0 Sat-3-01) 1 2)
(setq en_son_toplam_agirlik 0 )
(setq i 0)
(while (< i cap_adet)
(REN_Y P_013 (strcat "\\U+2300" (cdr (assoc 1 (nth i cap_liste)))) yzy1 sag (* (+ 0.5 i) Sut-3-02 ) asagi (* 1.5 Sat-3-01) 1 2)
(setq birim_agirlik (* 0.001 (fix (+ 0.5 (* 1000 (cp_ag (atof (cdr (assoc 1 (nth i cap_liste))))))))))
(REN_Y P_013 (rtos (* (* olcu_carp (atof (cdr (assoc 2 (nth i cap_liste))))) birim_agirlik) 2 2) yzy1 sag (* (+ 0.5 i) Sut-3-02 ) asagi (* 2.5 Sat-3-01) 1 2)
(setq en_son_toplam_agirlik (+ en_son_toplam_agirlik (* (* olcu_carp (atof (cdr (assoc 2 (nth i cap_liste))))) birim_agirlik)))
(setq i (+ i 1))
)
(REN_Y P_013 "Total" yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 1.5 Sat-3-01) 1 2)
(REN_Y P_013 (rtos en_son_toplam_agirlik 2 2) yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 2.5 Sat-3-01) 1 2)
(REN_Y P_013 (rtos en_son_toplam_agirlik 2 2) yzy1 sag (+ (* (+ 0.5 cap_adet) Sut-3-02) Sut-3-03 ) asagi (* 2.5 Sat-3-01) 1 2)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
; Rusca & Ingilizce tablo bolumu
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;
(defun RENAISSANCE_metraj_tablo_ciz_re (yerlesim cap_liste toplam_bilgi olcek / yerlesim cap_liste toplam_bilgi olcek )
;
(command "_.Undo" "_BE")
(setq cap_adet (length cap_liste    ))
(setq poz_adet (length toplam_bilgi ))
(setq tip_bilgi_adet 0)
(setq dosya (open (strcat client_path "Standard/Tablo_Tipi.TXT") "r"))
(setq Yerlesim_1 (read-line dosya) )
(setq Yerlesim_2 (read-line dosya) )
(setq Yerlesim_3 (read-line dosya) )
(close dosya)
(setq i 0)
(while (< i poz_adet)
(if (= "00" (cdr (assoc 6 (nth i toplam_bilgi)))) (progn (setq tip_bilgi_adet (+ tip_bilgi_adet 1)) ))
(setq i (+ i 1))
)
(setq poz_tip_adet 0)
(setq i 0)
(while (< i poz_adet)
(if (/= "00" (cdr (assoc 6 (nth i toplam_bilgi)))) (setq poz_tip_adet (+ poz_tip_adet 1)))
(setq i (+ i 1))
)
;
(setq Sut-1-01 ( * olcek 1.70 ))
(setq Sut-1-02 ( * olcek 6.00 ))

(setq Sat-1-01 ( * olcek 1.50 ))
(setq Sat-1-02 ( * olcek 1.50 ))
;
(setq Sut-1-2  ( * olcek 0.80 ))
;
(setq Sut-2-01 ( * olcek 1.50 ))
(setq Sut-2-02 ( * olcek 6.00 ))
(setq Sut-2-03 ( * olcek 6.00 ))
(setq Sut-2-04 ( * olcek 1.50 ))
(setq Sut-2-05 ( * olcek 1.50 ))
(setq Sut-2-06 ( * olcek 2.00 ))

(setq Sat-2-01 ( * olcek 0.80 ))
(setq Sat-2-02 ( * olcek 1.50 ))
(setq Sat-2-03 ( * olcek 0.80 ))
;
(setq Sut-2-3  ( * olcek 0.80 ))
;
(setq Sut-3-01 ( * olcek 4.00 ))
(setq Sut-3-02 ( * olcek 1.50 ))
(setq Sut-3-03 ( * olcek 2.00 ))
;
(setq Sat-3-01 ( * olcek 0.80 ))
;
(setq yzy1     ( * olcek 0.30 ))
(setq yzy2     ( * olcek 0.30 ))
;
(setq cl_1 3)  ;Baslik yazilari rengi
(setq cl_2 3)  ;Baslik disinda kalan yazilarin rengi
(setq cl_3 3)  ;Ana cizgilerin rengi
(setq cl_4 8)  ;Diger cizgilerin rengi
;

;
(setq sag    (* 0.0 pi ) )
(setq sol    (* 1.0 pi ) )
(setq yukari (* 0.5 pi ) )
(setq asagi  (* 1.5 pi ) )
(if (= poz_adet tip_bilgi_adet)
    (progn (setq duseyadet 6) (setq yatayadet 7))
    (progn (setq duseyadet 5) (setq yatayadet 6))
)
(cond ((= Yerlesim_1 "1")
         (setq P_001 yerlesim)
         (setq P_003 (polar P_001 sag   (+ Sut-1-01 Sut-1-02 Sut-1-2 )))
         (setq P_006 (polar P_001 sag (+ Sut-1-01 Sut-1-02 Sut-1-2 Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06 Sut-2-3)))
      )
      ((= Yerlesim_2 "1")
         (setq P_003 yerlesim)
         (setq P_001 (polar P_003 asagi   (+ Sat-2-02 (/ Sat-1-01 2) (* Sat-2-03 (+ poz_adet duseyadet 1 )))))
         (setq P_006 (polar P_001 sag (+ Sut-1-01 Sut-1-02 (/ Sat-1-01 2)))))

      ((= Yerlesim_3 "1")
         (setq P_001 yerlesim)
         (setq P_003 (polar P_001 sag   (+ Sut-1-01 Sut-1-02 Sut-1-2 )))
         (setq P_0061 (polar P_003 asagi   (+ Sat-2-02 (/ Sat-1-01 2) (* Sat-2-03 (+ poz_adet duseyadet 1 )))))
         (setq P_006 (polar P_0061 sag   (- (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)))))
)
;
; Tablo - 1  ;
;
(setq P_002 (polar P_001 asagi Sat-1-01))
(if (= tip_bilgi_adet 0)
    (progn (setq Sutadet 2)(setq Satadet 1)) (progn (setq Sutadet 3)(setq Satadet 2)) )
;
(REN_cizgi P_001                                   sag   (+ Sut-1-01 Sut-1-02) 2 )
(REN_cizgi P_001                                   asagi    Sat-1-01           2 )
(REN_cizgi (polar P_001 sag (+ Sut-1-01 Sut-1-02)) asagi    Sat-1-01           2 )
(REN_cizgi P_002                                   sag   (+ Sut-1-01 Sut-1-02) 1 )
(REN_cizgi P_002                                   asagi (* Sat-1-02 (+ poz_tip_adet Satadet ))  1 )
(REN_cizgi (polar P_002 sag Sut-1-01)              asagi (* Sat-1-02 (+ poz_tip_adet 1 ))  1 )
(REN_cizgi (polar P_002 sag (+ Sut-1-01 Sut-1-02)) asagi (* Sat-1-02 (+ poz_tip_adet Satadet ))  1 )
(setq i 1)
(while (< i (+ poz_tip_adet Sutadet))
(REN_cizgi (polar P_002 asagi (* i Sat-1-02))      sag   (+ Sut-1-01 Sut-1-02) 1 )
(setq i (+ i 1))
)
;(REN_Y pt1 text height yon1 mesafe1 yon2 mesafe2 D_72 D_73 )
; 72  0 = Left      1 = Center  2 = Right
; 73  0 = Baseline  1 = Bottom  2 = Middle    3 = Top
; 0 cntr
; 1 POZ
; 2 adet
; 3 cap
; 4 aralik
; 5 BOY
; 6 TIP
; 7 A
; 8 B
; 9 C
;10 D
;11 E
;12 F
;13 R
;14 boy
;15 MALZEME
;
;
(REN_Y P_001 "\\U+0412\\U+0435\\U+0434\\U+043E\\U+043C\\U+043E\\U+0441\\U+0442\\U+044C \\U+0434\\U+0435\\U+0442\\U+0430\\U+043B\\U+0435\\U+0439" yzy2 sag (* 0.5 (+ Sut-1-01 Sut-1-02)) asagi (* 0.25 Sat-1-01) 1 2)
(REN_Y P_001 "Detail list" yzy2 sag (* 0.5 (+ Sut-1-01 Sut-1-02)) asagi (* 0.75 Sat-1-01) 1 2)
;
(REN_Y P_002 "\\U+041F\\U+043E\\U+0437."  yzy2 sag (* 0.5 Sut-1-01) asagi (* 0.25 Sat-1-02) 1 2)
(REN_Y P_002 "Pos."  yzy2 sag (* 0.5 Sut-1-01) asagi (* 0.75 Sat-1-02) 1 2)
;
(REN_Y P_002 "\\U+042D\\U+0441\\U+043A\\U+0438\\U+0437" yzy2 sag (+ Sut-1-01 (* 0.5 Sut-1-02)) asagi (* 0.25 Sat-1-02) 1 2)
(REN_Y P_002 "Draft" yzy2 sag (+ Sut-1-01 (* 0.5 Sut-1-02)) asagi (* 0.75 Sat-1-02) 1 2)
(setq i 0)
(setq j 0)
(while (< i poz_adet)
(if (/= "00" (cdr (assoc 6 (nth i toplam_bilgi))))
  (progn
   (REN_Y P_002 (cdr (assoc 1 (nth i toplam_bilgi))) yzy2 sag (* 0.5 Sut-1-01) asagi (* (+ j 1.5) Sat-1-02) 1 2)
   (setq dwgname (strcat source_path "Standard/PZ_" (cdr (assoc 6 (nth i toplam_bilgi))) ".dwg" ))
   (if (not (findfile dwgname)) (setq dwgname (strcat source_path "Standard/PZ_99")))
   (setq dwgname_insert_point (polar P_002 sag (+ Sut-1-01 (* 0.5 Sut-1-02))))
   (setq dwgname_insert_point (polar dwgname_insert_point asagi (* (+ j 1.5) Sat-1-02)))
   (command ".-insert" dwgname dwgname_insert_point (/ Sut-1-02 450 ) (/ Sat-1-02 100 ) 0 )
   (setq bilgi_isle (entlast))
   (att_yaz bilgi_isle "A" (cdr (assoc   7 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "B" (cdr (assoc   8 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "C" (cdr (assoc   9 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "D" (cdr (assoc  10 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "E" (cdr (assoc  11 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "F" (cdr (assoc  12 (nth i toplam_bilgi))))
   (att_yaz bilgi_isle "R" (cdr (assoc  13 (nth i toplam_bilgi))))
   (setq j (+ j 1))
  )
)
   (setq i (+ i 1))
)
;
(if (and (/= poz_adet tip_bilgi_adet) (/= tip_bilgi_adet 0))
    (progn
(REN_Y4 dwgname_insert_point "\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043F\\U+043E\\U+0437.\\U+0020\\1-2\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+0432\\U+043D\\U+0443\\U+0442\\U+0440.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C.\\U+000D\\U+000A\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043E\\U+0441\\U+0442\\U+0430\\U+043B\\U+044C\\U+043D\\U+044B\\U+0445\\U+0020\\U+043F\\U+043E\\U+0437\\U+0438\\U+0446\\U+0438\\U+0439\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+043D\\U+0430\\U+0440\\U+0443\\U+0436.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C."  yzy1 0.65 sag (- (* 0.1 Sut-1-01) (+ Sut-1-01 (* 0.5 Sut-1-02))) asagi (* 0.75 Sat-1-02) 0 2)
(REN_Y2 dwgname_insert_point "Straight bars are conditionaly not presented in details list"  yzy1 0.65 sag (- (* 0.1 Sut-1-01) (+ Sut-1-01 (* 0.5 Sut-1-02))) asagi (* 1.15 Sat-1-02) 0 2)
))
;
; Tablo - 2  ;
;
(setq P_004 (polar P_003 asagi    Sat-2-01))
(setq P_004 (polar P_003 asagi    Sat-2-01))
;
(setq P_005 (polar P_004 asagi    Sat-2-02))
(setq P_106 (polar P_005 asagi  (* 2 Sat-2-03)))
(setq P_107 (polar P_106 sag (+ Sut-2-01 Sut-2-02 )))
(setq P_108 (polar P_106 sag (+ Sut-2-01 Sut-2-02 (* 0.30 Sut-2-03))))
(setq P_109 (polar P_106 sag (+ Sut-2-01 Sut-2-02 (* 0.60 Sut-2-03))))
;
(REN_cizgi P_003  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 2 )
(REN_cizgi P_003                                   asagi    Sat-2-01            2 )
(REN_cizgi (polar P_003 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi Sat-2-01 2 )
(REN_cizgi P_004  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(REN_cizgi P_005  sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
;
(setq i 1)
(while (< i (+ poz_adet yatayadet))
(REN_cizgi (polar P_005 asagi (* i Sat-2-03)) sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06) 1 )
(setq i (+ i 1))
)
;
(REN_cizgi P_004                                                                       asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet duseyadet )))  1 )
(REN_cizgi (polar P_004 sag    Sut-2-01)                                               asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02))                                     asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03))                            asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi P_108  asagi (* Sat-2-03 poz_adet)  1 )
(REN_cizgi P_109  asagi (* Sat-2-03 poz_adet)  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04))                   asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05))          asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5 )))  1 )
(REN_cizgi (polar P_004 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet duseyadet )))  1 )
;Reinforcement details specification \\U+0421\\U+043F\\U+0435\\U+0446\\U+0438\\U+0444\\U+0438\\U+043A\\U+0430\\U+0446\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0445 \\U+0438\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+0439
(REN_Y P_003 "\\U+0421\\U+043F\\U+0435\\U+0446\\U+0438\\U+0444\\U+0438\\U+043A\\U+0430\\U+0446\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0445 \\U+0438\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+0439" yzy2 sag (* 0.5 (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (* 0.25 Sat-2-01) 1 2)
(REN_Y P_003 "Reinforcement details specification" yzy2 sag (* 0.5 (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 Sut-2-06)) asagi (* 0.75 Sat-2-01) 1 2)
;
(REN_Y P_004 "\\U+041F\\U+043E\\U+0437."           yzy2 sag (* 0.5 Sut-2-01) asagi (* 0.25 Sat-2-02) 1 2)
(REN_Y P_004 "Pos."                                yzy2 sag (* 0.5 Sut-2-01) asagi (* 0.75 Sat-2-02) 1 2)
;
(REN_Y P_004 "\\U+041E\\U+0431\\U+043E\\U+0437\\U+043D\\U+0430\\U+0447\\U+0435\\U+043D\\U+0438\\U+0435"    yzy2 sag (+ Sut-2-01 (* 0.5 Sut-2-02)) asagi (* 0.25 Sat-2-02) 1 2)
(REN_Y P_004 "Designation"    yzy2 sag (+ Sut-2-01 (* 0.5 Sut-2-02)) asagi (* 0.75 Sat-2-02) 1 2)
;
(REN_Y P_004 "\\U+041D\\U+0430\\U+0438\\U+043C\\U+0435\\U+043D\\U+043E\\U+0432\\U+0430\\U+043D\\U+0438\\U+0435"           yzy2 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (* 0.25 Sat-2-02) 1 2)
(REN_Y P_004 "Name"           yzy2 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (* 0.75 Sat-2-02) 1 2)
;
(REN_Y P_004 "\\U+041A\\U+043E\\U+043B."           yzy2 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (* 0.25 Sat-2-02) 1 2)
(REN_Y P_004 "Q-ty"           yzy2 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (* 0.75 Sat-2-02) 1 2)
;
(REN_Y2 P_004 "\\U+041C\\U+0430\\U+0441\\U+0441\\U+0430 \\U+0435\\U+0434., \\U+043A\\U+0433" yzy1 0.6 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (* 0.25 Sat-2-02) 1 2)
(REN_Y P_004 "W. of pc."      yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (* 0.75 Sat-2-02) 1 2)
;
(REN_Y P_004 "\\U+041F\\U+0440\\U+0438\\U+043C\\U+0435\\U+0447\\U+0430\\U+043D\\U+0438\\U+0435"          yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (* 0.25 Sat-2-02) 1 2)
(REN_Y P_004 "Notes"          yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (* 0.75 Sat-2-02) 1 2)
;
(REN_Y P_004 "..."        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 0.25 Sat-2-03)) 1 2)
(REN_Y P_004 "..."        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 0.75 Sat-2-03)) 1 2)
;
(REN_Y P_004 "%%U\\U+0421\\U+0442\\U+0435\\U+0440\\U+0436\\U+043D\\U+0438"        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 1.25 Sat-2-03)) 1 2)
(REN_Y P_004 "Details"        yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* 1.75 Sat-2-03)) 1 2)
;
(REN_Y P_004 "\\U+041C\\U+0430\\U+0442\\U+0435\\U+0440\\U+0438\\U+0430\\U+043B"       yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 3.25 ))) 1 2)
(REN_Y P_004 "Material"       yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 3.75 ))) 1 2)
;
(REN_Y P_004 "\\U+0411\\U+0435\\U+0442\\U+043E\\U+043D"  yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.25 ))) 1 2)
(REN_Y P_004 "Concrete"  yzy1 sag (+ Sut-2-01 Sut-2-02 (* 0.5 Sut-2-03)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.75 ))) 1 2)
(REN_Y P_004 "M\U+00B3"   yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.5 Sut-2-04)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "..."  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.5 Sut-2-05)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
(REN_Y P_004 "..."  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.5 Sut-2-06)) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 4.5 ))) 1 2)
;
;
;
;
;
(if (= poz_adet tip_bilgi_adet)
(progn
(REN_Y3 P_004 "\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043F\\U+043E\\U+0437.\\U+0020\\1-2\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+0432\\U+043D\\U+0443\\U+0442\\U+0440.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C.\\U+000D\\U+000A\\U+0420\\U+0430\\U+0437\\U+043C\\U+0435\\U+0440\\U+044B\\U+0020\\U+043E\\U+0441\\U+0442\\U+0430\\U+043B\\U+044C\\U+043D\\U+044B\\U+0445\\U+0020\\U+043F\\U+043E\\U+0437\\U+0438\\U+0446\\U+0438\\U+0439\\U+0020\\U+0434\\U+0430\\U+043D\\U+044B\\U+0020\\U+043F\\U+043E\\U+0020\\U+043D\\U+0430\\U+0440\\U+0443\\U+0436.\\U+0020\\U+0433\\U+0440\\U+0430\\U+043D\\U+044F\\U+043C."  yzy1 sag (* 0.1 Sut-2-01) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5.25 ))) 0 2)
(REN_Y P_004 "Straight bars are conditionaly not presented in details list"  yzy1 sag (* 0.1 Sut-2-01) asagi (+ Sat-2-02 (* Sat-2-03 (+ poz_adet 5.75 ))) 0 2)
))
;
;
;
;
;
(setq i 0)
(while (< i poz_adet)
(REN_Y P_005 (cdr (assoc 1 (nth i toplam_bilgi)))  yzy1 sag (* 0.5 Sut-2-01) asagi (* (+ i 2.5) Sat-2-03) 1 2)
;
(REN_Y P_005 "\\U+0413\\U+041E\\U+0421\\U+0422 \\U+0420  52544-2006"  yzy1 sag (+ Sut-2-01 (* 0.1 Sut-2-02)) asagi (* (+ i 2.25) Sat-2-03) 0 2)
(REN_Y P_005 "GOST P 52544-2006"  yzy1 sag (+ Sut-2-01 (* 0.1 Sut-2-02)) asagi (* (+ i 2.75) Sat-2-03) 0 2)
;
; Ø \\U+0428
(REN_Y P_107 (strcat "\\U+2300" (cdr (assoc  3 (nth i toplam_bilgi)))) yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_108             (cdr (assoc 15 (nth i toplam_bilgi)))  yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_109             (cdr (assoc  5 (nth i toplam_bilgi)))  yzy1 sag (* 0.05 Sut-2-03) asagi (* (+ i 0.5) Sat-2-03) 0 2)
(REN_Y P_005 (cdr (assoc 2 (nth i toplam_bilgi)))  yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 (* 0.9 Sut-2-04)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(setq birim_agirlik (* 0.001 (fix (+ 0.5 (* 1000 (cp_ag (atof (cdr (assoc 3 (nth i toplam_bilgi))))))))))
(setq eleman_agirligi   (* olcu_carp (cdr (assoc 14 (nth i toplam_bilgi))) birim_agirlik))
(setq t_eleman_agirligi (* (atof (cdr (assoc  2 (nth i toplam_bilgi)))) eleman_agirligi))
(REN_Y P_005 (rtos eleman_agirligi   2 2) yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 (* 0.9 Sut-2-05)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(REN_Y P_005 (rtos t_eleman_agirligi 2 2) yzy1 sag (+ Sut-2-01 Sut-2-02 Sut-2-03 Sut-2-04 Sut-2-05 (* 0.9 Sut-2-06)) asagi (* (+ i 2.5) Sat-2-03) 2 2)
(setq i (+ i 1))
)
;
; Tablo - 3  ;
;
(setq P_007 (polar P_006 asagi Sat-3-01))
(setq P_008 (polar P_007 asagi (* 5 Sat-3-01)))
(setq P_009 (polar P_008 asagi Sat-3-01))
(setq P_010 (polar P_007 sag   Sut-3-01))
(setq P_011 (polar P_010 asagi Sat-3-01))
(setq P_012 (polar P_011 asagi Sat-3-01))
(setq P_013 (polar P_012 asagi Sat-3-01))
(setq P_014 (polar P_013 asagi Sat-3-01))
(setq P_015 (polar P_014 asagi Sat-3-01))
(setq P_016 (polar P_015 asagi Sat-3-01))
(setq P_026 (polar P_011 sag   (* (+ 1 cap_adet) Sut-3-02) ))
(setq P_032 (polar P_006 sag   (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)))
(setq P_033 (polar P_032 asagi Sat-3-01))

(REN_cizgi P_006  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 2 )
(REN_cizgi P_006  asagi  Sat-3-01  2 )
(REN_cizgi P_032  asagi  Sat-3-01  2 )
(REN_cizgi P_007  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_007  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_010  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_033  asagi (* 6 Sat-3-01)  1 )
(REN_cizgi P_008  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_009  sag (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_011  sag (+          (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03) 1 )
(REN_cizgi P_012  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_013  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_014  sag             (* (+ 1 cap_adet) Sut-3-02           ) 1 )
(REN_cizgi P_026  asagi (* 5 Sat-3-01)  1 )
(setq i 1)
(while (< i (+ 1 cap_adet))
(setq P_014 (polar P_014 sag Sut-3-02))
(REN_cizgi P_014 asagi (* 2 Sat-3-01) 1)
(setq i (+ i 1))
)
;
(REN_Y P_006 "\\U+0412\\U+0435\\U+0434\\U+043E\\U+043C\\U+043E\\U+0441\\U+0442\\U+044C \\U+0440\\U+0430\\U+0441\\U+0445\\U+043E\\U+0434\\U+0430 \\U+0441\\U+0442\\U+0430\\U+043B\\U+0438\\U+002C \\U+043A\\U+0433" yzy2 sag (* 0.5 (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.3 Sat-3-01) 1 2)
(REN_Y P_006 "Steel Flow Specification, kg" yzy2 sag (* 0.5 (+ Sut-3-01 (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.7 Sat-3-01) 1 2)
;
(REN_Y P_007 "\\U+041C\\U+0430\\U+0440\\U+043A\\U+0430 \\U+044D\\U+043B\\U+0435\\U+043C\\U+0435\\U+043D\\U+0442\\U+0430"              yzy1 sag (* 0.5 Sut-3-01) asagi (* 2.3 Sat-3-01) 1 2)
(REN_Y P_007 "Mark of Element"              yzy1 sag (* 0.5 Sut-3-01) asagi (* 2.7 Sat-3-01) 1 2)
;
;(REN_Y P_008 "\\U+041F\\U+043B\\U+0438\\U+0442\\U+0430 \\U+041F\\U+043C"                      yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.3 Sat-3-01) 1 2)
;(REN_Y P_008 "Column"                      yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.7 Sat-3-01) 1 2)
(REN_Y P_008 "..."  yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.3 Sat-3-01) 1 2)
(REN_Y P_008 "..."  yzy1 sag (* 0.5 Sut-3-01) asagi (* 0.7 Sat-3-01) 1 2)
;
(REN_Y P_010 "\\U+0418\\U+0437\\U+0434\\U+0435\\U+043B\\U+0438\\U+044F \\U+0430\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+043D\\U+044B\\U+0435"          yzy1 sag (* 0.5 (+ (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.3 Sat-3-01) 1 2)
(REN_Y P_010 "Reinforcement Goods"          yzy1 sag (* 0.5 (+ (* (+ 1 cap_adet) Sut-3-02 ) Sut-3-03)) asagi (* 0.7 Sat-3-01) 1 2)
;
(REN_Y P_011 "\\U+0410\\U+0440\\U+043C\\U+0430\\U+0442\\U+0443\\U+0440\\U+0430 \\U+043A\\U+043B\\U+0430\\U+0441\\U+0441\\U+0430"       yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.3 Sat-3-01) 1 2)
(REN_Y2 P_011 "Class of reinforcement"       yzy1 0.73 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 )  asagi (* 0.7 Sat-3-01) 1 2)
(REN_Y P_012 (cdr (assoc 15 (nth 0 toplam_bilgi))) yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.5 Sat-3-01) 1 2)
;
(REN_Y P_013 "\\U+0413\\U+041E\\U+0421\\U+0422 \\U+0420  52544-2006"            yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.3 Sat-3-01) 1 2)
(REN_Y P_013 "GOST P 52544-2006"            yzy1 sag (* 0.5 (+ 1 cap_adet) Sut-3-02 ) asagi (* 0.7 Sat-3-01) 1 2)
;
(REN_Y P_026 "\\U+0412\\U+0441\\U+0435\\U+0433\\U+043E"                        yzy1 sag (* 0.5 Sut-3-03 ) asagi (* 1.8 Sat-3-01) 1 2)
(REN_Y P_026 "Total"                        yzy1 sag (* 0.5 Sut-3-03 ) asagi (* 2.2 Sat-3-01) 1 2)
(setq en_son_toplam_agirlik 0 )
(setq i 0)
(while (< i cap_adet)
;
; Ø \\U+0428
(REN_Y P_013 (strcat "\\U+2300" (cdr (assoc 1 (nth i cap_liste)))) yzy1 sag (* (+ 0.5 i) Sut-3-02 ) asagi (* 1.5 Sat-3-01) 1 2)
(setq birim_agirlik (* 0.001 (fix (+ 0.5 (* 1000 (cp_ag (atof (cdr (assoc 1 (nth i cap_liste))))))))))
(REN_Y P_013 (rtos (* (* olcu_carp (atof (cdr (assoc 2 (nth i cap_liste))))) birim_agirlik) 2 2) yzy1 sag (* (+ 0.5 i) Sut-3-02 ) asagi (* 2.5 Sat-3-01) 1 2)
(setq en_son_toplam_agirlik (+ en_son_toplam_agirlik (* (* olcu_carp (atof (cdr (assoc 2 (nth i cap_liste))))) birim_agirlik)))
(setq i (+ i 1))
)
;
(REN_Y P_013 "\\U+0418\\U+0442\\U+043E\\U+0433\\U+043E" yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 1.3 Sat-3-01) 1 2)
(REN_Y P_013 "Total" yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 1.7 Sat-3-01) 1 2)
(REN_Y P_013 (rtos en_son_toplam_agirlik 2 2) yzy1 sag (* (+ 0.5 cap_adet) Sut-3-02 ) asagi (* 2.5 Sat-3-01) 1 2)
(REN_Y P_013 (rtos en_son_toplam_agirlik 2 2) yzy1 sag (+ (* (+ 0.5 cap_adet) Sut-3-02) Sut-3-03 ) asagi (* 2.5 Sat-3-01) 1 2)
(princ)
)







;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun RENAISSANCE_toplam_bilgi_olustur ( eset / )
;---------------------------------------------------------------------------------------
; Bu kisim metraji cikarilacak elemanlardan bilgilerin toplanmasi icindir.
; Daha sonraki gelistirmeler sirasinda bu bolumde toplanan bilgilerin uygun
; standartta olmasi ve bu bilgilerin farkli kaynaklara adaptasyonu icin
; bu bolumde gelistirme yapilmalidir.
;
(setq toplam_bilgi (list))
(setq cntr 0)
(while(< cntr (sslength eset))
(setq en (ssname eset cntr))
(setq grup_carpani (a_oku en "GC" ))
(if (= grup_carpani "") (setq grup_carpani "1"))
(setq gc_deger (atoi grup_carpani))
;
(setq ok_bilgi (list (cons  0  cntr  )           ;Metrajin cikarilmasi esnasinda sira numarasi veriliyor.
                     (cons  1 (a_oku en "POZ"))
                     (cons  2 (rtos (* gc_deger (atof (adet_oku_isle (a_oku en "TB" )))) 2 0 ))
                     (cons  3 (cap_oku_isle    (a_oku en "TB" )))
                     (cons  4 (aralik_oku_isle (a_oku en "TB" )))
                     (cons  5 (a_oku en "BOY" ))
                     (cons  6 (a_oku en "TIP" ))
                     (cons  7 (a_oku en "A"   ))
                     (cons  8 (a_oku en "B"   ))
                     (cons  9 (a_oku en "C"   ))
                     (cons 10 (a_oku en "D"   ))
                     (cons 11 (a_oku en "E"   ))
                     (cons 12 (a_oku en "F"   ))
                     (cons 13 (a_oku en "R"   ))
                     (cons 14 (atoi (boy_oku_isle (a_oku en "BOY" ))))
                     (cons 15 (a_oku en "MALZEME" ))

                                                                       ))
(setq toplam_bilgi (cons ok_bilgi toplam_bilgi))
(setq cntr(+ cntr 1))
)
(setq toplam_bilgi (vl-sort toplam_bilgi (function (lambda (e1 e2)      (< (atoi (cdr (assoc  1 e1))) (atoi (cdr (assoc  1 e2))))))))
(setq toplam_bilgi (vl-sort toplam_bilgi (function (lambda (e1 e2) (and (= (atoi (cdr (assoc  1 e1))) (atoi (cdr (assoc  1 e2))))
                                                                        (< (atoi (cdr (assoc  3 e1))) (atoi (cdr (assoc  3 e2)))))))))
(setq toplam_bilgi (vl-sort toplam_bilgi (function (lambda (e1 e2) (and (= (atoi (cdr (assoc  1 e1))) (atoi (cdr (assoc  1 e2))))
                                                                        (= (atoi (cdr (assoc  3 e1))) (atoi (cdr (assoc  3 e2))))
                                                                        (<       (cdr (assoc 14 e1))        (cdr (assoc 14 e2))))))))

; Bu kýsým metrajý çýkarýlacak elemanlardan bilgilerin toplanmasý içindir.
; Daha sonraki geliþtirmeler sýrasýnda bu bölümde toplanan bilgilerin uygun
; standartta olmasý ve bu bilgilerin farklý kaynaklara adaptasyonu için
; bu bölümde geliþtirme yapýlmalýdýr.
;--------------------------------------------------------------------------------------------------------------------------
toplam_bilgi
)
(defun RENAISSANCE_toplam_bilgi_derle ( toplam_bilgi / )
;---------------------------------------------------------------------------------------
; Bu kýsým toplanmýþ bilginin derlenmesi ve gruplandýrýlmasý için ilerde geliþtirilecek.
; Toplam bilgi için her poza ait toplam adetin çýkarýlmasý iþi burada yapýlýyor.
; Buradan çýkan liste metraj tablosunun düþey satýrlarýný oluþturacak.
;
(setq i 0)
(setq bilgi_adet (length toplam_bilgi))
(while(< i bilgi_adet)
(if (and (= (cdr (assoc 1 (nth i toplam_bilgi))) (cdr (assoc 1 (nth (+ i 1) toplam_bilgi))))
         (= (cdr (assoc 3 (nth i toplam_bilgi))) (cdr (assoc 3 (nth (+ i 1) toplam_bilgi))))
         (= (cdr (assoc 5 (nth i toplam_bilgi))) (cdr (assoc 5 (nth (+ i 1) toplam_bilgi)))))
(progn
; Poz ayný ise  "
(setq adet_topla (itoa (+ (atoi (cdr (assoc 2 (nth i toplam_bilgi)))) (atoi (cdr (assoc 2 (nth (+ i 1) toplam_bilgi)))))))
(setq toplam_bilgi (subst (subst (cons 2 adet_topla) (assoc 2 (nth i toplam_bilgi)) (nth i toplam_bilgi)) (nth i toplam_bilgi) toplam_bilgi))
(setq toplam_bilgi (vl-remove (nth (+ i 1) toplam_bilgi) toplam_bilgi))
(setq bilgi_adet (- bilgi_adet 1))
(setq i (- i 1))
)
; Poz farklý ise "
)
(setq i (+ i 1))
)
;
; Bu kýsým toplanmýþ bilginin derlenmesi ve gruplandýrýlmasý için ilerde geliþtirilecek.
; Toplam bilgi için he poza ait toplam adetin çýkarýlmasý iþi burada yapýlýyor.
; Buradan çýkan liste metraj tablosunun düþey satýrlarýný oluþturacak.
;---------------------------------------------------------------------------------------
toplam_bilgi
)

;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:RQT ( /
a_max
a_min
adet
adet_genislik
adet_isle_1
anlik_tarih
aralik_b
asagi
att
atts
B
b_max
b_min
b_yeri
bilgi_adet
bilgi_isle
birim_agirlik
blk
boy
boy_
boy_genislik
boy_hesap
boy_hesap_max
boy_hesap_min
boy_isle_1
boy_isle_2
c_max
c_min
c_tipi
cap
cap_adet
cap_ekle
cap_eski
cap_genislik
cap_liste
CARP_A
carp_adet_genislik
CARP_B
carp_boy_genislik
CARP_BR
CARP_C
CARP_cap
carp_cap_genislik
CARP_D
carp_dn_satir_yukseklik
CARP_E
CARP_F
carp_ozet_yukseklik
carp_poz_genislik
carp_poz_yukseklik
CARP_r
carp_sekil_genislik
carp_t_boy_genislik
carp_toplam_genislik
carp_toplam_yukseklik
cizgi_layer
cizgi_tipi
clayer_old
cl_1
cl_2
cl_3
cl_4
cntr
color
D_72
D_73
d_max
d_min
data
deger
degis_bilgi
degis_bilgi_e
dimzin_old
dn_satir_yukseklik
dosya
dosya_adi
dwgname
e_max
e_min
e1
e2
eleman
en
en_son_toplam_agirlik_yaz
Entity
eset
eset_once
eski_adet
eski_aralik
eski_deger
f_max
f_min
fi
fi_boyu
fi_yeri
font_name
gc_deger
grup_carpani
height
i
id
j
kot_yazi
L
l1
layer
layer_name
layer_s
line_type
metraj_dosya
n
nokta1
nokta2
olcek
olcu_carp
old_osmode
origin
ozet_yukseklik
pn
poz_adet
poz_genislik
poz_icin_oku
poz_siralanacak_liste
poz_yukseklik
ps
PT001
PT002
PT003
PT004
PT005
PT006
PT007
PT008
PT009
PT010
PT011
PT012
PT013
PT014
PT015
PT016
PT017
PT018
PT019
PT020
PT021
PT022
PT023
PT024
PT025
PT026
PT027
PT028
PT029
PT030
PT031
PT032
PT033
PT034
PT035
PT036
PT037
PT038
PT039
PT040
PT041
PT042
PT043
PT044
PT045
PT046
PT047
PT048
PT049
PT050
PT051
PT052
PT053
PT054
PT055
PT056
PT057
PT058
PT059
PT060
pt1
pv
r
s1
s2
sag
sekil_genislik
sira
sol
stil
stil_name
t_boy_genislik
tag
tb
tb_boyu
text
tip
toplam_bilgi
toplam_boy
toplam_genislik
toplam_yukseklik
tutma
with
withfactor
y_tipi
yazi_yuk
yazilacak1
yazilacak2
yazilacak3
yazilacak4
yazilacak5
yeni_adet
yeni_deger
yerlesim
yukari
yzy1
yzy1
yzy1
yzy2
              )
(vl-load-com)
(setq osmode_old (getvar "osmode"))
(setq dimzin_old (getvar "dimzin"))
(setq clayer_old (getvar "clayer"))
(ayarlari_dosyadan_oku_Tablo)
(setvar "cmdecho" 0 )
(setvar "dimzin"  1 )
(setvar "attreq"  0 )
(RENAISSANCE_metraj_layer_olustur)
(setvar "clayer" "ren.mtr.bar" )
(setq olcek     (* 0.0100 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq olcu_carp (/ 1 (BIRIM_OKU)))
(setq yazi_yuk  (* 0.0025 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq eset (ssget (list (cons 0 "INSERT") (cons 2  "RL-POS")))) ; Metraji cikarilacak elemanlar seciliyor.
(if (= eset nil) (progn (terpri) (princ "Hiç bir eleman seçilmedi...") (exit)))
(setvar "osmode"  0 )
(setq toplam_bilgi (RENAISSANCE_toplam_bilgi_olustur eset )) ; Bilgiler toplaniyor
;
(hata_kontrol toplam_bilgi) ; Hata kontrolu yapiliyor
;
(setq toplam_bilgi (RENAISSANCE_toplam_bilgi_derle toplam_bilgi )) ; Bilgiler derleniyor
(setq cap_liste    (cap_bilgi_olustur  toplam_bilgi )) ; Donati caplari ve toplam boylari derleniyor
(setvar "osmode" osmode_old)
(setq yerlesim (getpoint "\n Yerleþim noktasý...    "))
(setvar "osmode"  0 )
(cond
 ((= (TABLO_DILI) "eng" ) ( RENAISSANCE_metraj_tablo_ciz_eng yerlesim cap_liste toplam_bilgi olcek ) )
 ((= (TABLO_DILI) "re"  ) ( RENAISSANCE_metraj_tablo_ciz_re  yerlesim cap_liste toplam_bilgi olcek ) )
 (T                       ( RENAISSANCE_metraj_tablo_ciz_rus yerlesim cap_liste toplam_bilgi olcek ) )
)
; Hiçbiri yoksa rusça çizsin....

;(terpri)
;(princ "   Metraji Excel dosyasina yazmiyoruz...!")
;(terpri)
;(metraj_tablo_dosya_yaz    cap_liste toplam_bilgi      )
;
(setvar "osmode" osmode_old)
(setvar "clayer" clayer_old)
(setvar "dimzin" dimzin_old)
(setvar "cmdecho" 1)
(princ)
)
;-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
(setq donati_maksimum_boyu 11.70)
;

