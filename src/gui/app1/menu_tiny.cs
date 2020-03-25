/*
 * This file is part of ChronoJump
 *
 * Chronojump is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or   
 *    (at your option) any later version.
 *    
 * Chronojump is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 *    GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * Copyright (C) 2018   Xavier de Blas <xaviblas@gmail.com> 
 */

//this file has methods of ChronoJumpWindow related to manage menu_tiny

using System;
using Gtk;
using Glade;

public partial class ChronoJumpWindow
{
	[Widget] Gtk.Arrow arrow_menu_show_session_up1;
	[Widget] Gtk.Arrow arrow_menu_show_session_down1;
	[Widget] Gtk.Arrow arrow_menu_show_help_up1;
	[Widget] Gtk.Arrow arrow_menu_show_help_down1;
	[Widget] Gtk.Button button_show_modes1;
	[Widget] Gtk.Viewport viewport_menu_tiny;
	[Widget] Gtk.EventBox eventbox_button_show_modes1;
	[Widget] Gtk.EventBox eventbox_check_menu_session1;
	[Widget] Gtk.EventBox eventbox_button_menu_preferences1;
	[Widget] Gtk.EventBox eventbox_check_menu_help1;
	[Widget] Gtk.EventBox eventbox_button_menu_exit1;
	[Widget] Gtk.CheckButton check_menu_session1;
	[Widget] Gtk.CheckButton check_menu_help1;
	[Widget] Gtk.Alignment alignment_menu_session_options1;
	[Widget] Gtk.Alignment alignment_menu_person_options1;
	[Widget] Gtk.Alignment alignment_menu_help_options1;

	[Widget] Gtk.Image image_menu_folders1;
	[Widget] Gtk.Image image_session_new1;
	[Widget] Gtk.Image image_session_load1;
	[Widget] Gtk.Image image_session_edit1;
	[Widget] Gtk.Image image_session_delete1;
	[Widget] Gtk.Image image_button_show_modes1;
	[Widget] Gtk.Image image_menu_preferences1;
	[Widget] Gtk.Image image_menu_help1;
	[Widget] Gtk.Image image_menu_help_documents1;
	[Widget] Gtk.Image image_menu_help_accelerators1;
	[Widget] Gtk.Image image_menu_help_about1;
	[Widget] Gtk.Image image_menu_quit1;

	private void menuTinyInitialize ()
	{
		menuTinySetColors();

		//LogB.Information("hpaned MinPosition: " + hpaned_contacts_main.MinPosition.ToString());

		//unselect menu_help if selected
		if(check_menu_help1.Active)
			check_menu_help1.Active = false;
		alignment_menu_help_options1.Visible = false;

		/*
		//this is done to ensure hidden buttons will be shown (because also submenu items seems to have Allocation=1)
		//if we need it, pass also the other buttons but without the +16
		List <Gtk.Button> l = new List<Gtk.Button>();
		l.Add(button_menu_session_new);
		l.Add(button_menu_session_load);
		l.Add(button_menu_session_edit);
		l.Add(button_menu_session_delete);
		l.Add(button_menu_help_documents);
		l.Add(button_menu_help_accelerators);
		l.Add(button_menu_help_about);
		int maxWidth = getMenuButtonsMaxWidth(l) + 16 + 4 + 6; //16, 4, 6 are alignments spaces.

		//except on ICONS, consider also viewport_persons
		if(preferences.menuType != Preferences.MenuTypes.ICONS)
		{
			if(viewport_persons.SizeRequest().Width +4 +6 > maxWidth)
				maxWidth = viewport_persons.SizeRequest().Width +4 + 6; //+4 due to alignment_person, +6 to alignment_viewport_menu_top
			//if(frame_persons.SizeRequest().Width > maxWidth)
			//	maxWidth = frame_persons.SizeRequest().Width;
		}

		viewport_menu_top.SetSizeRequest(maxWidth, -1); //-1 is height
		*/
	}

	private void menuTinySetColors ()
	{
		Gdk.Color color = UtilGtk.ColorParse(preferences.colorBackgroundString);

		UtilGtk.ViewportColor(viewport_hpaned_contacts_main, color);
		UtilGtk.ViewportColor(viewport_menu_tiny, color);

		UtilGtk.EventBoxColorBackgroundActive (eventbox_button_show_modes1, UtilGtk.YELLOW);
		UtilGtk.EventBoxColorBackgroundActive (eventbox_check_menu_session1, UtilGtk.YELLOW);
		UtilGtk.EventBoxColorBackgroundActive (eventbox_button_menu_preferences1, UtilGtk.YELLOW);
		UtilGtk.EventBoxColorBackgroundActive (eventbox_check_menu_help1, UtilGtk.YELLOW);
		UtilGtk.EventBoxColorBackgroundActive (eventbox_button_menu_exit1, UtilGtk.YELLOW);
	}

	private void on_check_menu_session1_clicked (object o, EventArgs args)
	{
		menuShowVerticalArrow (check_menu_session1.Active, arrow_menu_show_session_up1, arrow_menu_show_session_down1);
		if(check_menu_session1.Active)
		{
			check_menu_help1.Active = false;
			alignment_menu_session_options1.Visible = true;

			alignment_menu_session_options1.Show();
		} else
			alignment_menu_session_options1.Visible = false;
	}

	private void on_check_menu_help1_clicked (object o, EventArgs args)
	{
		menuShowVerticalArrow (check_menu_help1.Active, arrow_menu_show_help_up1, arrow_menu_show_help_down1);
		if(check_menu_help1.Active)
		{
			check_menu_session1.Active = false;
			alignment_menu_help_options1.Visible = true;
		} else
			alignment_menu_help_options1.Visible = false;
	}

	private void on_button_show_modes1_clicked (object o, EventArgs args)
	{
		show_start_page();
		button_show_modes1.Sensitive = false;
	}

}
