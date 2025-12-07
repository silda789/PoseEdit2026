; -----------------------------------------------------------------------------------------------------------------------------
; -> 77        : bir pozun kesit uzerine konulacak baglantili bilgisini verir. Poz degisirse bu bilgide deðisecektir.
; -> 66        : dnt_block seklindeki eski poz bloklarini RL-POS seklindeki yenilerine cevirir
; -> degis     : Bir bilgiyi eskiyi sosrarak degistirir. Poz ayrimi yapmaz. Sadece verilen eski bilgiyi degistirir.
; -> pozver    : Otomatik poz verir. Eski pozlari secerseniz benzerlerine ayni pozlari digerlerine sondan itibaren poz verir.
; -> ara       : Pozda donati araligina ve orantili olarak adeti degistirir.
; -> boy       : Pozlarin donati boylarini bukum boylariyla girer 00, 11, 21 pozlari icin.
; -> pozbul    : Bir pozu cizim uzerinde bulur ve oraya zoom yapar.
; -> tdd0      : Bir pozu sifirlayip butun ozelliklerini kaldirir.
; ->      : Bir pozun tum ozelliklerini baska pozlara kopyalar.
; -> tddu      : Genele uygulanacak degisiklikleri klavyeden girerek veya ornek bir pozu secerek yapar
; -> tddb      : Bir pozun toplam boyunu bukum boylari ve tipine bagli olarak hesaplar.
; -> tddg      : Grup carpanini degistirir.
; -> tdd       : Cizim uzerindeki pozlarin metrajini yapar.
; -> tdds      : Bloklarin Olcegini degistirir.
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:tdds ( / )
(setq yeniolc (getreal "\nYeni Olcek 1/: "))
(setq B (ssget (list (cons 0 "INSERT")(cons 2 "RL-POS"))))
(setq i 0)
(while (< i (sslength b))
(setq eleman (ENTGET (SSNAME B i)))
(setq r1 (cdr (assoc 41 eleman)))
(setq r2 (/ (/ yeniolc 10) r1))
(setq n1 (cdr (assoc 10 eleman)))
(command "scale" (SSNAME B i) "" n1  r2 "")
(setq i (+ i 1))
))
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:tik0 (/)
(princ "\n Tüm pozlara TIK deðeri 0 yazýlacak...... ")
 (setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
 (setq i 0)
  (while(< i (sslength eset))
   (setq en (ssname eset i))
   (att_yaz en "TIK" "0")
   (setq i (1+ i))
  )
(princ)
)
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:tik1 (/)
(princ "\n Tüm pozlara TIK deðeri 0 yazýlacak...... ")
 (setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
 (setq i 0)
  (while(< i (sslength eset))
   (setq en (ssname eset i))
   (att_yaz en "TIK" "1")
   (setq i (1+ i))
  )
(princ)
)
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:tbac (/)
(princ "\n Tum pozlar guncelleniyor...... ")
 (setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS2"))))
 (setq i 0)
  (while(< i (sslength eset))
   (setq en (ssname eset i))
   ;(if (= (att_prop_oku en "TB" "Invisible") :vlax-true) (att_prop_yaz en "TB" "Invisible" "false") (att_prop_yaz en "TB" "Invisible" "true"))
   (att_prop_yaz en "TB" "Invisible" "false")
   (setq i (1+ i))
  )
)
;-------------------------------------------------------------------------------
(defun c:pozclayer (/)
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons -4  "OR>"))))
(command "CHPROP" B "" "LA" "ren.mtr.tb" "" )
(princ)
)
;-------------------------------------------------------------------------------
(defun c:pzg ( / )
(fi_)
(vl-load-com)
(arxload "battman")
(setq eset_ins_base_list nil)
(princ "\n Guncellenecek pozlari sec... ")
 (setq eset (ssget "X" (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
 (setq i 0)
  (while(< i (sslength eset))
   (setq en (ssname eset i))
   (setq pnt_11 (att_prop_oku en     "TB"  "InsertionPoint"))
   (setq pnt_12 (att_prop_oku en     "BOY" "InsertionPoint"))
   (setq pnt_13 (att_prop_oku en     "NOT" "InsertionPoint"))
   (setq eset_ins_base_list (cons (list  pnt_11 pnt_12 pnt_13) eset_ins_base_list))
   (setq i (1+ i))
  )
 (setq eset_ins_base_list (reverse eset_ins_base_list))
 (command "-insert" (strcat "RL-POS=" source_path "Standard/RL-POS.dwg") ^C)
 (acet-attsync "RL-POS")
 (setq i 0)
  (while(< i (sslength eset))
   (setq en_new (ssname eset i))
   (setq pnt_11 (nth 0 (nth i eset_ins_base_list)))
   (setq pnt_12 (nth 1 (nth i eset_ins_base_list)))
   (setq pnt_13 (nth 2 (nth i eset_ins_base_list)))
   (setq pnt_21 (att_prop_oku en_new  "TB"  "InsertionPoint"))
   (setq pnt_22 (att_prop_oku en_new  "BOY" "InsertionPoint"))
   (setq pnt_23 (att_prop_oku en_new  "NOT" "InsertionPoint"))
   (att_yaz en_new "ARALIK"  (aralik_oku_isle (a_oku en_new "TB"  )))
   (setq block  (vlax-ename->vla-object en_new))
   (foreach attrib (vlax-invoke block 'GetAttributes)
    (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib pnt_21 pnt_11))
    (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib pnt_22 pnt_12))
    (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib pnt_23 pnt_13))
   )
   (poz_sekil_topla en_new)
   (setq i (1+ i))
  )
)
; ------------------------------------------------------------------------------
(defun Create_layer ( dosya_adi / s1 s2 i n l1 l2 eleman deger boy dosya_adi)
(setvar "cmdecho" 0)
(setq s1 ";")
(setq grup nil)
(setq dosya (open dosya_adi "r"))
(setq s2    (read-line dosya))
 (while (/= s2 nil)
   (setq i           1  )
   (setq n           1  )
   (setq l1 (strlen s1) )
   (setq l2 (strlen s2) )
   (setq eleman    nil  )
       (while (<= i l2)
          (if (= (substr s2 i l1) s1)
             (progn
               (setq boy  (- i n 1))
               (if (< boy 0 ) (setq boy 0))
               (setq deger    (substr s2 (+ n 1) boy))
               (setq eleman   (cons deger eleman))
               (setq n  i )
             )
          )
          (setq i (+ i 1) )
       )
    (setq eleman      (reverse eleman) )
    (setq layer_name      (nth 1 eleman) )
    (setq color     (atoi (nth 2 eleman)))
    (setq line_type       (nth 3 eleman) )
    (if (= line_type " ") (setq line_type "Continuous"))
    (if (= (tblsearch "LTYPE" line_type ) nil) ( command "-linetype" "L" line_type (strcat source_path "Linetype/ekle.lin") ""))
    (if (= (tblsearch "LAYER" layer_name) nil)
           (command "-LAYER" "N" layer_name  "C" color layer_name "L" line_type layer_name "")
           (command "-LAYER" "T" layer_name "ON" layer_name "C" color layer_name "L" line_type layer_name "")
    )
 (setq s2 (read-line dosya))
 )
(close dosya)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun metraj_layer_olustur (/)
;
(setvar "cmdecho" 0)
(setq dosya_adi_icin (open (strcat source_path "Standard/REN_QT_layer_file.txt") "r"))
(setq dosya_adi (read-line dosya_adi_icin))
(close dosya_adi_icin)
(Create_layer (strcat source_path dosya_adi))
(donati_metraj_yazi_sitili_yap "ren Gost.common" "GOST Common.ttf" 0.70 )
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun pozbilgi_sekil_topla ( en /)
(setq TB_GEN  (+ (* 0.9 (att_prop_oku en "TB"  "Height"))(* 0.90 (att_prop_oku en "TB"  "Height")(att_prop_oku en "TB"  "ScaleFactor")(strlen (att_prop_oku en "TB"  "TextString")))))
(if (= (att_prop_oku en "TB"  "Invisible") :vlax-true ) (setq TB_GEN 0))
(setq point_move_0 (att_prop_oku en "TB"  "InsertionPoint"))
(setq point_move_3 (att_prop_oku en "NOT" "InsertionPoint"))
(setq INS_POINT_TB  (vlax-safearray->list (vlax-variant-value (att_prop_oku en "TB"  "InsertionPoint"))))
(setq INS_POINT_NOT (vlax-safearray->list (vlax-variant-value (att_prop_oku en "NOT" "InsertionPoint"))))
(setq point_move_2 (vlax-3D-point (polar INS_POINT_TB (att_prop_oku en "TB" "Rotation")  TB_GEN )))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes)
 (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib point_move_3 point_move_2 )))
(princ)
)
;------------------------------------------------------------------------------------------------------------------------------
;Bir pozun yerlesim noktalarini sifirlamak icin
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;
(defun c:tdd0 ( / )
(princ "\n Pozisyonlari sifirlanacak attributeleri seciniz...")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(setq i 0)
(while (< i (sslength B))
(setq en (ssname B i))
(if (< (cdr (assoc 41 (entget en))) 0) (command "._mirror" en "" (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 100)
                                                                 (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 1000) "y"))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))
;
(setq TB_GEN  (+ (* 0.05 (att_prop_oku en "TB"  "Height"))(* 0.65 (att_prop_oku en "TB"  "Height")(att_prop_oku en "TB"  "ScaleFactor")(strlen (att_prop_oku en "TB"  "TextString")))))
(setq BOY_GEN (+ (* 0.55 (att_prop_oku en "BOY" "Height"))(* 0.75 (att_prop_oku en "BOY" "Height")(att_prop_oku en "BOY" "ScaleFactor")(strlen (att_prop_oku en "BOY" "TextString")))))
;
(setq P11 (polar INS_BASE INS_ANG (* (ABS INS_SCALE) 45)))
(setq P11 (polar P11 (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10)))
(setq P21 (polar P11 (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
(setq P31 (polar P11 INS_ANG TB_GEN))
(setq P11 (vlax-3D-point P11))
(setq P21 (vlax-3D-point P21))
(setq P31 (vlax-3D-point P31))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "TB"  "InsertionPoint") P11)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "BOY" "InsertionPoint") P21)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "NOT" "InsertionPoint") P31)))
(setq i (1+ i))
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tdd1 ( / )
(princ "\n Pozisyonlari degisecek attributeleri seciniz...")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(setq i 0)
(while (< i (sslength B))
(setq en (ssname B i))
(if (< (cdr (assoc 41 (entget en))) 0) (command "._mirror" en "" (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 100)
                                                                 (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 1000) "y"))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))
(setq P11 (polar INS_BASE INS_ANG (* (ABS INS_SCALE) 45)))
(setq P11 (polar P11 (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10)))
(setq P21 (polar P11 (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
(setq P31 (polar P21 (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
(setq P11 (vlax-3D-point P11))
(setq P21 (vlax-3D-point P21))
(setq P31 (vlax-3D-point P31))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "TB"  "InsertionPoint") P11)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "BOY" "InsertionPoint") P21)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "NOT" "InsertionPoint") P31)))
(setq i (1+ i))
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tdd2 ( / )
(princ "\n Pozisyonlari degisecek attributeleri seciniz...")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(setq i 0)
(while (< i (sslength B))
(setq en (ssname B i))
(if (< (cdr (assoc 41 (entget en))) 0) (command "._mirror" en "" (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 100)
                                                                 (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 1000) "y"))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))
(setq P11 (polar INS_BASE (+ INS_ANG (* 1.5 Pi)) (* 45 INS_SCALE)))
(setq p11 (polar p11      (+ INS_ANG (* 1.0 Pi)) (* 30 INS_SCALE)))
(setq P21 (polar P11      (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
(setq P31 (polar P21      (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
(setq P11 (vlax-3D-point P11))
(setq P21 (vlax-3D-point P21))
(setq P31 (vlax-3D-point P31))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "TB"  "InsertionPoint") P11)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "BOY" "InsertionPoint") P21)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "NOT" "InsertionPoint") P31)))
(setq i (1+ i))
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tdd3 ( / )
(princ "\n Pozisyonlari degisecek attributeleri seciniz...")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(setq i 0)
(while (< i (sslength B))
(setq en (ssname B i))
(if (< (cdr (assoc 41 (entget en))) 0) (command "._mirror" en "" (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 100)
                                                                 (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 1000) "y"))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))
(setq P11 (polar INS_BASE INS_ANG (* (ABS INS_SCALE) 45)))
(setq P11 (polar P11 (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10)))
(setq P21 (polar P11 (+ INS_ANG (* 0.5 pi)) (* 45 INS_SCALE)))
(setq P31 (polar P21 (+ INS_ANG (* 0.5 pi)) (* 45 INS_SCALE)))
(setq P11 (vlax-3D-point P11))
(setq P21 (vlax-3D-point P21))
(setq P31 (vlax-3D-point P31))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "TB"  "InsertionPoint") P11)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "BOY" "InsertionPoint") P21)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "NOT" "InsertionPoint") P31)))
(setq i (1+ i))
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tdd4 ( / )
(princ "\n Pozisyonlari degisecek attributeleri seciniz...")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(setq i 0)
(while (< i (sslength B))
(setq en (ssname B i))
(if (< (cdr (assoc 41 (entget en))) 0) (command "._mirror" en "" (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 100)
                                                                 (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 1000) "y"))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))

(setq TB_GEN  (+ (* 0.05 (att_prop_oku en "TB"  "Height"))(* 0.65 (att_prop_oku en "TB"  "Height")(att_prop_oku en "TB"  "ScaleFactor")(strlen (att_prop_oku en "TB"  "TextString")))))
(setq BOY_GEN (+ (* 0.55 (att_prop_oku en "BOY" "Height"))(* 0.75 (att_prop_oku en "BOY" "Height")(att_prop_oku en "BOY" "ScaleFactor")(strlen (att_prop_oku en "BOY" "TextString")))))
(setq NOT_GEN (+ (* 0.55 (att_prop_oku en "NOT" "Height"))(* 0.65 (att_prop_oku en "NOT" "Height")(att_prop_oku en "NOT" "ScaleFactor")(strlen (att_prop_oku en "NOT" "TextString")))))

(setq P11 (polar INS_BASE (+ INS_ANG        pi)  (+ (* (ABS INS_SCALE) 30) TB_GEN) ))
(setq P11 (polar P11      (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10) ))
(setq P21 (polar P11      (+ INS_ANG        pi)    BOY_GEN              ))
(setq P31 (polar P21      (+ INS_ANG        pi)    NOT_GEN              ))
(setq P11 (vlax-3D-point P11))
(setq P21 (vlax-3D-point P21))
(setq P31 (vlax-3D-point P31))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "TB"  "InsertionPoint") P11)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "BOY" "InsertionPoint") P21)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "NOT" "InsertionPoint") P31)))
(setq i (1+ i))
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tdd5 ( / )
(princ "\n Pozisyonlari sifirlanacak attributeleri seciniz...")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(setq i 0)
(while (< i (sslength B))
(setq en (ssname B i))
(if (< (cdr (assoc 41 (entget en))) 0) (command "._mirror" en "" (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 100)
                                                                 (polar (cdr (assoc 10 (entget en))) (+ (cdr (assoc 50 (entget en))) (* 0.5 Pi)) 1000) "y"))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))
;
(setq TB_GEN  (+ (* 0.05 (att_prop_oku en "TB"  "Height"))(* 0.65 (att_prop_oku en "TB"  "Height")(att_prop_oku en "TB"  "ScaleFactor")(strlen (att_prop_oku en "TB"  "TextString")))))
(setq BOY_GEN (+ (* 0.55 (att_prop_oku en "BOY" "Height"))(* 0.75 (att_prop_oku en "BOY" "Height")(att_prop_oku en "BOY" "ScaleFactor")(strlen (att_prop_oku en "BOY" "TextString")))))
;
(setq P11 (polar INS_BASE INS_ANG (* (ABS INS_SCALE) 45)))
(setq P11 (polar P11 (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10)))
(setq P21 (polar P11 INS_ANG TB_GEN ))
(setq P31 (polar P21 INS_ANG BOY_GEN))
(setq P11 (vlax-3D-point P11))
(setq P21 (vlax-3D-point P21))
(setq P31 (vlax-3D-point P31))
(setq block  (vlax-ename->vla-object en))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "TB"  "InsertionPoint") P11)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "BOY" "InsertionPoint") P21)))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (att_prop_oku en "NOT" "InsertionPoint") P31)))
(setq i (1+ i))
)
(princ)
)
;
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;
(defun aci_olc (P1 P2 P3)
(setq aci_1 (ANGLE P1 P2))
(setq aci_2 (ANGLE P1 P3))
(setq aci (ABS (- aci_1 aci_2)))
aci
)
;
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;
(defun poz_sekil_topla ( en / )
(if     (< (strlen (a_oku en "TB")) 2)
        (att_prop_yaz en "TB" "Invisible" "true" )
        (att_prop_yaz en "TB" "Invisible" "false")
)
(if (or (= (a_oku en "BOY") "") (= (a_oku en "BOY") " "))
        (att_prop_yaz en "BOY" "Invisible" "true" )
        (att_prop_yaz en "BOY" "Invisible" "false")
)
(if (or (= (a_oku en "NOT") "") (= (a_oku en "NOT") " "))
        (att_prop_yaz en "NOT" "Invisible" "true" )
        (att_prop_yaz en "NOT" "Invisible" "false")
)
(setq block  (vlax-ename->vla-object en))
(setq INS_BASE   (cdr (assoc 10 (entget en))))
(setq INS_ANG    (cdr (assoc 50 (entget en))))
(setq INS_SCALE  (cdr (assoc 41 (entget en))))
(setq Etki_daire (* 20 INS_SCALE))
;
(setq TB_GEN  (+ (* 0.05 (att_prop_oku en "TB"  "Height"))(* 0.65 (att_prop_oku en "TB"  "Height")(att_prop_oku en "TB"  "ScaleFactor")(strlen (att_prop_oku en "TB"  "TextString")))))
(setq BOY_GEN (+ (* 0.55 (att_prop_oku en "BOY" "Height"))(* 0.75 (att_prop_oku en "BOY" "Height")(att_prop_oku en "BOY" "ScaleFactor")(strlen (att_prop_oku en "BOY" "TextString")))))
;
(setq P10 (vlax-safearray->list (vlax-variant-value (att_prop_oku en "TB"  "InsertionPoint"))))
;
(setq P11  (polar INS_BASE INS_ANG (* (ABS INS_SCALE) 45)))
(setq P11  (polar P11 (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10)))
;
(if (AND (> (distance P10 P11) 0) (< (distance P10 P11) Etki_daire))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "TB"  (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P10) (vlax-3D-point P11))))
)
;
(setq P10 (vlax-safearray->list (vlax-variant-value (att_prop_oku en "TB"  "InsertionPoint"))))
(setq P20 (vlax-safearray->list (vlax-variant-value (att_prop_oku en "BOY" "InsertionPoint"))))
;
(setq P21 (polar P10    INS_ANG TB_GEN))
(setq P22 (polar P10 (+ INS_ANG (* 0.5 pi)) (* 45 INS_SCALE)))
(setq P23 (polar P10 (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
;
(if (or (AND (> (distance P20 P21) 0) (< (distance P20 P21) Etki_daire)) (equal INS_ANG (angle p10 p20) 0.10 ))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P20) (vlax-3D-point P21))))
)

(if (AND (> (distance P20 P22) 0) (< (distance P20 P22) Etki_daire))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P20) (vlax-3D-point P22))))
)

(if (AND (> (distance P20 P23) 0) (< (distance P20 P23) Etki_daire))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "BOY" (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P20) (vlax-3D-point P23))))
)

(setq P30 (vlax-safearray->list (vlax-variant-value (att_prop_oku en "NOT" "InsertionPoint"))))
;
(if (or (AND (> (distance P20 P21) 0) (< (distance P20 P21) Etki_daire)) (equal INS_ANG (angle p10 p20) 0.10 ))
     (setq P31 (polar P10    INS_ANG (+ TB_GEN BOY_GEN)) )
     (setq P31 (polar P10    INS_ANG  TB_GEN ))
)
;
(setq P32 (polar P10 (+ INS_ANG (* 0.5 pi)) (* 90 INS_SCALE)))
(setq P33 (polar P10 (+ INS_ANG (* 1.5 pi)) (* 90 INS_SCALE)))
;
(if (or (AND (> (distance P30 P31) 0) (< (distance P30 P31) Etki_daire)) (equal INS_ANG (angle p10 p30) 0.10 ))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P30) (vlax-3D-point P31))))
)
(if (AND (> (distance P30 P32) 0) (< (distance P30 P32) Etki_daire))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P30) (vlax-3D-point P32))))
)
(if (AND (> (distance P30 P33) 0) (< (distance P30 P33) Etki_daire))
     (foreach attrib (vlax-invoke block 'GetAttributes) (if (eq "NOT" (strcase (vla-get-TagString attrib))) (vla-move attrib (vlax-3D-point P30) (vlax-3D-point P33))))
)
(princ)
)


;(setq P11 (polar INS_BASE INS_ANG (* (ABS INS_SCALE) 45)))
;(setq P11 (polar P11 (+ INS_ANG (* 0.5 Pi)) (* (ABS INS_SCALE) 10)))
;(setq P21 (polar P11 (+ INS_ANG (* 1.5 pi)) (* 45 INS_SCALE)))
;(setq P31 (polar P11 INS_ANG TB_GEN))


; -----------------------------------------------------------------------------------------------------------------------------
(defun c:adet ( / )
(fi_)
(setq tb nil)
(princ "\n Adeti degisecek pozlari seciniz....")
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(setq adet  (itoa (getint "\n Donati adeti          :" )))
(setq i 0)
(while(< i (sslength eset))
(setq en (ssname eset i))
(setq tb (a_oku en "TB" ))
(setq adet_carp    (poz_edit_adet_carpi tb))
(setq cap          (poz_edit_cap        tb))
(setq aralik       (poz_edit_aralik     tb))
(if (> (strlen adet_carp) 0) (setq adet_carp_ (strcat adet_carp "x")) (setq adet_carp_ ""))
(if (> (strlen aralik)    0) (setq aralik_    (strcat "/" aralik ))   (setq aralik_ ""   ))
(setq tb (strcat adet_carp_ adet fi cap aralik_))
(att_yaz en "TB" tb )
(poz_sekil_topla en)
(setq i (1+ i))))
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:adet2 ( / )
(fi_)
(setq tb nil)
(princ "\n Adeti carpani degisecek pozlari seciniz....")
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(setq adet_carpi (getint "\n Donati adeti çarpani:" ))
(if (> adet_carpi 1) (setq adet_carp (strcat (itoa adet_carpi) "x")) (setq adet_carp "") )
(setq i 0)
(while(< i (sslength eset))
(setq en (ssname eset i))
(setq tb (a_oku en "TB" ))

(setq position (vl-string-search "x" (strcase tb) 0 ))

(if (= position nil) (setq position -1 ))
(setq tb_sag (substr tb (+ position 2) (strlen tb)))
(att_yaz en "TB" (strcat adet_carp tb_sag))
(poz_sekil_topla en)
(setq i (1+ i))))
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:cap ( / adet )
(fi_)
(princ "\n Capi degisecek pozlari seciniz....")
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(setq cap  (itoa (getint "\n Donati çapi       :" )))
(setq i 0)
(while(< i (sslength eset))
(setq en (ssname eset i))
(setq tb (a_oku en "TB" ))
(setq adet_carp    (poz_edit_adet_carpi tb))
(setq adet         (poz_edit_adet       tb))
;(setq cap          (poz_edit_cap        tb))
(setq aralik       (poz_edit_aralik     tb))
(if (> (strlen adet_carp) 0) (setq adet_carp_ (strcat adet_carp "x")) (setq adet_carp_ ""))
(if (> (strlen aralik)    0) (setq aralik_    (strcat "/" aralik ))   (setq aralik_ ""   ))
(setq tb (strcat adet_carp_ adet fi cap aralik_))
(att_yaz en "TB" tb )
(att_yaz en "TIK" "1")
(poz_sekil_topla en)
(setq i (1+ i))))
; -----------------------------------------------------------------------------------------------------------------------------
(defun att_field_varmi (en tag / )
(vl-load-com)
(setq tag (strcase tag))
(setq deger 0)
(setq blk  (vlax-ename->vla-object en))
(setq atts (vlax-invoke blk "Getattributes"))
(foreach att atts
  (if (= (vlax-get-property att "TagString" ) tag )
    (progn
      (setq xdict (cdr (assoc 360 (entget (vlax-vla-object->ename att)))))
      (if (= xdict nil)
             (setq deger 0)
             (if (/= (dictsearch xdict "ACAD_FIELD") nil) (setq deger 1))
      )
    )
  )
)
deger
)
; -----------------------------------------------------------------------------------------------------------------------------
(defun att_field_sil (en tag / )
(vl-load-com)
(setq tag (strcase tag))
(setq deger 0)
(setq blk  (vlax-ename->vla-object en))
(setq atts (vlax-invoke blk "Getattributes"))
(foreach att atts
  (if (= (vlax-get-property att "TagString" ) tag )
    (progn
      (setq xdict (cdr (assoc 360 (entget (vlax-vla-object->ename att)))))
      (if (= xdict nil)
             (setq deger 0)
             (if (/= (dictsearch xdict "ACAD_FIELD") nil) (progn (dictremove xdict "ACAD_FIELD")(entdel (vlax-vla-object->ename att))))
      )
    )
  )
)
)
; -----------------------------------------------------------------------------------------------------------------------------
(defun c:aralik ( / adet )
(fi_)
(princ "\n Aralýðý deðiþecek pozlarý seçiniz....")
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(setq aralik_yeni (getint "\n Donati araliðý       :" ))
(setq i 0)
 (while(< i (sslength eset))
  (setq en (ssname eset i))
  (setq tb (a_oku en "TB" ))
  (setq adet_carp    (poz_edit_adet_carpi tb))
  (setq adet_eski    (poz_edit_adet       tb))
  (setq cap          (poz_edit_cap        tb))
  (setq aralik_eski  (poz_edit_aralik     tb))
  (att_yaz en "ARALIK" aralik_yeni )
  (if (= (att_field_varmi en "TB") 0)
    (setq adet_yeni (itoa (max 1 (fix (+ 0.5 (/ (* (atoi adet_eski) (atoi aralik_eski)) aralik_yeni))))))
    (setq adet_yeni adet_eski)
  )
    (if (> (strlen adet_carp  ) 0) (setq adet_carp_ (strcat adet_carp "x"   )) (setq adet_carp_ "" ))
    (if (> (strlen (itoa aralik_yeni)) 0) (setq aralik_    (strcat "/" (itoa aralik_yeni) )) (setq aralik_  "" ))
    (setq tb (strcat adet_carp_ adet_yeni fi cap aralik_))
    (att_yaz en "TB" tb )
    (VL-CmdF "_.UpdateField" en "" )
    (poz_sekil_topla en)
 (setq i (1+ i))
)
(princ)
)
;------------------------------------------------------------------------------------------------------------------------------
(defun fi_ (/) (setq fi "%%C" ))
;------------------------------------------------------------------------------------------------------------------------------
(defun c:51 (/)
(setq i 0)
(if (or (= etr_gen nil) (= etr_gen "")) (setq etr_gen "250" ))
(if (or (= etr_yuk nil) (= etr_yuk "")) (setq etr_yuk "450" ))
(if (or (= knc_yuk nil) (= knc_yuk "")) (setq knc_yuk "10" ))
(princ (strcat "\n  Etriye genisligi: " etr_gen "  Etriye yuksekligi : " etr_yuk " Kanca boyu : " knc_yuk "  Toplam boy : " (RTOS (+ (* 2 (+ (ATOF etr_gen) (ATOF etr_yuk))) (* 2 (atof knc_yuk))) 2 0 ) ))
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(if (= eset nil)
      (progn
             (setq etr_gen_e etr_gen)
             (setq etr_gen (getstring (strcat "\n Etriye genisligi   < " etr_gen " > : ")))
             (if (or (= etr_gen nil) (= etr_gen "")) (setq etr_gen etr_gen_e ))
             (setq etr_yuk_e etr_yuk)
             (setq etr_yuk (getstring (strcat "\n Etriye yuksekligi  < " etr_yuk " > : ")))
             (if (or (= etr_yuk nil) (= etr_yuk "")) (setq etr_yuk etr_yuk_e ))
             (setq knc_yuk_e knc_yuk)
             (setq knc_yuk (getstring (strcat "\n Etriye kanca boyu  < " knc_yuk " > : ")))
             (if (or (= knc_yuk nil) (= knc_yuk "")) (setq knc_yuk knc_yuk_e ))
         (setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
       )
)
(while(< i (sslength eset))
(setq en (ssname eset i))
(att_yaz en "POZ" "X")
(att_yaz en "BOY" (STRCAT "L=" (RTOS (+ (* 2 (+ (ATOF etr_gen) (ATOF etr_yuk))) (* 2 (atof knc_yuk))) 2 0 )))
(att_yaz en "TIP" "51")
(att_yaz en "A"   etr_yuk)
(att_yaz en "B"   etr_gen)
(att_yaz en "C"   knc_yuk)
(att_yaz en "D"   knc_yuk)
(att_yaz en "E"    "0")
(att_yaz en "F"    "0")
(att_yaz en "R"    "0")
(setq i (1+ i))
)
)
;------------------------------------------------------------------------------------------------------------------------------
(defun c:boy ( / boy_lst)
(vl-load-com)
(setq boy_lst (list))
(setq en1 (car (entsel "\n Boyu degisecek pozlari seciniz... " )))
(if (= en1 nil)
         (progn
          (initget "T L")
          (setq boy_al_tip (getkword "\n Bukum boylarini <L>ine <T>ext den al Enter .... :"))
          (setq en1 (car (entsel "\n Boyu degisecek pozlari seciniz... " )))
         )
)
(if (/= boy_al_tip "L")
  (progn
    (SETQ SAYI 1)
    (princ "\n Boy bilgileri yazilardan alinacak. Boy yazilarini sec...     ")
    (setq B (ssget (list (cons 0 "TEXT"))))
    (foreach ent (vl-remove-if 'listp (mapcar 'cadr(ssnamex B))) (setq boy_lst (cons (float (* (fix (+ (/ (atof (cdr (assoc 1 (entget ent)))) SAYI) 0.5)) SAYI)) boy_lst )))
    (setq boy_lst (vl-sort boy_lst (function (lambda (e1 e2) (< e1 e2)))))
  )
  (progn
    (SETQ SAYI 5)
    (setq oran (GETVAR "DIMLFAC"))
    (princ "\n Boy bilgileri cizgilerden alinacak. Donati cizgilerini sec...")
    (setq B (ssget (list (cons 0 "LINE"))))
    (foreach ent (vl-remove-if 'listp (mapcar 'cadr(ssnamex B))) (setq boy_lst (cons (float (* (fix (+ (/ (* oran (distance (cdr (assoc 10 (entget ent))) (cdr (assoc 11 (entget ent))))) SAYI) 0.5)) SAYI)) boy_lst )))
    (setq boy_lst (vl-sort boy_lst (function (lambda (e1 e2) (< e1 e2)))))
  )
)
(cond
  ((= (length boy_lst) 1)
                       (progn
                         (att_yaz en1 "POZ"  "X")
                         (att_yaz en1 "BOY" (STRCAT "L=" (rtos (nth 0 boy_lst) 2 0)))
                         (att_yaz en1 "TIP" "00")
                         (att_yaz en1 "A"   (rtos (nth 0 boy_lst) 2 0))
                         (att_yaz en1 "B"    "0")
                         (att_yaz en1 "C"    "0")
                         (att_yaz en1 "D"    "0")
                         (att_yaz en1 "E"    "0")
                         (att_yaz en1 "F"    "0")
                         (att_yaz en1 "R"    "0")
                       )
  )
  ((= (length boy_lst) 2)
                       (progn
                         (att_yaz en1 "POZ"  "X")
                         (att_yaz en1 "BOY" (STRCAT "L=" (rtos (+ (nth 0 boy_lst) (nth 1 boy_lst)) 2 0)))
                         (att_yaz en1 "A"   (rtos (nth 0 boy_lst) 2 0))
                         (att_yaz en1 "B"   (rtos (nth 1 boy_lst) 2 0))
                         (att_yaz en1 "TIP" "11")
                         (att_yaz en1 "C"    "0")
                         (att_yaz en1 "D"    "0")
                         (att_yaz en1 "E"    "0")
                         (att_yaz en1 "F"    "0")
                         (att_yaz en1 "R"    "0")
                       )
  )
  ((= (length boy_lst) 3)
                       (progn
                         (att_yaz en1 "POZ"  "X")
                         (att_yaz en1 "BOY" (STRCAT "L=" (rtos (+ (nth 0 boy_lst) (nth 1 boy_lst) (nth 2 boy_lst)) 2 0)))
                         (att_yaz en1 "A"   (rtos (nth 0 boy_lst) 2 0 ))
                         (att_yaz en1 "B"   (rtos (nth 2 boy_lst) 2 0 ))
                         (att_yaz en1 "C"   (rtos (nth 1 boy_lst) 2 0 ))
                         (att_yaz en1 "TIP" "21")
                         (att_yaz en1 "D"    "0")
                         (att_yaz en1 "E"    "0")
                         (att_yaz en1 "F"    "0")
                         (att_yaz en1 "R"    "0")
                       )
  )
( T                    (princ)          )
)
(poz_sekil_topla en1)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun a_tum_oku ( en /)
(vl-load-com)
(setq deger "Prob->" )
(setq blk  (vlax-ename->vla-object en))
(setq atts (vlax-invoke blk 'getattributes))
(foreach att atts (setq deger (strcat deger (vla-get-tagstring  att)   ": " (vla-get-textstring att) " , ")))
deger
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:ppp (/)
(setq i 0)
(metraj_layer_olustur)
(SETQ HK (* 0.05 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq pt1 nil)
(if (/= poz_no      nil) (setq eski_poz_no      poz_no     ))
(if (/= pozbul_eset nil) (setq eski_pozbul_eset pozbul_eset)
                         (setq eski_pozbul_eset (ssget "X" (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") ))))
(setq poz_no (getstring "\n Poz no girin : "))
(if (= poz_no nil) (setq poz_no eski_poz_no))
(setq pozbul_eset (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(if (= pozbul_eset nil) (setq pozbul_eset eski_pozbul_eset))
(setq pt3 (getpoint "\n Bulunan pozlara þimdi iþaretleyeceðiniz noktadan oklar çizilecek... Lutfen bu noktayý gösteriniz... "))
 (setq toplam_adet 0)
  (while(< i (sslength pozbul_eset))
   (setq en (ssname pozbul_eset i))
   (setq poz_bul  (a_oku en "POZ" ))
   (if (= poz_no poz_bul)
        (progn
          (setq toplam_adet (+ toplam_adet 1))
          (setq pt1 (cdr (assoc 10 (entget en))))
          (setq aci (angle pt1 pt3))
          (setq pt2 (polar pt1 aci (* hk  0.240) ) )
          (command "osmode" 0 "clayer" "ren.arrow" "pline" pt1 "w" 0 (* hk  0.080) pt2 "w" 0 0 pt3 "")
         )
   )
   (setq i (+ i 1))
  )
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:ppp2 (/)
(setq i 0)
(metraj_layer_olustur)
(SETQ HK (* 0.05 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq pt1 nil)
(if (/= poz_no      nil) (setq eski_poz_no      poz_no     ))
(if (/= pozbul_eset nil) (setq eski_pozbul_eset pozbul_eset)
                         (setq eski_pozbul_eset (ssget "X" (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") ))))
(setq poz_no (getstring "\n Poz no girin : "))
(if (= poz_no nil) (setq poz_no eski_poz_no))
(setq pozbul_eset (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "RL-POS2") (cons -4  "OR>") )))
(if (= pozbul_eset nil) (setq pozbul_eset eski_pozbul_eset))
 (setq toplam_adet 0)
  (while(< i (sslength pozbul_eset))
   (setq en (ssname pozbul_eset i))
   (setq poz_bul  (a_oku en "POZ" ))
   (if (= poz_no poz_bul) (setq toplam_adet (+ toplam_adet 1)))
   (setq i (+ i 1))
  )
(setq i 0)
(setq sira 0)
(while(< i (sslength pozbul_eset))
(setq en (ssname pozbul_eset i))
(setq poz_bul  (a_oku en "POZ" ))
(if (= poz_no poz_bul)
(progn
 (setq sira (+ sira 1))
 (setq pt1 (cdr (assoc 10 (entget en))))
 (command "zoom" "w" (polar pt1 (*  pi 0.15) (* 3 (BIRIM_OKU))) (polar pt1 (*  pi 1.15) (* 1 (BIRIM_OKU))))
 (setq x (getstring (STRCAT "\n No:" (itoa sira) "/" (itoa toplam_adet) " - " (a_tum_oku en ) " Aramaya devam edilsin mi?..<E>...")))
 (if (or ( = x "h") ( = x "H")) (exit))
)
)
;
(if (and (= (substr poz_bul 1 1) "#") (= (substr poz_no 1 1) "#"))
     (progn
       (setq pt1 (cdr (assoc 10 (entget en))))
       (command "zoom" "w" (polar pt1 (*  pi 0.15) (* 3 (BIRIM_OKU))) (polar pt1 (*  pi 1.15) (* 1 (BIRIM_OKU))))
       (setq x (getstring "\n Geçerli eleman silinsin mi?...  "))
       (if  ( = (strcase x) "E") (command "erase" (ssname pozbul_eset i) "" ))
    )
)
(setq i (+ i 1))
)
(princ "\n Arama tamamlandý ... ")
(if (= pt1 nil) (princ "ama biþey bulamadýk..!"))
(gc)
(gc)
(gc)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tbcoz ( / poz_scale)
(vl-load-com)
(command "-insert" (strcat "RL-POS=" source_path "Standard/RL-POS.dwg") ^C)
(command "-LAYER" "T" "mtr_tb" "ON" "mtr_tb" "")
(command "_.Undo" "_BE")
(setq poz_scale (* 0.00010 (* (OLCEK_OKU) (BIRIM_OKU))))     ;  Cizim olcegi mm CALISIRKEN 1000 CM CALISIRKEN 100
(setq blipmode_old (getvar "blipmode"))
(setq cmdecho_old  (getvar "cmdecho" ))
(setq clayer_old   (getvar "clayer"  ))
(setq luprec_old   (getvar "luprec"  ))
(setq osmode_old   (getvar "osmode"  ))
(setvar "osmode"  0 )
(setvar "cmdecho" 0 )
(setvar "attreq"  0 )
(fi_)
(setq fi_e "ƒ")
; (setq fi_e "%%D")
; (setq fi_e "ƒ")

(princ "\n Okunacak STA4CAD Poz' u seç... ")
(setq B   (ssget (list (cons 0 "TEXT"))))
(setq i 0)
(setq n (sslength b))
;
(while (< i n)
(setq en (ssname B i))
(setq poz_not "")
(setq tb_boy  (cdr (assoc 1 (entget en))))
(repeat (strlen tb_boy) (setq tb_boy  (vl-string-subst " " "  " tb_boy )))
(setq l_yeri  (vl-string-search "="  tb_boy ))
(setq b_yeri  (vl-string-search "/"  tb_boy ))
(setq f_yeri  (vl-string-search fi_e tb_boy ))
(if (/= f_yeri nil) (COMMAND "CHPROP" en "" "C" "BYLAYER" "LT" "BYLAYER" "LA" "DONATI_yaz2" "S" "1" ""))
(if (and (/= l_yeri nil) (/= f_yeri nil))
(progn

(if (= b_yeri nil) (setq aralik_b_b 5) (setq aralik_b_b 4))
(setq tb (substr tb_boy 1 (- l_yeri 1)))
(setq bos_yeri  (vl-string-search " " tb 1))
(if (/= bos_yeri nil)
       (progn
        (setq poz_not  (substr tb (+ bos_yeri 1) (- (strlen tb) bos_yeri)))
        (repeat (strlen poz_not) (setq poz_not  (vl-string-subst "" " " poz_not)))
        (setq tb       (substr tb 1 bos_yeri))
       )
)
(repeat (strlen tb) (setq tb  (vl-string-subst fi fi_e tb )))
(setq boy     (substr tb_boy l_yeri (strlen tb_boy)))
(setq adet_b   (adet_oku_isle    tb ))
(setq cap_b    (cap_oku_isle     tb ))
(setq aralik_b (substr tb (+ 1 (vl-string-search (strcat fi cap_b) tb) (strlen (strcat fi cap_b))) aralik_b_b))
(setq boy_b    (rtos (atof (boy_oku_isle boy)) 2 0))
(setvar "clayer" "ren.mtr.tb")
(command "-insert" (strcat source_path "Standard/RL-POS") (polar (cdr (assoc 10 (entget en))) (* 1.5 pi) 3) poz_scale poz_scale (/ (* 180 (cdr (assoc 50 (entget en)))) pi))
(setq en1 (entlast))
(att_yaz en1 "POZ"  "0")
(att_yaz en1 "TB"   (strcat adet_b fi cap_b aralik_b))
(att_yaz en1 "BOY"  (strcat "L=" boy_b))
(att_yaz en1 "TIP"  "00")
(att_yaz en1 "A"    boy_b)
(att_yaz en1 "B"    "0")
(att_yaz en1 "C"    "0")
(att_yaz en1 "D"    "0")
(att_yaz en1 "E"    "0")
(att_yaz en1 "F"    "0")
(att_yaz en1 "R"    "0")
(att_yaz en1 "NOT"  poz_not)
(poz_sekil_topla en1)
(command "erase" en "")
)
)
(setq i (1+ i))
)
;
(setvar "blipmode" blipmode_old )
(setvar "cmdecho"  cmdecho_old  )
(setvar "clayer"   clayer_old   )
(setvar "luprec"   luprec_old   )
(setvar "osmode"   osmode_old   )
(command "_.Undo" "_E")
(princ)
)
;------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:grup (/)
(command "_.Undo" "_BE")
(princ "\n Grup carpani degisecek pozlar... ")
(setq B (ssget (list (cons 0 "INSERT") (cons -4  "<OR") (cons 2 "RL-POS") (cons 2 "REN_QT_stl_mtr") (cons -4  "OR>") )))
(setq gc_deger (getint "\n Grup çarpani                   : "))
(setq i 0)
(while (< i (sslength b)) (setq en (ssname b i)) (att_yaz en "GC" gc_deger) (poz_sekil_topla en) (setq i (1+ i)))
(command "_.Undo" "_E")
(princ)
)
;------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tddb (/)
;
;Bukum bilgileri girilmis bir donati pozunun toplam boy bilgisini hesaplar ve poz icerisine girer
;
(command "_.Undo" "_BE")
(princ "\n Otomatik boy hesabi yapilacak pozlarý sec. <Not:99 tipi pozlar duzeltilmez>  ")
(setq B (ssget (list (cons 0 "INSERT")(cons 2 "RL-POS"))))
(setq i 0)
(while (< i (sslength b))
(setq en (ssname b i))
(setq boy_str (b_h (a_oku en "TIP") (a_oku en "A" ) (a_oku en "B" ) (a_oku en "C" ) (a_oku en "D" ) (a_oku en "E" ) (a_oku en "F" ) (atof (a_oku en "R")) (atof (cap_oku_isle (a_oku en "TB")))))
(princ "\n Poz : ")(princ (a_oku en "POZ"))(princ "      Tip : ")(princ (a_oku en "TIP"))(princ "      Boy : ")(princ boy)
(if (/= (a_oku en "TIP") "99") (att_yaz en "BOY"  boy_str))
(poz_sekil_topla en)
(setq i (1+ i))
)
(command "_.Undo" "_E")
(princ)
)
; -----------------------------------------------------------------------------------------------------------------------------
; Pozlar icin kesit uzerine bagli bilgi koyar. Poz degistikce bu bilgi de degisir.
;
(defun c:77 ( / en blk atts att pn id olcu aci p71 text)
(vl-load-com)
(SetVar "CmdEcho" 0)
(if (= (getvar "FIELDDISPLAY") 0) (setvar "FIELDDISPLAY" 1)) ;1 arka tarafi beyaz gosterir
(setq en (car (entsel "\n Okunacak Poz' u seç... " )))
;(setq ins_scale  (cdr (assoc 41 (entget en))))
(setq ins_scale (* (/ (OLCEK_OKU) (BIRIM_OKU)) 100))
(setq blk  (vlax-ename->vla-object en))
(setq atts (vlax-invoke blk 'getattributes))
(foreach att atts
(setq pn   (vlax-get-property att 'TagString))
  (if (= pn "POZ")
    (progn
      (setq id1  (vlax-get-property att 'ObjectId))
      (setq olcu (vlax-get-property att 'Height))
      (setq aci  (vlax-get-property att 'Rotation))
    )
  )
  (if (= pn "TB")
    (progn
      (setq id2   (vlax-get-property att 'ObjectId))
    )
  )
  (if (= pn "NOT")
    (progn
      (setq id3   (vlax-get-property att 'ObjectId))
    )
  )
)
(princ (strcat "\n Açý : " (rtos (/ (* 180 aci) pi) 2 0)))
(setq pt1 (getpoint "\n Yerleþim Noktasý... "))
(while (= pt1 nil)
      (if (= aci 0) (setq aci (* 0.5 pi)) (setq aci 0))
      (princ (strcat "\n Açý : " (rtos (/ (* 180 aci) pi) 2 0)))
      (setq pt1 (getpoint "\n Yerleþim Noktasý... "))
)
;(while (/= pt1 nil)
;(setq pt1 (polar pt1 (+ aci (* 0.5 pi)) olcu))
(setq text1 (strcat "%<\\AcObjProp Object(%<\\_ObjId " ( itoa id1 ) ">%).TextString>%"))
(setq text2 (strcat "%<\\AcObjProp Object(%<\\_ObjId " ( itoa id2 ) ">%).TextString>%"))
(setq text3 (strcat "%<\\AcObjProp Object(%<\\_ObjId " ( itoa id3 ) ">%).TextString>%"))
(setq clayer_old   (getvar "clayer"  ))
(setvar "clayer" "ren.mtr.Pos")
(command "-insert" (strcat source_path "Standard/RL-POS2.dwg") pt1 ins_scale ins_scale (/ (* aci 180) pi) )
(setq en (entlast))
(att_yaz en "POZ" text1 )
(att_yaz en "TB"  text2 )
(att_yaz en "NOT" text3 )
(VL-CmdF "_.UpdateField" en "")
(SetVar "CmdEcho" 1)
(setvar "clayer"   clayer_old   )
(princ)
;(setq pt1 (getpoint "\n Yerlesim Noktasi... "))
;)
)
;------------------------------------------------------------------------------------------------------------------------------
(defun c:tddu     ( /
                      bolu_isareti
                      bilgi_poz
                      bilgi_cap
                      bilgi_boy
                      bilgi_tip
                      bilgi_a
                      bilgi_b
                      bilgi_c
                      bilgi_d
                      bilgi_e
                      bilgi_f
                      bilgi_r
                      bilgi_not
                      en1
                      en2
                      i
                      eset
                      tb
                                )
(fi_)
(setq en1 (car (entsel "\n Okunacak Poz' u sec..."           )))
(if (= en1 nil)
(progn
(setq bilgi_poz    (getstring "\n Poz     : "))
(setq bilgi_cap    (getstring "\n Çap     : "))
(setq bilgi_boy    (getstring "\n Boy     : "))
(if (/= (substr bilgi_boy 1 1 ) "L") (setq bilgi_boy (strcat "L=" bilgi_boy )))
(setq bilgi_tip    (getstring "\n Tip     : "))
(setq bilgi_a      (getstring "\n A       : "))
(setq bilgi_b      (getstring "\n B       : "))
(setq bilgi_c      (getstring "\n C       : "))
(setq bilgi_d      (getstring "\n D       : "))
(setq bilgi_e      (getstring "\n E       : "))
(setq bilgi_f      (getstring "\n F       : "))
(setq bilgi_r      (getstring "\n R       : "))
)
(progn
(setq bilgi_poz    (a_oku en1 "POZ"          ))
(setq bilgi_cap    (cap_oku_isle (a_oku en1 "TB")))
(setq bilgi_boy    (a_oku en1 "BOY"          ))
(setq bilgi_tip    (a_oku en1 "TIP"          ))
(setq bilgi_a      (a_oku en1 "A"            ))
(setq bilgi_b      (a_oku en1 "B"            ))
(setq bilgi_c      (a_oku en1 "C"            ))
(setq bilgi_d      (a_oku en1 "D"            ))
(setq bilgi_e      (a_oku en1 "E"            ))
(setq bilgi_f      (a_oku en1 "F"            ))
(setq bilgi_r      (a_oku en1 "R"            ))
)
)
(setq i 0)
(princ "\n Bu poza benzetilecek...diger pozlari sec...")
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(command "_.Undo" "_BE")
(while(< i (sslength eset))
(setq en2 (ssname eset i))
(if (= bilgi_poz (a_oku en2 "POZ"   ))
(progn
(if (> (strlen (aralik_oku_isle (a_oku en2 "TB"  ))) 0) (setq bolu_isareti "/") (setq bolu_isareti ""))
(setq tb (strcat (substr (a_oku en2 "TB") 1 (vl-string-search fi (a_oku en2 "TB"))) fi bilgi_cap bolu_isareti (aralik_oku_isle (a_oku en2 "TB"  ))))
(att_yaz en2 "POZ"     bilgi_poz    )
(att_yaz en2 "TB"      tb           )
(att_yaz en2 "BOY"     bilgi_boy    )
(att_yaz en2 "TIP"     bilgi_tip    )
(att_yaz en2 "A"       bilgi_a      )
(att_yaz en2 "B"       bilgi_b      )
(att_yaz en2 "C"       bilgi_c      )
(att_yaz en2 "D"       bilgi_d      )
(att_yaz en2 "E"       bilgi_e      )
(att_yaz en2 "F"       bilgi_f      )
(att_yaz en2 "R"       bilgi_r      )
(poz_sekil_topla en2)
)
)
(setq i (1+ i))
)
(command "_.Undo" "_E")
(princ)
)
;------------------------------------------------------------------------------------------------------------------------------
(defun c:degis (/)
(fi_)
(command "_.Undo" "_BE")
(princ "\n Degisiklik uygulanacak pozlari seciniz.... ")
(setq eset (ssget (list (cons 0 "INSERT") (cons 2 "RL-POS"))))
(if (= eset nil) (exit))
(setq i 0)
(setq degis_bilgi (strcase (getstring (strcat "\n Degisecek bilgi : POZ, ADET2, ADET, CAP, ARALIK, BOY, TIP, A, B, C, D, E, F, R, GC : "))))
(if (= degis_bilgi "") (exit))
;
(setq eski_deger  (getstring (strcat "\n Eski deger...: ")))
(setq yeni_deger  (getstring (strcat "\n Yeni deger...: ")))
;
(while(< i (sslength eset))
;
(setq en (ssname eset i))
;
  (setq poz          (a_oku en "POZ" ))
  (setq tb           (a_oku en "TB"  ))
  (setq adet_carp    (poz_edit_adet_carpi tb))
  (setq adet         (poz_edit_adet       tb))
  (setq cap          (poz_edit_cap        tb))
  (setq aralik       (poz_edit_aralik     tb))
  (setq boy          (a_oku en "BOY" ))
  (setq tip          (a_oku en "TIP" ))
  (setq BB_A         (a_oku en "A"   ))
  (setq BB_B         (a_oku en "B"   ))
  (setq BB_C         (a_oku en "C"   ))
  (setq BB_D         (a_oku en "D"   ))
  (setq BB_E         (a_oku en "E"   ))
  (setq BB_F         (a_oku en "F"   ))
  (setq BB_R         (a_oku en "R"   ))
  (setq aciklama     (a_oku en "NOT" ))
  (setq grup_carpani (a_oku en "GC"  ))
;
(cond
  ((and (= degis_bilgi "POZ")    (= poz          eski_deger)) (progn (setq poz          yeni_deger)(att_yaz en "POZ"     poz       )))
  ((and (= degis_bilgi "ADET2")  (= adet_carp    eski_deger)) (progn (setq adet_carp    yeni_deger)
                                                                     (if (> (strlen adet_carp) 0) (setq carpi_isareti "x") (setq carpi_isareti ""))
                                                                     (if (> (strlen aralik   ) 0) (setq bolu_isareti  "/") (setq bolu_isareti  ""))
                                                                     (att_yaz en "TB" (strcat adet_carp carpi_isareti adet fi cap bolu_isareti aralik))))
  ((and (= degis_bilgi "ADET")   (= adet         eski_deger)) (progn (setq adet         yeni_deger)
                                                                     (if (> (strlen adet_carp) 0) (setq carpi_isareti "x") (setq carpi_isareti ""))
                                                                     (if (> (strlen aralik   ) 0) (setq bolu_isareti  "/") (setq bolu_isareti  ""))
                                                                     (att_yaz en "TB" (strcat adet_carp carpi_isareti adet fi cap bolu_isareti aralik))))
  ((and (= degis_bilgi "CAP")    (= cap          eski_deger)) (progn (setq cap          yeni_deger)
                                                                     (if (> (strlen adet_carp) 0) (setq carpi_isareti "x") (setq carpi_isareti ""))
                                                                     (if (> (strlen aralik   ) 0) (setq bolu_isareti  "/") (setq bolu_isareti  ""))
                                                                     (att_yaz en "TB" (strcat adet_carp carpi_isareti adet fi cap bolu_isareti aralik))))
  ((and (= degis_bilgi "ARALIK") (= aralik       eski_deger)) (progn (setq aralik       yeni_deger)
                                                                     (if (> (strlen adet_carp) 0) (setq carpi_isareti "x") (setq carpi_isareti ""))
                                                                     (if (> (strlen aralik   ) 0) (setq bolu_isareti  "/") (setq bolu_isareti  ""))
                                                                     (att_yaz en "TB"     (strcat adet_carp carpi_isareti adet fi cap bolu_isareti aralik))
                                                                     (att_yaz en "ARALIK" aralik)                                                          ))
  ((and (= degis_bilgi "BOY")    (= boy          eski_deger)) (progn (setq boy          yeni_deger)(att_yaz en "BOY"     boy          )))
  ((and (= degis_bilgi "TIP")    (= tip          eski_deger)) (progn (setq tip          yeni_deger)(att_yaz en "TIP"     tip          )))
  ((and (= degis_bilgi "A")      (= BB_A         eski_deger)) (progn (setq BB_A         yeni_deger)(att_yaz en "A"       BB_A         )))
  ((and (= degis_bilgi "B")      (= BB_B         eski_deger)) (progn (setq BB_B         yeni_deger)(att_yaz en "B"       BB_B         )))
  ((and (= degis_bilgi "C")      (= BB_C         eski_deger)) (progn (setq BB_C         yeni_deger)(att_yaz en "C"       BB_C         )))
  ((and (= degis_bilgi "D")      (= BB_D         eski_deger)) (progn (setq BB_D         yeni_deger)(att_yaz en "D"       BB_D         )))
  ((and (= degis_bilgi "E")      (= BB_E         eski_deger)) (progn (setq BB_E         yeni_deger)(att_yaz en "E"       BB_E         )))
  ((and (= degis_bilgi "F")      (= BB_F         eski_deger)) (progn (setq BB_F         yeni_deger)(att_yaz en "F"       BB_F         )))
  ((and (= degis_bilgi "R")      (= BB_R         eski_deger)) (progn (setq BB_R         yeni_deger)(att_yaz en "R"       BB_R         )))
  ((and (= degis_bilgi "GC")     (= grup_carpani eski_deger)) (progn (setq grup_carpani yeni_deger)(att_yaz en "GC"      grup_carpani )))
  ( T                    (princ)          )
)
  (poz_sekil_topla en)
  (setq i (1+ i))
)
(command "_.Undo" "_E")
(gc)
(gc)
(gc)
(princ)
)
;------------------------------------------------------------------------------------------------------------------------------
;Diger komutlardan farkli olarak bu komut butun attribute tanimli elemanlar arasinda bilgi aktarmak icin kullanilabilir.
;
(defun c:tddk (/)
(vl-load-com)
(setq en1 (car (entsel "\n Okunacak Poz' u sec... "        )))
(setq blk  (vlax-ename->vla-object en1))
(setq atts (vlax-invoke blk 'getattributes))
(princ "\n Degistirilecek Pozlari sec... " )
(setq B (ssget (list (cons 0 "INSERT"))))
(while (/= b nil)
 (setq i 0)
 (setq en2 (ssname B i))
  (while (< i (sslength b))
      (foreach att atts
       (setq pn (vlax-get-property att 'TagString))
       (setq pv (vlax-get-property att 'TextString))
       (att_yaz en2 pn pv)
       (if (= (cdr (assoc 2 (entget en2))) "RL-POS") (poz_sekil_topla en2))
      )
   (setq i (1+ i))
   (setq en2 (ssname B i))
  )
 (princ "\n Degistirilecek Pozlari sec... " )
 (setq B (ssget (list (cons 0 "INSERT"))))
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun a_oku (en tag /)
(vl-load-com)
(setq tag (strcase tag))
(setq deger "")
(setq blk  (vlax-ename->vla-object en))
(setq atts (vlax-invoke blk 'getattributes))
(foreach att atts
    (if (= tag (vla-get-tagstring att)) (setq deger (vla-get-textstring att)))
)
 deger
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun att_prop_oku (en tag prop /)
(vl-load-com)
(setq deger "")
(setq block  (vlax-ename->vla-object en))
(setq tag (strcase tag))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq tag (strcase (vla-get-TagString attrib))) (setq deger (vlax-get-property attrib prop))))
deger
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun att_yaz (en tag value)
(vl-load-com)
(setq block  (vlax-ename->vla-object en))
(setq tag (strcase tag))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq tag (strcase (vla-get-TagString attrib))) (progn (vla-put-TextString attrib value) value)))
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun att_prop_yaz (en tag prop value)
(vl-load-com)
(setq block  (vlax-ename->vla-object en))
(setq tag (strcase tag))
(foreach attrib (vlax-invoke block 'GetAttributes) (if (eq tag (strcase (vla-get-TagString attrib))) (vlax-put-property attrib prop value)))
)
; Properties
;
; Alignment
; Application
; Backward
; Constant
; Document
; FieldLength
; Handle
; HasExtensionDictionary
; Height
; Hyperlinks
; InsertionPoint
; Invisible
; Layer
; Linetype
; LinetypeScale
; Lineweight
; LockPosition
; MTextAttribute
; MTextAttributeContent
; MTextBoundaryWidth
; MTextDrawingDirection
; Normal
; ObjectID
; ObjectName
; ObliqueAngle
; OwnerID
; PlotStyleName
; Rotation
; ScaleFactor
; StyleName
; TagString
; TextAlignmentPoint
; TextGenerationFlag
; TextString
; Thickness
; TrueColor
; UpsideDown
; Visible
;
;--------------------------------------------------------------------------------------------------------------------------------------------------------
; cp Donati capi mm olarak verilecek sonuc kg/m olarak cikacaktir.
(defun cp_ag ( cp / cp ) (* 0.00616537558266997 (* cp cp)))
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun r_b_h (cap) (if (> cap 20) (* cap 3.5) (* cap 2.0)))
(defun m_b_h (cap) (if (> cap 20) (* cap 7.0) (* cap 4.0)))
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun PZ_TUM_coz ( / s1 s2 i n l1 l2 eleman deger boy dosya_adi)
(setq s1 ";")
(setq grup nil)
(setq grup_liste nil)
(setq PZ_TUM nil)
(setq dosya_adi (strcat source_path "Standard/PZ_TUM.txt" ))
(setq dosya (open dosya_adi "r"))
(setq s2    (read-line dosya))
 (while (/= s2 nil)
   (setq i           1  )
   (setq n           1  )
   (setq l1 (strlen s1) )
   (setq l2 (strlen s2) )
   (setq eleman    nil  )
       (while (<= i l2)
          (if (= (substr s2 i l1) s1)
             (progn
               (setq boy  (- i n 1))
               (if (< boy 0 ) (setq boy 0))
               (setq deger    (substr s2 (+ n 1) boy))
               (setq eleman   (cons deger eleman))
               (setq n  i )
             )
          )
          (setq i (+ i 1) )
       )
    (setq eleman    (reverse eleman) )
    (setq s2       (read-line dosya) )
      (if (/= eleman nil )
        (progn
        (setq grup       (cons eleman grup))
        (setq grup_liste (cons (cadr eleman) grup_liste))
        )
      )
 )
(close dosya)
(princ)
    (setq tip_liste (reverse grup_liste))
    (setq PZ_TUM    (reverse grup))
)
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
(defun min_boy (boy / )
(setq boy_isle_2 (vl-string-translate "-~" "  " boy))
  (if (= boy boy_isle_2)
    (setq boy boy_isle_2)
    (setq boy (rtos (eval (read (strcat "(min " boy_isle_2 ")"))) 2 0))
  )
boy
)
;-------------------------------------------------------------------------------
(defun max_boy (boy / )
(setq boy_isle_2 (vl-string-translate "-~" "  " boy))
  (if (= boy boy_isle_2)
    (setq boy boy_isle_2)
    (setq boy (rtos (eval (read (strcat "(max " boy_isle_2 ")"))) 2 0))
  )
boy
)
;-------------------------------------------------------------------------------
(defun b_h (tip A B C D E F BR cap / )
(setq a_min (ATOF (min_boy A)))
(setq a_max (ATOF (max_boy A)))
(setq b_min (ATOF (min_boy B)))
(setq b_max (ATOF (max_boy B)))
(setq c_min (ATOF (min_boy C)))
(setq c_max (ATOF (max_boy C)))
(setq d_min (ATOF (min_boy D)))
(setq d_max (ATOF (max_boy D)))
(setq e_min (ATOF (min_boy E)))
(setq e_max (ATOF (max_boy E)))
(setq f_min (ATOF (min_boy F)))
(setq f_max (ATOF (max_boy F)))
(setq r (r_b_h cap))
(if (= PZ_TUM nil) (PZ_TUM_coz)) ; Boy hesabi icin gerekli katsayilarin listesi olusturuluyor.
(setq j 0)
(while (and (< j (length tip_liste))(/= tip (nth j tip_liste))) (setq j (1+ j)))
(if (= tip (nth j tip_liste))
    (progn
    (setq CARP_A   (atof (nth  2 (nth j PZ_TUM))))
    (setq CARP_B   (atof (nth  3 (nth j PZ_TUM))))
    (setq CARP_C   (atof (nth  4 (nth j PZ_TUM))))
    (setq CARP_D   (atof (nth  5 (nth j PZ_TUM))))
    (setq CARP_E   (atof (nth  6 (nth j PZ_TUM))))
    (setq CARP_F   (atof (nth  7 (nth j PZ_TUM))))
    (setq CARP_BR  (atof (nth  8 (nth j PZ_TUM))))
    (setq CARP_r   (atof (nth  9 (nth j PZ_TUM))))
    (setq CARP_cap (atof (nth 10 (nth j PZ_TUM))))
    (setq boy_hesap_min (+ (* CARP_A A_min) (* CARP_B B_min) (* CARP_C C_min) (* CARP_D D_min) (* CARP_E E_min) (* CARP_F F_min) (* CARP_BR BR)))
    (setq boy_hesap_max (+ (* CARP_A A_max) (* CARP_B B_max) (* CARP_C C_max) (* CARP_D D_max) (* CARP_E E_max) (* CARP_F F_max) (* CARP_BR BR)))
    (if (= tip "77") (progn (setq boy_hesap_min (* a_min boy_hesap_min)) (setq boy_hesap_max (* a_max boy_hesap_max))))
    (if (= boy_hesap_min boy_hesap_max)
        (setq boy_hesap (strcat "L="  (RTOS boy_hesap_min 2 0)))
        (setq boy_hesap (strcat "L="  (RTOS boy_hesap_min 2 0) "~" (RTOS boy_hesap_max 2 0)))
    )
    ;
    ; (+ (* CARP_A A) (* CARP_B B) (* CARP_C C) (* CARP_D D) (* CARP_E E) (* CARP_F F) (* CARP_BR BR) (* CARP_r r) (* CARP_cap cap))
    ; BS8666:2005 e göre boy hesabi icin ustteki formul kullanilmalidir. Biz burada bukum son iki bileseni cikardik, boylece donati boyu daha uzun hesaplanacaktýr.
    ;
   )
    (setq boy_hesap "L=")
  )


boy_hesap
)
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
; A string that identifies a symbol table.
; Valid table-name values are
; "LAYER", "LTYPE", "VIEW", "STYLE", "BLOCK", "UCS", "APPID", "DIMSTYLE", and "VPORT".
; The argument is not case sensitive.
(DEFUN donati_metraj_yazi_sitili_yap ( stil_name font_name withfactor / )
(setq uzanti (strcase (substr font_name (- (strlen font_name) 2 ) (strlen font_name))))
(setq font_name (findfile (strcat source_path "Fonts/" font_name )))
(vl-load-com)
(if (= (tblsearch "STYLE" stil_name ) nil)
          (progn
           (princ "\n Stil mevcut deðil...")
           (entmake (list (cons    0  "STYLE"         )
               (cons  100  "AcDbSymbolTableRecord"    )
               (cons  100  "AcDbTextStyleTableRecord" )
               (cons   40  0                          )
               (cons   41  withfactor                 )
               (cons   70  0                          )
               (cons   50  0.1745326666666667         )
               (cons    2  stil_name                  )
               (cons    3  font_name                  ))
           )
          )
          (progn
           (princ "\n Stil mevcut...")
            (SETVAR "CMDECHO" 0)
             ;(if (= (strcase uzanti) "SHX") (COMMAND "-STYLE" stil_name font_name 0 withfactor 0 "No" "No" "No" ))
             ;(if (= (strcase uzanti) "TTF") (COMMAND "-STYLE" stil_name font_name 0 withfactor 0 "No" "No" ))
            (SETVAR "CMDECHO" 1)
          )
)
(princ)
)
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
;-------------------------------------------------------------------------------
(defun donati_metraj_layer_olustur  (layer_name color line_type / layer_name color line_type layer_ent)
(vl-load-com)
(if (= line_type "") (setq line_type "Continuous"))
(if (= (tblsearch "LTYPE" line_type ) nil) ( command "-linetype" "L" line_type (strcat source_path "Linetype/ekle.lin") ""))
(if (= (tblsearch "LAYER" layer_name) nil)
          (command "-LAYER" "N" layer_name  "C" color layer_name "L" line_type layer_name "")
          (command "-LAYER" "T" layer_name "ON" layer_name "C" color layer_name "L" line_type layer_name "")
)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun metraj_tablo_dosya_yaz (cap_liste toplam_bilgi / en_son_toplam_agirlik anlik_tarih fname template_file cll csht rang sht nwb awb aexc)
(setq anlik_tarih (RTOS (GETVAR "CDATE") 2 6))
(setq template_file (strcat source_path "Standard/RL-POS.xls"))
(setq fname (strcat (getvar "dwgprefix") (substr (getvar "dwgname") 1 (- (strlen (getvar "dwgname")) 4 )) "-" anlik_tarih ".xls"))
(vl-file-copy template_file fname)
(setq aexc  (vlax-get-or-create-object "Excel.Application")) (if (null aexc) (progn (alert "Excel baclatilamadi.") (exit)))
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
 (t_y_k yerlesim (strcat "Metraj dosyasi : " anlik_tarih) (* 0.25 yzy2) 0 0)
(princ (strcat "\n Metraj tablosu, " fname " adiyla diske yazildi" ))
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun boy_oku_isle (boy / )
(setq boy_isle_1 (vl-string-translate "lL=" "   " boy))
(setq boy_isle_2 (vl-string-translate "-~" "  " boy_isle_1))
  (if (= boy_isle_1 boy_isle_2)
    (setq boy boy_isle_1)
    (setq boy (rtos (eval (read (strcat "(* 0.5 (+ " boy_isle_2 "))"))) 2 2))
  )
boy
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun boy_a_oku_isle (boy / )
(setq boy_isle_2 (vl-string-translate "-~" "  " boy))
  (if (= boy boy_isle_2)
    (setq boy boy_isle_2)
    (setq boy (rtos (eval (read (strcat "(* 0.5 (+ " boy_isle_2 "))"))) 2 0))
  )
boy
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun adet_oku_isle (tb / )
(fi_)
(setq fi_boyu (strlen fi))
(setq fi_yeri (vl-string-search fi tb))
(setq adet_isle_1 (vl-string-translate "xX" "  " (substr tb 1 fi_yeri)))
  (if (= adet adet_isle_1)
    (setq adet adet_isle_1)
    (setq adet (rtos (eval (read (strcat "(* " adet_isle_1 ")"))) 2 0))
  )
adet
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun cap_oku_isle (tb / )
(fi_)
(setq fi_boyu (strlen fi))
(setq tb_boyu (strlen tb))
(setq fi_yeri (vl-string-search fi   tb      ))
(setq b_yeri  (vl-string-search "/"  tb      ))
(if (= b_yeri nil) (setq b_yeri (+ 1 (strlen tb))))
(setq cap (substr tb (+ 1 fi_boyu fi_yeri)  (- b_yeri  fi_yeri fi_boyu)))
(setq cap (itoa (atoi cap)))
cap
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun aralik_oku_isle (tb / )
(fi_)
(setq fi_boyu  (strlen fi))
(setq tb_boyu  (strlen tb))
(setq b_yeri   (vl-string-search "/"  tb      ))
(if (/= b_yeri nil)
        (setq aralik_b   (substr tb (+ 2 b_yeri )  (- tb_boyu b_yeri)))
        (setq aralik_b ""))
aralik_b
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
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun toplam_bilgi_olustur ( eset / )
;---------------------------------------------------------------------------------------
; Bu kisim metraji cikarilacak elemanlardan bilgilerin toplanmasi icindir.
; Daha sonraki gelistirmeler sirasinda bu bolumde toplanan bilgilerin uygun
; standartta olmass ve bu bilgilerin farkls kaynaklara adaptasyonu icin
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
; Bu kisim metraji cikarilacak elemanlardan bilgilerin toplanmasc icindir.
; Daha sonraki gelistirmeler sirasinda bu bolumde toplanan bilgilerin uygun
; standartta olmass ve bu bilgilerin farkls kaynaklara adaptasyonu icin
; bu bolumde gelistirme yapilmalidir.
;--------------------------------------------------------------------------------------------------------------------------
toplam_bilgi
)
(defun toplam_bilgi_derle ( toplam_bilgi / )
;---------------------------------------------------------------------------------------
; Bu kisim toplanmsi bilginin derlenmesi ve gruplandirilmasi icin ilerde gelistirilecek.
; Toplam bilgi icin her poza ait toplam adetin cikarilmasi isi burada yapýliyor.
; Buradan cikan liste metraj tablosunun dusey satirlarini olusturacak.
;
(setq i 0)
(setq bilgi_adet (length toplam_bilgi))
(while(< i bilgi_adet)
(if (and (= (cdr (assoc 1 (nth i toplam_bilgi))) (cdr (assoc 1 (nth (+ i 1) toplam_bilgi))))
         (= (cdr (assoc 3 (nth i toplam_bilgi))) (cdr (assoc 3 (nth (+ i 1) toplam_bilgi))))
         (= (cdr (assoc 5 (nth i toplam_bilgi))) (cdr (assoc 5 (nth (+ i 1) toplam_bilgi)))))
(progn
; Poz ayni ise  "
(setq adet_topla (itoa (+ (atoi (cdr (assoc 2 (nth i toplam_bilgi)))) (atoi (cdr (assoc 2 (nth (+ i 1) toplam_bilgi)))))))
(setq toplam_bilgi (subst (subst (cons 2 adet_topla) (assoc 2 (nth i toplam_bilgi)) (nth i toplam_bilgi)) (nth i toplam_bilgi) toplam_bilgi))
(setq toplam_bilgi (vl-remove (nth (+ i 1) toplam_bilgi) toplam_bilgi))
(setq bilgi_adet (- bilgi_adet 1))
(setq i (- i 1))
)
; Poz farkli ise "
)
(setq i (+ i 1))
)
;
; Bu kisim toplanmis bilginin derlenmesi ve gruplandirilmasi icin ilerde gelistirilecek.
; Toplam bilgi icin he poza ait toplam adetin cikarilmasi isi burada yapiliyor.
; Buradan cikan liste metraj tablosunun dusey satirlarini olusturacak.
;---------------------------------------------------------------------------------------
toplam_bilgi
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
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun b_h_check (poz tip A B C D E F r1 / )
(setq a1 (ATOF (min_boy A)))
(setq b1 (ATOF (min_boy B)))
(setq c1 (ATOF (min_boy C)))
(setq d1 (ATOF (min_boy D)))
(setq e1 (ATOF (min_boy E)))
(setq f1 (ATOF (min_boy F)))
(setq j 0)
(while (and (< j (length tip_liste))(/= tip (nth j tip_liste))) (setq j (1+ j)))
                                 (if (= tip (nth j tip_liste))
                                     (progn
                                      (setq a2   (atof (nth  2 (nth j PZ_TUM))))
                                      (setq b2   (atof (nth  3 (nth j PZ_TUM))))
                                      (setq c2   (atof (nth  4 (nth j PZ_TUM))))
                                      (setq d2   (atof (nth  5 (nth j PZ_TUM))))
                                      (setq e2   (atof (nth  6 (nth j PZ_TUM))))
                                      (setq f2   (atof (nth  7 (nth j PZ_TUM))))
                                      (setq r2   (atof (nth  8 (nth j PZ_TUM))))
                                     )
                                 )
(if (and ( = a1 0) (/= a2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " A bukum boyu girilmemis.")   ))
(if (and (/= a1 0) ( = a2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " A bukum boyu carpani sifir.")))
(if (and ( = b1 0) (/= b2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " B bukum boyu girilmemis.")   ))
(if (and (/= b1 0) ( = b2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " B bukum boyu carpani sifir.")))
(if (and ( = c1 0) (/= c2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " C bukum boyu girilmemis.")   ))
(if (and (/= c1 0) ( = c2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " C bukum boyu carpani sifir.")))
(if (and ( = d1 0) (/= d2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " D bukum boyu girilmemis.")   ))
(if (and (/= d1 0) ( = d2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " D bukum boyu carpani sifir.")))
(if (and ( = e1 0) (/= e2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " E bukum boyu girilmemis.")   ))
(if (and (/= e1 0) ( = e2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " E bukum boyu carpani sifir.")))
(if (and ( = f1 0) (/= f2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " F bukum boyu girilmemis.")   ))
(if (and (/= f1 0) ( = f2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " F bukum boyu carpani sifir.")))
(if (and ( = r1 0) (/= r2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " R bukum boyu girilmemis.")   ))
(if (and (/= r1 0) ( = r2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " R bukum boyu carpani sifir.")))
;
(if (and ( = a1 0) (/= a2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " A bukum boyu girilmemis.")   dosya))
(if (and (/= a1 0) ( = a2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " A bukum boyu carpani sifir.")dosya))
(if (and ( = b1 0) (/= b2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " B bukum boyu girilmemis.")   dosya))
(if (and (/= b1 0) ( = b2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " B bukum boyu carpani sifir.")dosya))
(if (and ( = c1 0) (/= c2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " C bukum boyu girilmemis.")   dosya))
(if (and (/= c1 0) ( = c2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " C bukum boyu carpani sifir.")dosya))
(if (and ( = d1 0) (/= d2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " D bukum boyu girilmemis.")   dosya))
(if (and (/= d1 0) ( = d2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " D bukum boyu carpani sifir.")dosya))
(if (and ( = e1 0) (/= e2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " E bukum boyu girilmemis.")   dosya))
(if (and (/= e1 0) ( = e2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " E bukum boyu carpani sifir.")dosya))
(if (and ( = f1 0) (/= f2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " F bukum boyu girilmemis.")   dosya))
(if (and (/= f1 0) ( = f2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " F bukum boyu carpani sifir.")dosya))
(if (and ( = r1 0) (/= r2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " R bukum boyu girilmemis.")   dosya))
(if (and (/= r1 0) ( = r2 0)) (princ (strcat "\n No: " poz ", Tip: " tip " R bukum boyu carpani sifir.")dosya))
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
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun hata_kontrol ( toplam_bilgi / )
(if (= PZ_TUM nil) (PZ_TUM_coz))
;--------------------------------------------------------------------------------------------------------------------------
; Eleman hata kontrolu...
; Karsilastirmala kontroller
;(setq hata_dosya (strcat (getvar "dwgprefix") (substr (getvar "dwgname") 1 (- (strlen (getvar "dwgname")) 4 )) "-met_error.txt"))
;(setq hata_dosya (strcat (getvar "dwgprefix") "error.txt" ))
(setq hata_dosya (strcat client_path "error.txt" ))
(setq dosya (open hata_dosya "w"))
(setq i 0)
(repeat  (- (length toplam_bilgi) 1)
(if          (and ( = (cdr (assoc  1 (nth i toplam_bilgi))) (cdr (assoc  1 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc  3 (nth i toplam_bilgi))) (cdr (assoc  3 (nth (+ i 1) toplam_bilgi)))))
(progn
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Cap hatasi var. Duzeltip yeniden metraj yapin....! "))
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: cap hatasi var. Duzeltip yeniden metraj yapin....! ") dosya)
)
)
(if          (and ( = (cdr (assoc  1 (nth i toplam_bilgi))) (cdr (assoc  1 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc  5 (nth i toplam_bilgi))) (cdr (assoc  5 (nth (+ i 1) toplam_bilgi)))))
(progn
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Toplam boy hatasi var. Duzeltip yeniden metraj yapin....! "))
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Toplam boy hatasi var. Duzeltip yeniden metraj yapin....! ") dosya)
)
)
(if          (and ( = (cdr (assoc  1 (nth i toplam_bilgi))) (cdr (assoc  1 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc  6 (nth i toplam_bilgi))) (cdr (assoc  6 (nth (+ i 1) toplam_bilgi)))))
(progn
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Tip hatasi var. Duzeltip yeniden metraj yapin....! "))
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Tip hatasi var. Duzeltip yeniden metraj yapin....! ") dosya)
)
)
(if          (and ( = (cdr (assoc  1 (nth i toplam_bilgi))) (cdr (assoc  1 (nth (+ i 1) toplam_bilgi))))
              (or
                  (/= (cdr (assoc  7 (nth i toplam_bilgi))) (cdr (assoc  7 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc  8 (nth i toplam_bilgi))) (cdr (assoc  8 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc  9 (nth i toplam_bilgi))) (cdr (assoc  9 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc 10 (nth i toplam_bilgi))) (cdr (assoc 10 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc 11 (nth i toplam_bilgi))) (cdr (assoc 11 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc 12 (nth i toplam_bilgi))) (cdr (assoc 12 (nth (+ i 1) toplam_bilgi))))
                  (/= (cdr (assoc 13 (nth i toplam_bilgi))) (cdr (assoc 13 (nth (+ i 1) toplam_bilgi))))))
(progn
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Bukum boylarinda hata var. Duzeltip yeniden metraj yapin....! "))
(princ   (strcat "\n "(cdr (assoc  1 (nth i toplam_bilgi))) ".NoPoz: Bukum boylarinda hata var. Duzeltip yeniden metraj yapin....! ") dosya)
)
)
(setq i (1+ i))
(princ)
)
; Bireysel kontroller.
(setq i 0)
(repeat  (length toplam_bilgi)
; Büküm boyu kontrolleri.
(b_h_check
                                   (cdr (assoc  1 (nth i toplam_bilgi)))
                                   (cdr (assoc  6 (nth i toplam_bilgi)))
                                   (cdr (assoc  7 (nth i toplam_bilgi)))
                                   (cdr (assoc  8 (nth i toplam_bilgi)))
                                   (cdr (assoc  9 (nth i toplam_bilgi)))
                                   (cdr (assoc 10 (nth i toplam_bilgi)))
                                   (cdr (assoc 11 (nth i toplam_bilgi)))
                                   (cdr (assoc 12 (nth i toplam_bilgi)))
                             (atof (cdr (assoc 13 (nth i toplam_bilgi))))
)
; Toplam boy hesabýnýn doðruluðu kontrolü
(setq boy (atof (boy_oku_isle (b_h (cdr (assoc  6 (nth i toplam_bilgi)))
                                   (cdr (assoc  7 (nth i toplam_bilgi)))
                                   (cdr (assoc  8 (nth i toplam_bilgi)))
                                   (cdr (assoc  9 (nth i toplam_bilgi)))
                                   (cdr (assoc 10 (nth i toplam_bilgi)))
                                   (cdr (assoc 11 (nth i toplam_bilgi)))
                                   (cdr (assoc 12 (nth i toplam_bilgi)))
                             (atof (cdr (assoc 13 (nth i toplam_bilgi))))
                             (atof (cdr (assoc  3 (nth i toplam_bilgi))))))))
(if (and (/= (cdr (assoc  6 (nth i toplam_bilgi))) "99") (/= (fix boy)  (cdr (assoc 14 (nth i toplam_bilgi)))))
(progn
(princ   (strcat "\n "(cdr (assoc 1 (nth i toplam_bilgi))) ".NoPoz: Bukum boylariyla hesaplanan boy <L=" (itoa (fix boy)) "> mevcuttan <L=" (itoa (cdr (assoc 14 (nth i toplam_bilgi)))) "> farkli...! "))
(princ   (strcat "\n "(cdr (assoc 1 (nth i toplam_bilgi))) ".NoPoz: Bukum boylariyla hesaplanan boy <L=" (itoa (fix boy)) "> mevcuttan <L=" (itoa (cdr (assoc 14 (nth i toplam_bilgi)))) "> farkli...! ") dosya)
)
)
; Toplam boyun sýfýr olmasý kontrolü
(if (= (cdr (assoc 14 (nth i toplam_bilgi))) 0)
(progn
(princ   (strcat "\n "(cdr (assoc 1 (nth i toplam_bilgi))) ".NoPoz: Toplam boy L=0 ! "))
(princ   (strcat "\n "(cdr (assoc 1 (nth i toplam_bilgi))) ".NoPoz: Toplam boy L=0 ! ") dosya)
)
)
; Toplam boyun donati maksimum boyu nu geçmesi kontrolü
;
(if (> (* olcu_carp (cdr (assoc 14 (nth i toplam_bilgi)))) (+ 0.001 donati_maksimum_boyu))
(progn
(princ   (strcat "\n "(cdr (assoc 1 (nth i toplam_bilgi))) ".NoPoz: Bukum boylariyla hesaplanan boy " (rtos donati_maksimum_boyu 2 2) " m yi geciyor ! "))
(princ   (strcat "\n "(cdr (assoc 1 (nth i toplam_bilgi))) ".NoPoz: Bukum boylariyla hesaplanan boy " (rtos donati_maksimum_boyu 2 2) " m yi geciyor ! ") dosya)
)
)
(setq i (1+ i))
)
; Hata kontrolleri sonu
;--------------------------------------------------------------------------------------------------------------------------
(close dosya)
(princ)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
;--------------------------------------------------------------------------------------------------------------------------------------------------------
(defun c:tddh ( / pn1 )
(setq yuk       (* 0.0020 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq kacma     (* 0.0010 (* (OLCEK_OKU) (BIRIM_OKU))))
(setq pn1 (getpoint "\n Metraj hata dosyasini yazdirmak istediginiz noktayi gosteriniz...."))
;(setq hata_dosya (strcat (getvar "dwgprefix") (substr (getvar "dwgname") 1 (- (strlen (getvar "dwgname")) 4 )) "-met_error.txt"))
;(setq hata_dosya (strcat (getvar "dwgprefix") "error.txt" ))
(setq hata_dosya (strcat client_path "error.txt" ))
(setq dosya (open hata_dosya "r"))
(setq text (read-line dosya))
(while (not (null text))
(setq pn1 (polar pn1 (* 1.5 pi) (+ yuk (* 2.5 kacma))))
(setq text (read-line dosya))
(command "TEXT" pn1 yuk "" text)
)
(close dosya)
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------

(defun cap_bilgi_olustur ( toplam_bilgi / )
;---------------------------------------------------------------------------------------
;
; Cap listesinin oluSturulmasi ve bu listedeki caplara giren tum pozlardaki donati
; boylarinin hesaplanmasi burada yapilacak
;
(setq i 0)
(setq cap_liste (list))
(while(< i (length toplam_bilgi))
(setq cap  (cdr (nth  3 (nth i toplam_bilgi))))
(setq adet (cdr (nth  2 (nth i toplam_bilgi))))
(setq boy  (cdr (nth 14 (nth i toplam_bilgi))))
(setq toplam_boy (rtos (* (atof adet) boy ) 2 8))
(setq cap_ekle (list (cons 0 i ) (cons 1 cap) (cons 2 toplam_boy)))
(setq cap_liste (cons cap_ekle cap_liste))
(setq i (+ i 1))
)
;
; Caplar siraya diziliyor.
;
(setq cap_liste (vl-sort cap_liste (function (lambda (e1 e2) (< (atoi (cdr (assoc 1 e1))) (atoi (cdr (assoc 1 e2))))))))
;
(setq i 0)
(setq bilgi_adet (length cap_liste))
(while(< i bilgi_adet)
(if (= (cdr (assoc 1 (nth i cap_liste))) (cdr (assoc 1 (nth (+ i 1) cap_liste))))
(progn
(setq adet_topla (rtos (+ (atof (cdr (assoc 2 (nth i cap_liste)))) (atof (cdr (assoc 2 (nth (+ i 1) cap_liste))))) 2 8))
(setq cap_liste  (subst (subst (cons 2 adet_topla) (assoc 2 (nth i cap_liste)) (nth i cap_liste)) (nth i cap_liste) cap_liste))
(setq cap_liste  (vl-remove (nth (+ i 1) cap_liste) cap_liste))
(setq bilgi_adet (- bilgi_adet 1))
(setq i (- i 1))
)
)
(setq i (+ i 1))
)
;
; Bu asamada cap_liste metraji cikarilacak olan elemanlarin icindeki tum caplari icerir
; Bu cap bilgileri ekinde o captan kac birim malzeme oldugu da vardir.
;--------------------------------------------------------------------------------------------------------------------------------------------------------
cap_liste
)
;--------------------------------------------------------------------------------------------------------------------------------------------------------
