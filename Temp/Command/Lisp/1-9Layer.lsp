; ------------------------------------------------------------------------------
; --- 1,2,3,4,5,6,7,8 LAYERLERI ILE ILGILI LISPLER -----------------------------
; --- Bu kýsýmda eleman verilen numaralý layer e geçer
; ------------------------------------------------------------------------------

(defun C:0    ( ) (num "0"))
(defun C:1    ( ) (num "1"))
(defun C:2    ( ) (num "2"))
(defun C:3    ( ) (num "3"))
(defun C:4    ( ) (num "4"))
(defun C:5    ( ) (num "5"))
(defun C:6    ( ) (num "6"))
(defun C:7    ( ) (num "7"))
(defun C:8    ( ) (num "8"))
(defun C:9    ( ) (num "9"))

; ------------------------------------------------------------------------------
; --- 1,2,3,4,5,6,7,8 LAYERLERI ILE ILGILI LISPLER -----------------------------
; --- Bu kýsýmda layer e numaralýo layer e geçilir
; ------------------------------------------------------------------------------

(defun C:L0   () (layer_e_gec "0" "255" "" ))
(defun C:L1   () (layer_e_gec "1" "1"   "" ))
(defun C:L2   () (layer_e_gec "2" "2"   "" ))
(defun C:L3   () (layer_e_gec "3" "3"   "" ))
(defun C:L4   () (layer_e_gec "4" "4"   "" ))
(defun C:L5   () (layer_e_gec "5" "5"   "" ))
(defun C:L6   () (layer_e_gec "6" "6"   "" ))
(defun C:L7   () (layer_e_gec "7" "7"   "" ))
(defun C:L8   () (layer_e_gec "8" "8"   "" ))
(defun C:L9   () (layer_e_gec "9" "9"   "" ))

; ------------------------------------------------------------------------------
; --- 1,2,3,4,5,6,7,8 LAYERLERI ILE ILGILI LISPLER -----------------------------
; --- Bu kýsýmda seçilen elemanýn rengi verilen numaraya döner (Layer deyil sadece eleman için)
; ------------------------------------------------------------------------------

(defun C:C0   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "255" ""))
(defun C:C1   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "1" ""))
(defun C:C2   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "2" ""))
(defun C:C3   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "3" ""))
(defun C:C4   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "4" ""))
(defun C:C5   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "5" ""))
(defun C:C6   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "6" ""))
(defun C:C7   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "7" ""))
(defun C:C8   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "8" ""))
(defun C:C9   ( / b ) (setq B (SSGET)) (command "CHPROP" B ""  "C" "9" ""))



(defun c:kalip ( / )
(setq osmode_old   (getvar "osmode"  ))
(setq clayer_old   (getvar "clayer"  ))
(setvar "osmode"   0)
(setq blk11 (entsel "\n Degismesi gereken kalibi seciniz..."))
(setq eleman (entget (car blk11)))
(setq ismi (cdr (assoc 2 eleman)))
(setq noktasi (cdr (assoc 10 eleman)))
(command "._erase" (car blk11) "")
(command "purge" "b" "*" "n")
(command "-insert" (strcat "//fs-proj/Projects/2-DESIGN_OFFICE_PROJECTS/PLOT 17-18/06_KJ/KJ3/Altliklar/"ismi".dwg") noktasi 1 "" 0 )
)


;(defun c:eal (/ lo loList)
;  (setvar "FILEDIA" 0)
;  (foreach lo (layoutlist)
;    (progn
;      (setvar "CTAB" lo)
;      (command "exportlayout" "")
;    )
;  )
;  (setvar "FILEDIA" 1)
;)
