ad_dialog : dialog
          {
            label = "Ardisik Yazi V.2.1b (c)";
            : column
                    {
                      alignment     = left;
                     : edit_box    {
                                     label              = "Baslama sayisi:";
                                     mnemonic           = "b";
                                     key                = "ad_basla_sayi";
                                     alignment          = right;
                                     edit_width         = 30;
                                     edit_limit         = 30;
                                    }
                     : edit_box    {
                                     label              = "On ek:";
                                     mnemonic           = "n";
                                     key                = "ad_yazi_on_ek";
                                     alignment          = right;
                                     edit_width         = 30;
                                     edit_limit         = 30;
                                    }
                      : edit_box    {
                                     label              = "Arka ek:";
                                     mnemonic           = "a";
                                     key                = "ad_yazi_arka_ek";
                                     alignment          = right;
                                     edit_width         = 30;
                                     edit_limit         = 30;
                                    }
                      : edit_box    {
                                     label              = "Sayi grubu adeti:";
                                     mnemonic           = "s";
                                     key                = "ad_yazi_hane_adet";
                                     alignment          = right;
                                     edit_width         = 5;
                                     edit_limit         = 5;
                                    }
                      : boxed_column
                              {
                      : toggle
                                   {
                                     label              = "Sayi grubunu iptal et.....";
                                     key                = "ad_sayi_grubu_iptal";
                                    }
                      : popup_list
                                   {
                                     label              = "Hazir yazi";
                                     key                = "ad_hazir_yazi";
                                     alignment          = right;
                                     edit_width         = 30;
                                   }
                              }
                      : boxed_column
                              {
                            : row {
                               : toggle
                                        {
                                          label         = "Yeni yazi yaz";
                                          key           = "ad_yeni_yazi_yaz";
                                         }
                               : edit_box {
                                          label              = "Yuk:";
                                           mnemonic           = "s";
                                           key                = "ad_yeni_yazi_yuk";
                                           alignment          = right;
                                           edit_width         = 5;
                                           edit_limit         = 5;
                                          }

                               : edit_box {
                                          label              = "Aci:";
                                           mnemonic           = "s";
                                           key                = "ad_yeni_yazi_aci";
                                           alignment          = right;
                                           edit_width         = 5;
                                           edit_limit         = 5;
                                          }
                                   }
                               : toggle
                                        {
                                          label         = "Mevcut yaziyi bununla degistir.";
                                          key           = "ad_ekle_yazi_degis";
                                         }
                               : toggle
                                        {
                                          label         = "Mevcud yazinin onune ekle.";
                                          key           = "ad_ekle_yazi_on_ek";
                                         }
                               : toggle
                                        {
                                          label         = "Mevcut yazinin arkasina ekle";
                                          key           = "ad_ekle_yazi_arka_ek";
                                         }
                               }
                     }
            : boxed_column
                    {
                      : toggle
                              {
                               label                    = "Layeri degistirme";
                               key                      = "ad_layer_mevcut";
                               value                    = "0";
                               }
                      : row
                              {
                               : toggle {
                                          label         = "Layer sec";
                                          key           = "ad_layer_degis";
                                          value         = "0";
                                          alignment     = left;
                                         }
                               : popup_list
                                        {
                                          label         = " ";
                                          key           = "ad_yeni_layer_adi";
                                          alignment     = right;
                                          edit_width    = 30;
                                         }
                                }
                     }

            : row   {
                      : button
                              {
                                label                   = "ciz";
                                key                     = "ad_ciz";
                                height                  = 1;
                                width                   = 3;
                                is_default              = true;
                                allow_accept            = true;
                               }
                      : button
                              {
                                label                   = "cik";
                                key                     = "ad_cik";
                                height                  = 1;
                                width                   = 3;
                                is_cancel               = true;
                               }
                     }
           }
