poz_edit : dialog
{
 label         = "Pose Edit (c) RENAISSANCE CONSTRUCTION, Designed by Osman YILMAZ, Istanbul - 2013";
: row
{
 : column
  {
   : row
    {
   : column
    {
     : edit_box
      {
       label         = "Pose:";
       mnemonic      = "P";
       key           = "poz_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
      }
     : edit_box
      {
       label         = "Group Mult.:";
       key           = "gc_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
      }
     : edit_box
      {
       label         = "Item Mult.:";
       mnemonic      = "A";
       key           = "adet_carp_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
      }
     : row
      {
     : edit_box
      {
       label         = "Item     :";
       mnemonic      = "D";
       key           = "adet_key";
       alignment     = right;
       edit_width    = 8;
       edit_limit    = 15;
      }
     : button
      {
       label         = ">";
       key           = "adet_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 1;
       fixed_width   = true;
      }
     : button
      {
       label         = "L";
       key           = "adet_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 1;
       fixed_width   = true;
      }
      }
     : edit_box
      {
       label         = "Diameter :";
       mnemonic      = "Ç";
       key           = "cap_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
      }
     : edit_box
      {
       label         = "R. Space:";
       mnemonic      = "r";
       key           = "aralik_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
      }
     : edit_box
      {
       label         = "Length :";
       mnemonic      = "r";
       key           = "boy_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
      }
     : edit_box
      {
       label         = "Type :";
       mnemonic      = "r";
       key           = "tip_key";
       alignment     = right;
       edit_width    = 15;
       edit_limit    = 15;
       is_enabled    = false;
      }
      }
   : column
    {
   : row
    {
     : button
      {
       label         = "(-)";
       key           = "p_azalt_key";
       height        = 1;
       width         = 3;
      }
     : button
      {
       label         = "(+)";
       key           = "p_arttir_key";
       height        = 1;
       width         = 3;
      }
    }
   : row
    {
     : button
      {
       label         = "(-)";
       key           = "grup_c_azalt_key";
       height        = 1;
       width         = 3;
      }
     : button
      {
       label         = "(+)";
       key           = "grup_c_arttir_key";
       height        = 1;
       width         = 3;
      }
    }
   : row
    {
     : button
      {
       label         = "(-)";
       key           = "adet_c_azalt_key";
       height        = 1;
       width         = 3;
      }
     : button
      {
       label         = "(+)";
       key           = "adet_c_arttir_key";
       height        = 1;
       width         = 3;
      }
    }
   : row
    {
     : button
      {
       label         = "(-)";
       key           = "adet_azalt_key";
       height        = 1;
       width         = 3;
      }
     : button
      {
       label         = "(+)";
       key           = "adet_arttir_key";
       height        = 1;
       width         = 3;
      }
    }
   : row
    {
     : button
      {
       label         = "(-)";
       key           = "cap_azalt_key";
       height        = 1;
       width         = 3;
      }
     : button
      {
       label         = "(+)";
       key           = "cap_arttir_key";
       height        = 1;
       width         = 3;
      }
    }
   : row
    {
     : button
      {
       label         = "(-)";
       key           = "aralik_azalt_key";
       height        = 1;
       width         = 3;
      }
     : button
      {
       label         = "(+)";
       key           = "aralik_arttir_key";
       height        = 1;
       width         = 3;
      }
    }
   : row
    {
     : button
      {
       label         = "Calculation";
       key           = "boy_hesapla_key";
       height        = 1;
       width         = 3;
      }
    }
     : popup_list
      {
       key           = "tip_liste_key";
       alignment     = right;
       edit_width    = 12;
       fixed_width   = true;
      }
    }
   : column
    {
     : edit_box
      {
       label         = "A:";
       key           = "A_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : edit_box
      {
       label         = "B:";
       key           = "B_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : edit_box
      {
       label         = "C:";
       key           = "C_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : edit_box
      {
       label         = "D:";
       key           = "D_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : edit_box
      {
       label         = "E:";
       key           = "E_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : edit_box
      {
       label         = "F:";
       key           = "F_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : edit_box
      {
       label         = "R:";
       key           = "R_key";
       alignment     = right;
       edit_width    = 12;
       edit_limit    = 12;
      }
     : button
      {
       label         = "Update All";
       key           = "tum_poz_gucelle";
       height        = 1;
       width         = 6;
      }
    }
       : column
    {
     : button
      {
       label         = "->";
       key           = "a_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "->";
       key           = "b_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "->";
       key           = "c_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "->";
       key           = "d_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "->";
       key           = "e_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "->";
       key           = "f_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "->";
       key           = "r_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Read";
       key           = "tddk_al_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
    }
       : column
    {
     : button
      {
       label         = "Link";
       key           = "a_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Link";
       key           = "b_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Link";
       key           = "c_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Link";
       key           = "d_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Link";
       key           = "e_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Link";
       key           = "f_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Link";
       key           = "r_bag_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Determination";
       key           = "donati_tani_key";
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
    }
    }
   : row
    {
     : button
      {
       label         = "Update Pose";
       key           = "poz_edit_ciz";
       is_default    = true;
       allow_accept  = true;
       height        = 1;
       width         = 6;
      }
     : button
      {
       label         = "Discard";
       key           = "poz_edit_cik";
       height        = 1;
       width         = 6;
       is_cancel     = true;
      }
     : edit_box
      {
       label = "Tik:";
       key = "tik_key";
       alignment = right;
       edit_width = 5;
       edit_limit = 5;
      }
     : radio_row
      {
       : radio_button { label = "0:"; key = "tik_0_key"; }
       : radio_button { label = "1:"; key = "tik_1_key"; value = "1"; }
      }
  }
 }
   : column
    {
     : image
     {
       key          = "poz_image_key";
       color        = dialog_background;
       height       = 10;
       aspect_ratio = 2.5;
       fixed_height = true;
       fixed_width  = true;
    }
   : column
    {
     : edit_box
      {
       label         = "Pose Image:" ;
       key           = "poz_toplam_goruntu" ;
       alignment     = Right;
       edit_width    = 35;
       edit_limit    = 50;
      }
   : column
    {
     : edit_box
      {
       label         = "Current Note:" ;
       mnemonic      = "N";
       key           = "not_key";
       alignment     = Right;
       edit_width    = 35;
       edit_limit    = 150;
      }
     : popup_list
      {
       label         = "Note List:";
       key           = "not_liste_key";
       alignment     = Right;
       edit_width    = 35;
      }
    }
   : column
    {
   : row
    {
     : edit_box
      {
       label         = "Material:";
       mnemonic      = "M";
       key           = "malzeme_key";
       alignment     = Right;
       edit_width    = 14;
       edit_limit    = 150;
       is_enabled    = false;
      }
     : popup_list
      {
       key           = "malzeme_liste_key";
       alignment     = Right;
       edit_width    = 18;
       fixed_width   = true;
      }
    }
      spacer;
    }
    }
}
}
}



