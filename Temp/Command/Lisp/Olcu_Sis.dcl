main : dialog { label = "Olcu Sistemi V1.1";
       : row {
         : column {
           : boxed_column { label = "Dimension Units";

             : popup_list   { key = "DUNT";   value = "13"; width = 20.0; fixed_width = true; alignment = centered; }
             : radio_column {
             : radio_button { label = "Genel Olculendirme"; key = "DUNT_G"; value = "1"; }
             : radio_button { label = "Detay Olculendirme"; key = "DUNT_D"; value = "0"; }
                             }
             : spacer { height = 0; }
           }
           : boxed_column { label = "Control Buttons";
             : button { label = "&OK";   key = "accept"; width = 20; fixed_width = true; alignment = centered; is_default = true; }  
             : button { label = "&Exit"; key = "cancel"; width = 20; fixed_width = true; alignment = centered; is_cancel = true;  }
           }
         }
       }
     }
     


