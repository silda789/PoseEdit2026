Standart_Ayarlar_dialog :  dialog { label = "Standart Ayarlari V1.0.0";
  : row {
   : column {
    : edit_box
    {
     label              = "Cizim Birimi m->1 cm->100 mm->1000";
     mnemonic           = "b";
     key                = "cizim_birimi";
     alignment          = right;
     edit_width         = 20;
     edit_limit         = 50;
    }
    : edit_box
    {
     label              = "Pafta Olcegi 1/:";
     mnemonic           = "n";
     key                = "pafta_olcegi";
     alignment          = right;
     edit_width         = 20;
     edit_limit         = 50;
    }

   }}
                         : popup_list
                                   {
                                     label              = "Hazir yazi";
                                     key                = "hazir_yazi";
                                     alignment          = right;
                                     edit_width         = 30;
                                   }
   : row   {
                      : button
                              {
                                label                   = "Ayarla";
                                key                     = "ayarla";
                                height                  = 1;
                                width                   = 3;
                                is_default              = true;
                                allow_accept            = true;
                               }
                      : button
                              {
                                label                   = "Cik";
                                key                     = "ad_cik";
                                height                  = 1;
                                width                   = 3;
                                is_cancel               = true;
                               }
                     }


   }
