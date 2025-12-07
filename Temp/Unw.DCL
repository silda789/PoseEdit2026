Standart_Ayarlar_dialog :  dialog { label = "The Standard Settings V1.1.0";
  : row {
   : column {
    : edit_box
    {
     label              = "Sheet scale 1 /... ";
     mnemonic           = "n";
     key                = "pafta_olcegi";
     alignment          = right;
     edit_width         = 20;
     edit_limit         = 50;
    }

   : radio_row
    {
       label = "Drawing unit..:";
     : radio_button { label = "Meter:";         key = "tik_m_key"; value = "1";  }
     : radio_button { label = "Centimeter:";    key = "tik_cm_key"; }
     : radio_button { label = "Millimeter:";    key = "tik_mm_key"; }
    }
   : radio_row
    {
       label = "Quantity table language..:";
     : radio_button { label = "Rus:";     key = "tik_rus_key"; value = "1";  }
     : radio_button { label = "Eng:";     key = "tik_eng_key"; }
     : radio_button { label = "Rus/Eng:"; key = "tik_r_e_key"; }
    }
   }}
                            : popup_list
                                   {
                                     label              = "Project name..:";
                                     key                = "hazir_yazi";
                                     alignment          = right;
                                     edit_width         = 30;
                                   }
   : row   {
: spacer { width = 0.1; }
                      : button
                              {
                                label                   = "M.Tablo Yerlesimi";
                                key                     = "M_T_Y";
                                height                  = 1;
                                width                   = 3;
                                is_default              = true;
                                allow_accept            = true;
                               }
: spacer { width = 0.1; }

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
: spacer { width = 0.1; }
                     }


   }
   
   nest1 : dialog {
        label = "Metraj Tablo Yerlesimi V1.1.0";
 : row {
 : boxed_radio_column
    {
     : radio_button { label = "Yerlesim-1:";     key = "Yerlesim_1"; }
     : radio_button { label = "Yerlesim-2:";     key = "Yerlesim_2"; }
     : radio_button { label = "Yerlesim-3:";     key = "Yerlesim_3"; }
    }


 : column
    {
      : image
     {
       key          = "yerlesim_image";
       height       = 10;
       aspect_ratio = 4;
       fixed_height = true;
       fixed_width  = true;
    }
      : image
     {
       key          = "yerlesim_image_2";
       height       = 10;
       aspect_ratio = 4;
     alignment          = right;
       fixed_height = true;
       fixed_width  = true;
    }
      : image
     {
       key          = "yerlesim_image_3";
       height       = 10;
       aspect_ratio = 4;
       fixed_height = true;
       fixed_width  = true;
    }
   }
  }

   	: row {

	: spacer { width = 1; }

	: button {
	label = "OK";
	key = "accept";
	width = 12;
	fixed_width = true;
	mnemonic = "O";
	is_default = true;
	}

	: button {
	label = "Cancel";
	key = "cancel";
	width = 12;
	fixed_width = true;
	mnemonic = "C";
	is_cancel = true;
  	}

	: spacer { width = 1;}

	}

  		 }  		
