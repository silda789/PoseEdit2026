;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;----- DAÝRESEL TEMEL YAY ÞEKLÝNDEKÝ DONATILARI --------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
(defun daire_donatisi_ayarla ( pt1 yaricap tam_boy bindirme / )
(setq blipmode_old (getvar "blipmode"))
(setq cmdecho_old  (getvar "cmdecho" ))
(setq clayer_old   (getvar "clayer"  ))
(setq luprec_old   (getvar "luprec"  ))
(setq osmode_old   (getvar "osmode"  ))
;
(setvar "osmode"   0   )
(setq cevre       (* yaricap 2 pi))
(setq derece_icin (/ cevre (* 2 pi)))
(setq asil_kisim_aci (/ (- tam_boy bindirme) derece_icin))
(setq bindirme_aci   (/            bindirme  derece_icin))
(setq tam_boy_aci    (/            tam_boy   derece_icin))
(setq aci_sargi      (+    pi pi   bindirme_aci         ))
;
 (setq aci1 0           )
 (setq aci2 tam_boy_aci )
 (setq l 0)
;
(if (> cevre tam_boy )
 (while (< l aci_sargi )
;
   (setq arc_p1 (list (+ (car   pt1 ) ( * yaricap (Cos aci1))) (+ (cadr  pt1 ) ( * yaricap (Sin aci1))) 0 ))
   (setq arc_p2 (list (+ (car   pt1 ) ( * yaricap (Cos aci2))) (+ (cadr  pt1 ) ( * yaricap (Sin aci2))) 0 ))
;
 (command "arc" "c" pt1 arc_p1 arc_p2)
   (setq aci1 (+ aci1 asil_kisim_aci))
   (setq aci2 (+ aci2 asil_kisim_aci))
   (if (= aci2 aci_sargi) (exit))
   (if (> aci2 aci_sargi) (setq aci2 aci_sargi))
   (setq l (+ l asil_kisim_aci))
 )
(command "circle" pt1 yaricap )
)
(setvar "blipmode" blipmode_old )
(setvar "cmdecho"  cmdecho_old  )
(setvar "clayer"   clayer_old   )
(setvar "luprec"   luprec_old   )
(setvar "osmode"   osmode_old   )
)

(defun c:ddon (/)
(setq pt1             (getpoint "\n Yerlesim noktasi ...............:"))
(if (= pt1 nil)       (exit))
(setq yaricap2        (getreal  "\n Dis Daire yariçapi..............:"))
(if (= yaricap2 nil)  (exit))
(setq yaricap1        (getreal  (strcat "\n Iç Daire yaricapi.<" (rtos  yaricap2 ) ">.:" )))
(if (= yaricap1 nil)  (setq yaricap1 yaricap2))
(setq aralik          (getreal  "\n Donati araligi...<   200 >......:"))
(if (= aralik nil)    (setq aralik    200 ))
(setq tam_boy         (getreal  "\n Tam boy donati...< 11700 >......:"))
(if (= tam_boy nil)   (setq tam_boy 11700 ))
(setq bindirme        (getreal  "\n Bindirme boyu....<  1000 >......:"))
(if (= bindirme nil)  (setq bindirme 1000 ))
;
(setq yaricap yaricap1)
  (while (<= yaricap yaricap2)
    ( daire_donatisi_ayarla pt1 yaricap tam_boy bindirme)
    ( setq yaricap (+ yaricap aralik))
 )
)
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;----- DAÝRESEL TEMEL IÞILSAL DONATILARI ---------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
(defun c:ddon2 (/)
(setq blipmode_old (getvar "blipmode"))
(setq cmdecho_old  (getvar "cmdecho" ))
(setq clayer_old   (getvar "clayer"  ))
(setq luprec_old   (getvar "luprec"  ))
(setq osmode_old   (getvar "osmode"  ))
(setq pt1                      (getpoint "\n Yerlesim noktasi ..............:"))
(if (= pt1 nil)                (exit))
(setq dis_cap                  (getreal  "\n Dis çap giriniz.................:"))
(if (= dis_cap nil)            (exit))
(setq ic_cap                   (getreal  "\n Iç çap giriniz..................:"))
(if (= dis_cap nil)            (exit))
(setq mak_donati_araligi       (getreal  "\n Maksimum donati araligi giriniz.:"))
(if (= mak_donati_araligi nil) (exit))
(setq min_donati_araligi       (getreal  "\n Minimum donati araligi giriniz..:"))
(if (= min_donati_araligi nil) (exit))
(setq ad1  (fix (/ (* 2 pi ic_cap ) min_donati_araligi)))
(setq ad2  (fix (/ (* 2 pi dis_cap) mak_donati_araligi)))
(setq donati_ekran_yazi (strcat "\n Donatý adeti giriniz.....Min<" (itoa ad1 ) ">...Mak<" (itoa ad2 ) ">...:"))
(setq donati_adeti             (getint   donati_ekran_yazi))
(if (= donati_adeti nil)       (exit))
;
(setq minimum_yaricap  (/ (* donati_adeti min_donati_araligi) (* 2 pi)))
(setq maksimum_yaricap (/ (* donati_adeti mak_donati_araligi) (* 2 pi)))
;
(princ "\n Minimum yaricap  ....:")(princ minimum_yaricap)
(princ "\n Maksimum yaricap  ...:")(princ maksimum_yaricap)
;
(if (< minimum_yaricap ic_cap)
          (setq dn_p1 (polar pt1 0 ic_cap           ))
          (setq dn_p1 (polar pt1 0 minimum_yaricap  ))
)
          (setq dn_p2 (polar pt1 0 dis_cap ))
;
(setvar "osmode"   0)
(command "line" dn_p1 dn_p2 "")
(command "array" "l" "" "p" pt1 donati_adeti 360 "y" )
;
(while (< maksimum_yaricap dis_cap)
(setq aci1 (/  (+ pi pi )  (* 2 donati_adeti)))
(princ "\n D.Ad.:") (princ donati_adeti)
(princ "\n Aci 1:") (princ (/ (* aci1 180) pi))
   (if (< maksimum_yaricap ic_cap)
    (setq dn_p1 (list (+ (car   pt1 ) ( * ic_cap           (Cos aci1))) (+ (cadr  pt1 ) ( * ic_cap           (Sin aci1))) 0 ))
    (setq dn_p1 (list (+ (car   pt1 ) ( * maksimum_yaricap (Cos aci1))) (+ (cadr  pt1 ) ( * maksimum_yaricap (Sin aci1))) 0 ))
   )
    (setq dn_p2 (list (+ (car   pt1 ) ( * dis_cap (Cos aci1))) (+ (cadr  pt1 ) ( * dis_cap (Sin aci1))) 0 ))
  (if (< (distance dn_p1 dn_p2) (* 4 mak_donati_araligi)) (exit))
  (command "line" dn_p1 dn_p2 "")
  (command "array" "l" "" "p" pt1 donati_adeti 360 "y" )
 (setq maksimum_yaricap (* 2 maksimum_yaricap))
 (setq donati_adeti (* 2 donati_adeti))
)
;
;
;
  ( setvar  "blipmode"  blipmode_old  )
  ( setvar  "cmdecho"   cmdecho_old   )
  ( setvar  "clayer"    clayer_old    )
  ( setvar  "luprec"    luprec_old    )
  ( setvar  "osmode"    osmode_old    )
)



