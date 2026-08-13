;*******************************************************************************
( defun c:kot (/)
(vl-load-com)
(if (= (getvar "ATTREQ") 1) (setvar "ATTREQ" 0))
(setq blipmode_old (getvar "blipmode"))
(setq cmdecho_old  (getvar "cmdecho" ))
(setq clayer_old   (getvar "clayer"  ))
(setq luprec_old   (getvar "luprec"  ))
(setq osmode_old   (getvar "osmode"  ))
(setq dimzin_old   (getvar "dimzin"  ))
;
(setvar "luprec"  3)
(SETQ OL (* 0.01 (OLCEK_OKU)))
(SETQ oranlama (GETVAR "DIMLFAC"))
(SETQ D_b (* 0.17 OL )) ; 0.16
(SETQ D_h (* 0.32 OL )) ; 0.22
(setq kot_birim_olcek (BIRIM_OKU))     ;  Çizim ölçeði mm ÇALIÞIRKEN 1000 CM ÇALIÞIRKEN 100
(if (or (= ayar_kot_eksen "x")(= ayar_kot_eksen "X")) (setq kotacisi 90) (setq kotacisi 0))
;( yeni_layer_ekle "KOD_C"   5  ""        )
;( yeni_layer_ekle "KOD_D"   7  ""        )
;(COMMAND  "-STYLE"  "OLCU"  "c:/depo/lsp/simplx.SHX"  0.0  0.70  0.0  "N" "N" "N")
;(setvar "TEXTSTYLE" "OLCU" )
(setvar "osmode"   691)
(SETVAR "DIMZIN"     1 )
;
(setq pt1           (getpoint  "\n Nokta belirleyiniz ....:"))
(if (= pt1 nil )
      (progn
                   (setq ayar_nokta     (getpoint "\n Koordinat ayarý için kotu bilinen bir nokta belirleyiniz.."))
                   (setq ayar_kot_deger (getreal  "\n Bu noktanýn kotunu giriniz.....................< 0 >......:"))
                   (initget 1 "Y X")
                   (setq ayar_kot_eksen (getstring "\n Koordinatlar hangi eksenden alýnacak (Y X).....< Y >.....:"))
                   (if (or (= ayar_kot_eksen "x")(= ayar_kot_eksen "X")) (setq kotacisi 90) (setq kotacisi 0))
          (setq x_ayr_kor (car      ayar_nokta   ))
          (setq y_ayr_kor (car (cdr ayar_nokta ) ))
          (setq pt1            (getpoint  "\n Nokta belirleyiniz ....:"))
      )
)
(if (= ayar_kot_deger nil) (setq ayar_kot_deger 0))
(setq x_kor (car      pt1  ))
(setq y_kor (car (cdr pt1) ))
(if (= x_ayr_kor  nil) (setq x_ayr_kor      0))
(if (= y_ayr_kor  nil) (setq y_ayr_kor      0))
(setq kot_x (- x_ayr_kor  x_kor  ))
(setq kot_y (- y_kor  y_ayr_kor  ))
(setq kot_x (+ (* oranlama (/ kot_x kot_birim_olcek)) ayar_kot_deger))
(setq kot_Y (+ (* oranlama (/ kot_y kot_birim_olcek)) ayar_kot_deger))
(if (= kot_birim_olcek 1000) (setq kusurat 3) (setq kusurat 2))
(if (or (= ayar_kot_eksen "x")(= ayar_kot_eksen "X"))
           (setq kot_yazi (rtos kot_x 2 kusurat ))
           (setq kot_yazi (rtos kot_y 2 kusurat ))
)
;
 (setq pt2  (list (+  x_kor       0.0  ) (+ y_kor        D_h  ) 0))
 (setq pt3  (list (+  x_kor       D_b  ) (+ y_kor        D_h  ) 0))
 (setq pt4  (list (+  x_kor (* -1 D_b )) (+ y_kor        D_h  ) 0))
 (setq pt5  (list (+  x_kor (* -0.06 ol))(+ y_kor (*  1.2 D_h)) 0))
 (setq pt6  (list (-  x_kor (* 1.2 D_h ))(+ y_kor (* -0.06 ol)) 0))
;
(if (> (atof kot_yazi) 0) (setq kot_yazi (strcat "+"   kot_yazi)))
(if (= (atof kot_yazi) 0) (setq kot_yazi (strcat "%%p" kot_yazi)))
;
;(setvar "osmode"  0)
;(layer_e_gec "KOD_D" "7" "")
;(if (= kotacisi 0 )
;(command "text" pt5 (* 0.25 ol)  "0" kot_yazi )
;(command "text" pt6 (* 0.25 ol) "90" kot_yazi )
;)
(layer_yap "ren.elevation"  "3" "continuous")
(command "clayer" "ren.elevation")
(command "-insert" (strcat source_pathlisp"Ayar/Ren.ElevM.dwg") pt1 (* 1 OL) "" kotacisi)
;(command "_.explode" (entlast))
(setq block  (vlax-ename->vla-object (entlast)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "OTM" (strcase (vla-get-TagString attrib))) (progn (vla-put-TextString attrib kot_yazi) kot_yazi)) )
;
(setvar "blipmode" blipmode_old )
(setvar "cmdecho"  cmdecho_old  )
(setvar "clayer"   clayer_old   )
(setvar "luprec"   luprec_old   )
(setvar "osmode"   osmode_old   )
(setvar "dimzin"   dimzin_old   )
(if (= (getvar "ATTREQ") 0) (setvar "ATTREQ" 1))
)




;*******************************************************************************
( defun kotyaz (pt1 ayar_nokta ayar_kot_deger / pt1 ayar_nokta ayar_kot_deger)
(vl-load-com)
(if (= (getvar "ATTREQ") 1) (setvar "ATTREQ" 0))
(setq blipmode_old (getvar "blipmode"))
(setq cmdecho_old  (getvar "cmdecho" ))
(setq clayer_old   (getvar "clayer"  ))
(setq luprec_old   (getvar "luprec"  ))
(setq osmode_old   (getvar "osmode"  ))
(setq dimzin_old   (getvar "dimzin"  ))
;
(setvar "luprec"  3)
(SETQ OL (* 0.01 25))
(SETQ oranlama (GETVAR "DIMLFAC"))
(SETQ D_b (* 0.17 OL )) ; 0.16
(SETQ D_h (* 0.32 OL )) ; 0.22
(setq kot_birim_olcek (BIRIM_OKU))     ;  Çizim ölçeði mm ÇALIÞIRKEN 1000 CM ÇALIÞIRKEN 100
(if (or (= ayar_kot_eksen "x")(= ayar_kot_eksen "X")) (setq kotacisi 90) (setq kotacisi 0))
(setvar "osmode"   0)
(SETVAR "DIMZIN"     1 )
;

                   (setq kotacisi 0)

          (setq y_ayr_kor (car (cdr ayar_nokta ) ))



(if (= ayar_kot_deger nil) (setq ayar_kot_deger 0))

(setq y_kor (car (cdr pt1) ))

(if (= y_ayr_kor  nil) (setq y_ayr_kor      0))

(setq kot_y (- y_kor  y_ayr_kor  ))

(setq kot_Y (+ (* oranlama (/ kot_y kot_birim_olcek)) ayar_kot_deger))
(if (= kot_birim_olcek 1000) (setq kusurat 3) (setq kusurat 2))
           (setq kot_yazi (rtos kot_y 2 kusurat ))

(if (> (atof kot_yazi) 0) (setq kot_yazi (strcat "+"   kot_yazi)))
(if (= (atof kot_yazi) 0) (setq kot_yazi (strcat "%%p" kot_yazi)))

(layer_yap "ren.elevation"  "3" "continuous")
(command "clayer" "ren.elevation")
(command "-insert" (strcat source_pathlisp"Ayar/Ren.ElevM.dwg") pt1 (* 1 OL) "" kotacisi)
(setq block  (vlax-ename->vla-object (entlast)))
(setq block1 (entlast))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "OTM" (strcase (vla-get-TagString attrib))) (progn (vla-put-TextString attrib kot_yazi) kot_yazi)) )
(command "_.mirror" block1 "" (polar pt1 (* 0.5 pi) 1000) (polar pt1 (* 1.5 pi) 1000) "Y")

(setvar "blipmode" blipmode_old )
(setvar "cmdecho"  cmdecho_old  )
(setvar "clayer"   clayer_old   )
(setvar "luprec"   luprec_old   )
(setvar "osmode"   osmode_old   )
(setvar "dimzin"   dimzin_old   )
(if (= (getvar "ATTREQ") 0) (setvar "ATTREQ" 1))
)
