;;;/////////////////////////////////////////////////////////
(defun DSTYLE3_FONT_STYLE (FSTY$ TWF# TOA# PFN$ BFN$)
  (if (null (tblsearch "style" FSTY$))
    (progn
      (COMMAND "-STYLE" FSTY$ TWF# TOA# PFN$ BFN$ "N" "N"  )

      ))
  (princ))

; Layer Create.
(defun DSTYLE3_LAYER_CREATE (LNAM$ LCLR# LTYP$ LWGT#)
  (if (null (tblsearch "layer" LNAM$))
    (progn
      (entmake
        (list
          (cons 0   "LAYER")
          (cons 100 "AcDbSymbolTableRecord")
          (cons 100 "AcDbLayerTableRecord")
          (cons 2    LNAM$)
          (cons 6    LTYP$)
          (cons 62   LCLR#)
          (cons 70   0)
          (cons 290  1)
          (cons 370  0)))))
  (princ))
; Main Function.
(defun c:dims (/ DCL_ID SNAM DSCL BUTTON)
  (or D::UNT (setq D::UNT "0"))
  (setq DUNT (list "1:1" "1:2" "1:4" "1:5" "1:8" "1:10" "1:15" "1:20" "1:25" "1:30" "1:40" "1:50" "1:75" "1:100" "1:150" "1:200"))
  (cond
    ( (<= (setq DCL_ID (load_dialog (strcat source_pathlisp"Lisp/Olcu_Sis.dcl"))) 0)
      (princ "\n** DCL File not Found **"))
    ( (not (new_dialog "main" DCL_ID))
      (princ "\n** Dialog could not be loaded **"))
    (t 
      (start_list "DUNT")(mapcar 'add_list DUNT)(end_list)
      (set_tile "DUNT" D::UNT)
      (action_tile "DUNT"   "(setq D::UNT $value)")
      (action_tile "accept" "(progn (setq dim_style_secim (get_tile \"DUNT_G\"))(setq BUTTON T)) (done_dialog)"  )
      (action_tile "cancel" "(done_dialog)(setq BUTTON nil)")
      (start_dialog)
      (unload_dialog DCL_ID)))
      (if BUTTON ( (princ dim_style_secim)
 (if (= dim_style_secim "1")
  (cond
   ((= D::UNT  "0") (progn (setq DSTY$ "ren_1"        ) (setq DSCL# 0.01 )))
   ((= D::UNT  "1") (progn (setq DSTY$ "ren_2"        ) (setq DSCL# 0.02 )))
   ((= D::UNT  "2") (progn (setq DSTY$ "ren_4"        ) (setq DSCL# 0.04 )))
   ((= D::UNT  "3") (progn (setq DSTY$ "ren_5"        ) (setq DSCL# 0.05 )))
   ((= D::UNT  "4") (progn (setq DSTY$ "ren_8"        ) (setq DSCL# 0.08 )))
   ((= D::UNT  "5") (progn (setq DSTY$ "ren_10"       ) (setq DSCL# 0.10 )))
   ((= D::UNT  "6") (progn (setq DSTY$ "ren_15"       ) (setq DSCL# 0.15 )))
   ((= D::UNT  "7") (progn (setq DSTY$ "ren_20"       ) (setq DSCL# 0.20 )))
   ((= D::UNT  "8") (progn (setq DSTY$ "ren_25"       ) (setq DSCL# 0.25 )))
   ((= D::UNT  "9") (progn (setq DSTY$ "ren_30"       ) (setq DSCL# 0.30 )))
   ((= D::UNT "10") (progn (setq DSTY$ "ren_40"       ) (setq DSCL# 0.40 )))
   ((= D::UNT "11") (progn (setq DSTY$ "ren_50"       ) (setq DSCL# 0.50 )))
   ((= D::UNT "12") (progn (setq DSTY$ "ren_75"       ) (setq DSCL# 0.75 )))
   ((= D::UNT "13") (progn (setq DSTY$ "ren_100"      ) (setq DSCL# 1.00 )))
   ((= D::UNT "14") (progn (setq DSTY$ "ren_150"      ) (setq DSCL# 1.50 )))
   ((= D::UNT "15") (progn (setq DSTY$ "ren_200"      ) (setq DSCL# 2.00 )))
  )
  (cond
   ((= D::UNT  "0") (progn (setq DSTY$ "ren.detay_1"  ) (setq DSCL# 0.01 )))
   ((= D::UNT  "1") (progn (setq DSTY$ "ren.detay_2"  ) (setq DSCL# 0.02 )))
   ((= D::UNT  "2") (progn (setq DSTY$ "ren.detay_4"  ) (setq DSCL# 0.04 )))
   ((= D::UNT  "3") (progn (setq DSTY$ "ren.detay_5"  ) (setq DSCL# 0.05 )))
   ((= D::UNT  "4") (progn (setq DSTY$ "ren.detay_8"  ) (setq DSCL# 0.08 )))
   ((= D::UNT  "5") (progn (setq DSTY$ "ren.detay_10" ) (setq DSCL# 0.10 )))
   ((= D::UNT  "6") (progn (setq DSTY$ "ren.detay_15" ) (setq DSCL# 0.15 )))
   ((= D::UNT  "7") (progn (setq DSTY$ "ren.detay_20" ) (setq DSCL# 0.20 )))
   ((= D::UNT  "8") (progn (setq DSTY$ "ren.detay_25" ) (setq DSCL# 0.25 )))
   ((= D::UNT  "9") (progn (setq DSTY$ "ren.detay_30" ) (setq DSCL# 0.30 )))
   ((= D::UNT "10") (progn (setq DSTY$ "ren.detay_40" ) (setq DSCL# 0.40 )))
   ((= D::UNT "11") (progn (setq DSTY$ "ren.detay_50" ) (setq DSCL# 0.50 )))
   ((= D::UNT "12") (progn (setq DSTY$ "ren.detay_75" ) (setq DSCL# 0.75 )))
   ((= D::UNT "13") (progn (setq DSTY$ "ren.detay_100") (setq DSCL# 1.00 )))
   ((= D::UNT "14") (progn (setq DSTY$ "ren.detay_150") (setq DSCL# 1.50 )))
   ((= D::UNT "15") (progn (setq DSTY$ "ren.detay_200") (setq DSCL# 2.00 )))
  )
 )
  (DSTYLE3_LAYER_CREATE "ren.dimension" 1 "Continuous" 18)
  (DSTYLE3_LAYER_CREATE "ren.arrow" 34 "Continuous" 18)
  (setq nokta (strcat "\U+002E"))
  (DSTYLE3_FONT_STYLE   "ren Gost.common" "GOST Common" 0 0.7 10 )
  (if (= dim_style_secim "1") (setq blk$  "OBLIQUE") (setq blk$ "" ))
  (princ "\n Blk : ") (princ blk$) (terpri)
  (if (= (tblsearch "dimstyle" DSTY$) nil)
    (progn
      (if (= dim_style_secim "1") (setvar "clayer" "ren.dimension") (setvar "clayer" "ren.arrow"))
      (entmake (list (cons 0  "DIMSTYLE") (cons 100 "AcDbSymbolTableRecord") (cons 100 "AcDbDimStyleTableRecord") (cons 2 DSTY$) (cons 70 0)))
      (command "-dimstyle"          "_restore" DSTY$  ) ; Set dimstyle current
 	  (if (= dim_style_secim "1") (setq DSCL#_2 100.0000)  (setq DSCL#_2 1.0000))
         ( SETVAR "DIMADEC"         0                 ) ;    Angular decimal places
         ( SETVAR "DIMALT"          0                 ) ;    Alternate units selected
         ( SETVAR "DIMALTD"         2                 ) ;    Alternate unit decimal places
         ( SETVAR "DIMALTF"         25.4000           ) ;    Alternate unit scale factor
         ( SETVAR "DIMALTRND"       0.0000            ) ;    Alternate units rounding value
         ( SETVAR "DIMALTTD"        2                 ) ;    Alternate tolerance decimal places
         ( SETVAR "DIMALTTZ"        0                 ) ;    Alternate tolerance zero suppression
         ( SETVAR "DIMALTU"         2                 ) ;    Alternate units
         ( SETVAR "DIMALTZ"         0                 ) ;    Alternate unit zero suppression
         ( SETVAR "DIMAPOST"        ""                ) ;    Prefix and suffix for alternate text
         ( SETVAR "DIMARCSYM"       0                 ) ;    Arc length symbol
         ( SETVAR "DIMASSOC"        1                 )
         ( SETVAR "DIMASZ"          (* DSCL# 200.0000)) ;    Arrow size
         ( SETVAR "DIMATFIT"        3                 ) ;    Arrow and text fit
         ( SETVAR "DIMAUNIT"        0                 ) ;    Angular unit format
         ( SETVAR "DIMBLK"          blk$              ) ;    Arrow block name
         ( SETVAR "DIMBLK1"         blk$              ) ;    First arrow block name
         ( SETVAR "DIMBLK2"         blk$              ) ;    Second arrow block name
         ( SETVAR "DIMCEN"          (* DSCL# 100.0000)) ;    Center mark size
         ( SETVAR "DIMCLRD"         256               ) ;    Dimension line and leader color
         ( SETVAR "DIMCLRE"         256               ) ;    Extension line color
         ( SETVAR "DIMCLRT"         3                 ) ;    Dimension text color
         ( SETVAR "DIMDEC"          0                 ) ;    Decimal places
         ( SETVAR "DIMDLE"          (* DSCL# 100.0000)) ;    Dimension line extension
         ( SETVAR "DIMDLI"          (* DSCL# 100.0000)) ;    Dimension line spacing
         ( SETVAR "DIMDSEP"         "."               ) ;    Decimal separator
         ( SETVAR "DIMEXE"          (* DSCL# 100.0000)) ;    Extension above dimension line
         ( SETVAR "DIMEXO"          (* DSCL# DSCL#_2) ) ;    Extension line origin offset
         ( SETVAR "DIMFRAC"         0                 ) ;    Fraction format
         ( SETVAR "DIMFXL"          1.0000            ) ;    Fixed Extension Line
         ( SETVAR "DIMGAP"          (* DSCL# 50.0000) ) ;    Gap from dimension line to text
         ( SETVAR "DIMJUST"         0                 ) ;    Justification of text on dimension line
         ( SETVAR "DIMLDRBLK"       ""                ) ;    Leader block name
         ( SETVAR "DIMLFAC"         1                 ) ;    Linear unit scale factor
         ( SETVAR "DIMLIM"          0                 ) ;    Generate dimension limits
         ( SETVAR "DIMLTEX1"        "BYBLOCK"         ) ;    Linetype extension line 1
         ( SETVAR "DIMLTEX2"        "BYBLOCK"         ) ;    Linetype extension line 2
         ( SETVAR "DIMLTYPE"        "BYBLOCK"         ) ;    Dimension linetype
         ( SETVAR "DIMLUNIT"        2                 ) ;    Linear unit format
         ( SETVAR "DIMLWD"          -2                ) ;    Dimension line and leader lineweight
         ( SETVAR "DIMLWE"          -2                ) ;    Extension line lineweight
         ( SETVAR "DIMPOST"         ""                ) ;    Prefix and suffix for dimension text
         ( SETVAR "DIMRND"          5.0000            ) ;    Rounding value
         ( SETVAR "DIMSAH"          0                 ) ;    Separate arrow blocks
         ( SETVAR "DIMSCALE"        1                 ) ;    Overall scale factor
         ( SETVAR "DIMSD1"          0                 ) ;    Suppress the first dimension line
         ( SETVAR "DIMSD2"          0                 ) ;    Suppress the second dimension line
         ( SETVAR "DIMSE1"          0                 ) ;    Suppress the first extension line
         ( SETVAR "DIMSE2"          0                 ) ;    Suppress the second extension line
         ( SETVAR "DIMSOXD"         0                 ) ;    Suppress outside dimension lines
         ( SETVAR "DIMTAD"          1                 ) ;    Place text above the dimension line
         ( SETVAR "DIMTDEC"         0                 ) ;    Tolerance decimal places
         ( SETVAR "DIMTFAC"         1.0000            ) ;    Tolerance text height scaling factor
         ( SETVAR "DIMTFILL"        0                 ) ;    Text background enabled
         ( SETVAR "DIMTIH"          0                 ) ;    Text inside extensions is horizontal
         ( SETVAR "DIMTIX"          1                 ) ;    Place text inside extensions
         ( SETVAR "DIMTM"           0.0000            ) ;    Minus tolerance
         ( SETVAR "DIMTMOVE"        1                 ) ;    Text movement
         ( SETVAR "DIMTOFL"         1                 ) ;    Force line inside extension lines
         ( SETVAR "DIMTOH"          0                 ) ;    Text outside horizontal
         ( SETVAR "DIMTOL"          0                 ) ;    Tolerance dimensioning
         ( SETVAR "DIMTOLJ"         1                 ) ;    Tolerance vertical justification
         ( SETVAR "DIMTP"           0.0000            ) ;    Plus tolerance
         ( SETVAR "DIMTSZ"          0.0000            ) ;    Tick size
         ( SETVAR "DIMTVP"          0.0000            ) ;    Text vertical position
         ( SETVAR "DIMTXSTY"        "ren Gost.common" ) ;    Text style
         ( SETVAR "DIMTXT"          (* DSCL# 300.0000)) ;    Text height
         ( SETVAR "DIMTXTDIRECTION" 0                 ) ;    Dimension text direction
         ( SETVAR "DIMTZIN"         0                 ) ;    Tolerance zero suppression
         ( SETVAR "DIMUPT"          0                 ) ;    User positioned text
         ( SETVAR "DIMZIN"          8                 ) ;    Zero suppression
         ( SETVAR "DIMUNIT"         2                 ) ;    Obsolete.
         ( SETVAR "DIMFIT"          4                 ) ;    Obsolete.
     (alert (strcat "\n"DSTY$" Ismindeki Olcu Sistemi Olusturuldu"))                  ; Display current Dimstyle
     (princ)
    (command "-dimstyle" "_save" DSTY$ "y" )
    )
        (progn
    (if (= dim_style_secim "1") (setvar "clayer" "ren.dimension") (setvar "clayer" "ren.arrow"))
    (command "-dimstyle" "_restore" DSTY$)                                  ; Set dimstyle current
 	(alert (strcat "\n"DSTY$" Ismindeki Olcu Sistemi Mevcut")))

  )))
  (princ)
)
