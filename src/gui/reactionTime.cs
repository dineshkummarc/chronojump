/*
 * This file is part of ChronoJump
 *
 * ChronoJump is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or   
 *    (at your option) any later version.
 *    
 * ChronoJump is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 *    GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * Copyright (C) 2004-2011   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using Gtk;
using Glade;
using System.Text; //StringBuilder
using System.Collections; //ArrayList

using System.Threading;
using Mono.Unix;

public partial class ChronoJumpWindow 
{
	[Widget] Gtk.Label label_extra_window_radio_reaction_time;
	[Widget] Gtk.RadioButton extra_window_radio_reaction_time;
	
	private void on_extra_window_reaction_times_test_changed(object o, EventArgs args)
	{
		if(extra_window_radio_reaction_time.Active)
			currentReactionTimeType = new ReactionTimeType("reactionTime");
		currentEventType = currentReactionTimeType;
	}
}

//--------------------------------------------------------
//---------------- EDIT REACTION TIME WIDGET -------------
//--------------------------------------------------------

public class EditReactionTimeWindow : EditEventWindow
{
	static EditReactionTimeWindow EditReactionTimeWindowBox;

	EditReactionTimeWindow (Gtk.Window parent) {
		Glade.XML gladeXML;
		gladeXML = Glade.XML.FromAssembly (Util.GetGladePath() + "chronojump.glade", "edit_event", null);
		gladeXML.Autoconnect(this);
		this.parent = parent;
		
		//put an icon to window
		UtilGtk.IconWindow(edit_event);
	
		eventBigTypeString = Catalog.GetString("reaction time");
	}

	static new public EditReactionTimeWindow Show (Gtk.Window parent, Event myEvent, int pDN)
	{
		if (EditReactionTimeWindowBox == null) {
			EditReactionTimeWindowBox = new EditReactionTimeWindow (parent);
		}

		EditReactionTimeWindowBox.pDN = pDN;
		
		EditReactionTimeWindowBox.initializeValues();

		EditReactionTimeWindowBox.fillDialog (myEvent);

		//reaction time has no types
		EditReactionTimeWindowBox.label_type_title.Hide();
		EditReactionTimeWindowBox.hbox_combo_eventType.Hide();

		EditReactionTimeWindowBox.edit_event.Show ();

		return EditReactionTimeWindowBox;
	}
	
	protected override void initializeValues () {
		typeOfTest = Constants.TestTypes.RT;
		headerShowDecimal = false;
		showType = false;
		showRunStart = false;
		showTv = false;
		showTc= false;
		showFall = false;
		showDistance = false;
		showTime = true;
		showSpeed = false;
		showWeight = false;
		showLimited = false;
		showMistakes = false;
	}

	protected override string [] findTypes(Event myEvent) {
		//reaction time has no types
		string [] myTypes = new String[0];
		return myTypes;
	}
	
	protected override void fillTime(Event myEvent) {
		ReactionTime myRT = (ReactionTime) myEvent;
		entryTime = myRT.Time.ToString();
		
		//show all the decimals for not triming there in edit window using
		//(and having different values in formulae like GetHeightInCm ...)
		//entry_time_value.Text = Util.TrimDecimals(entryTime, pDN);
		entry_time_value.Text = entryTime;
	}
	
	protected override void updateEvent(int eventID, int personID, string description) {
		SqliteReactionTime.Update(eventID, "", entryTime, personID, description); //2nd is type
	}

	protected override void on_button_cancel_clicked (object o, EventArgs args)
	{
		EditReactionTimeWindowBox.edit_event.Hide();
		EditReactionTimeWindowBox = null;
	}
	
	protected override void on_delete_event (object o, DeleteEventArgs args)
	{
		EditReactionTimeWindowBox.edit_event.Hide();
		EditReactionTimeWindowBox = null;
	}
	
	protected override void hideWindow() {
		EditReactionTimeWindowBox.edit_event.Hide();
		EditReactionTimeWindowBox = null;
	}

}
