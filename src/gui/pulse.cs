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
 * Xavier de Blas: 
 * http://www.xdeblas.com, http://www.deporteyciencia.com (parleblas)
 */

using System;
using Gtk;
using Glade;
using System.Text; //StringBuilder
using System.Collections; //ArrayList

using System.Threading;
using Mono.Unix;



//--------------------------------------------------------
//---------------- EDIT PULSE WIDGET ---------------------
//--------------------------------------------------------

public class EditPulseWindow : EditEventWindow
{
	static EditPulseWindow EditPulseWindowBox;

	EditPulseWindow (Gtk.Window parent) {
		Glade.XML gladeXML;
		gladeXML = Glade.XML.FromAssembly (Util.GetGladePath() + "chronojump.glade", "edit_event", null);
		gladeXML.Autoconnect(this);
		this.parent 	= parent;
		
		//put an icon to window
		UtilGtk.IconWindow(edit_event);
	
		eventBigTypeString = Catalog.GetString("pulse");
	}

	static new public EditPulseWindow Show (Gtk.Window parent, Event myEvent, int pDN)
	{
		if (EditPulseWindowBox == null) {
			EditPulseWindowBox = new EditPulseWindow (parent);
		}

		EditPulseWindowBox.pDN = pDN;
		
		EditPulseWindowBox.initializeValues();

		EditPulseWindowBox.fillDialog (myEvent);

		EditPulseWindowBox.edit_event.Show ();

		return EditPulseWindowBox;
	}
	
	protected override void initializeValues () {
		showTv = false;
		showTc= false;
		showFall = false;
		showDistance = false;
		showTime = false;
		showSpeed = false;
		showWeight = false;
		showLimited = false;
	}

	protected override string [] findTypes(Event myEvent) {
		string [] myTypes = SqlitePulseType.SelectPulseTypes("", true); //don't show allEventName row, only select name

		//on pulses can not change type
		combo_eventType.Sensitive = false;

		return myTypes;
	}
	
	/*
	protected override void fillTime(Event myEvent) {
		Pulse myPulse = (Pulse) myEvent;
		entryTime = myPulse.Time.ToString();
		entry_time_value.Text = Util.TrimDecimals(entryTime, pDN);
	}
	*/
	
	protected override void updateEvent(int eventID, int personID, string description) {
		SqlitePulse.Update(eventID, personID, description);
	}

	protected override void on_button_cancel_clicked (object o, EventArgs args)
	{
		EditPulseWindowBox.edit_event.Hide();
		EditPulseWindowBox = null;
	}
	
	protected override void on_delete_event (object o, DeleteEventArgs args)
	{
		EditPulseWindowBox.edit_event.Hide();
		EditPulseWindowBox = null;
	}
	
	protected override void hideWindow() {
		EditPulseWindowBox.edit_event.Hide();
		EditPulseWindowBox = null;
	}

}


//--------------------------------------------------------
//---------------- pulse extra WIDGET --------------------
//--------------------------------------------------------

public class PulseExtraWindow 
{
	[Widget] Gtk.Window pulse_extra;
	[Widget] Gtk.SpinButton spinbutton_pulse_step;
	[Widget] Gtk.SpinButton spinbutton_ppm;
	[Widget] Gtk.SpinButton spinbutton_total_pulses;
	[Widget] Gtk.CheckButton checkbutton_unlimited;
	[Widget] Gtk.HBox hbox_total_pulses;
	[Widget] Gtk.Button button_accept;

	static double pulseStep = 1.000;
	static bool unlimited = true;
	static int totalPulses = 10;
	
	static PulseExtraWindow PulseExtraWindowBox;
	Gtk.Window parent;

	PulseExtraWindow (Gtk.Window parent) {
		Glade.XML gladeXML;
		gladeXML = Glade.XML.FromAssembly (Util.GetGladePath() + "chronojump.glade", "pulse_extra", null);
		gladeXML.Autoconnect(this);
		this.parent = parent;
		
		//put an icon to window
		UtilGtk.IconWindow(pulse_extra);
	}
	
	static public PulseExtraWindow Show (Gtk.Window parent, PulseType myPulseType) 
	{
		if (PulseExtraWindowBox == null) {
			PulseExtraWindowBox = new PulseExtraWindow (parent);
		}
		
		//put default values or values from previous pulse
		PulseExtraWindowBox.spinbutton_pulse_step.Value = pulseStep;
		if(totalPulses == -1)
			totalPulses = 10;
		
		PulseExtraWindowBox.spinbutton_total_pulses.Value = totalPulses;
		
		if(unlimited) 
			PulseExtraWindowBox.checkbutton_unlimited.Active = true;
		
		PulseExtraWindowBox.pulse_extra.Show ();

		return PulseExtraWindowBox;
	}
	
	
	void on_button_cancel_clicked (object o, EventArgs args)
	{
		PulseExtraWindowBox.pulse_extra.Hide();
		PulseExtraWindowBox = null;
	}
	
	void on_delete_event (object o, DeleteEventArgs args)
	{
		PulseExtraWindowBox.pulse_extra.Hide();
		PulseExtraWindowBox = null;
	}
	
	void on_button_accept_clicked (object o, EventArgs args)
	{
		pulseStep = (double) PulseExtraWindowBox.spinbutton_pulse_step.Value;
		Log.WriteLine(string.Format("pulsestep: {0}", pulseStep));
		if(checkbutton_unlimited.Active) {
			totalPulses = -1;
			unlimited = true;
		}
		else {
			totalPulses = (int) PulseExtraWindowBox.spinbutton_total_pulses.Value;
			unlimited = false;
		}
		
		PulseExtraWindowBox.pulse_extra.Hide();
		PulseExtraWindowBox = null;
	}

	
	void on_checkbutton_unlimited_clicked (object o, EventArgs args)
	{
		if(checkbutton_unlimited.Active) {
			hbox_total_pulses.Hide();
		} else {
			hbox_total_pulses.Show();
		}
	}

	void on_spinbutton_pulse_step_changed (object o, EventArgs args)
	{
		if((double) PulseExtraWindowBox.spinbutton_pulse_step.Value == 0) 
			PulseExtraWindowBox.spinbutton_ppm.Value = 0;
		else 
			PulseExtraWindowBox.spinbutton_ppm.Value = 60 / 
				(double) PulseExtraWindowBox.spinbutton_pulse_step.Value;
	}

	void on_spinbutton_ppm_changed (object o, EventArgs args)
	{
		if((int) PulseExtraWindowBox.spinbutton_ppm.Value == 0)
			PulseExtraWindowBox.spinbutton_pulse_step.Value = 0;
		else
			PulseExtraWindowBox.spinbutton_pulse_step.Value = 60 / 
				(double) PulseExtraWindowBox.spinbutton_ppm.Value;
	}


		
	public Button Button_accept 
	{
		set { button_accept = value;	}
		get { return button_accept;	}
	}

	public double PulseStep
	{
		get { return pulseStep;	}
	}
	
	public int TotalPulses
	{
		get { return totalPulses;	}
	}
	
}

//--------------------------------------------------------
//---------------- Repair pulse WIDGET -------------------
//--------------------------------------------------------

public class RepairPulseWindow 
{
	[Widget] Gtk.Window repair_sub_event;
	[Widget] Gtk.Label label_header;
	[Widget] Gtk.Label label_totaltime_value;
	[Widget] Gtk.TreeView treeview_subevents;
	private TreeStore store;
	[Widget] Gtk.Button button_accept;
	[Widget] Gtk.Button button_add_before;
	[Widget] Gtk.Button button_add_after;
	[Widget] Gtk.Button button_delete;
	[Widget] Gtk.TextView textview1;

	static RepairPulseWindow RepairPulseWindowBox;
	Gtk.Window parent;

	PulseType pulseType;
	Pulse myPulse; //used on button_accept
	

	RepairPulseWindow (Gtk.Window parent, Pulse myPulse, int pDN) {
		Glade.XML gladeXML;
		gladeXML = Glade.XML.FromAssembly (Util.GetGladePath() + "chronojump.glade", "repair_sub_event", null);
		gladeXML.Autoconnect(this);
		
		//put an icon to window
		UtilGtk.IconWindow(repair_sub_event);

		this.parent = parent;
		this.myPulse = myPulse;
	
		repair_sub_event.Title = Catalog.GetString("Repair pulse");
		
		System.Globalization.NumberFormatInfo localeInfo = new System.Globalization.NumberFormatInfo();
		localeInfo = System.Globalization.NumberFormatInfo.CurrentInfo;
		label_header.Text = string.Format(Catalog.GetString("Use this window to repair this test.\nDouble clic any cell to edit it (decimal separator: '{0}')"), localeInfo.NumberDecimalSeparator);
	
		
		pulseType = SqlitePulseType.SelectAndReturnPulseType(myPulse.Type);
		
		TextBuffer tb = new TextBuffer (new TextTagTable());
		tb.Text = createTextForTextView(pulseType);
		textview1.Buffer = tb;
		
		createTreeView(treeview_subevents);
		//count, time
		store = new TreeStore(typeof (string), typeof (string));
		treeview_subevents.Model = store;
		fillTreeView (treeview_subevents, store, myPulse, pDN);
	
		button_add_before.Sensitive = false;
		button_add_after.Sensitive = false;
		button_delete.Sensitive = false;
		
		label_totaltime_value.Text = getTotalTime().ToString() + " " + Catalog.GetString("seconds");
		
		treeview_subevents.Selection.Changed += onSelectionEntry;
	}
	
	static public RepairPulseWindow Show (Gtk.Window parent, Pulse myPulse, int pDN)
	{
		if (RepairPulseWindowBox == null) {
			RepairPulseWindowBox = new RepairPulseWindow (parent, myPulse, pDN);
		}
		
		RepairPulseWindowBox.repair_sub_event.Show ();

		return RepairPulseWindowBox;
	}
	
	private string createTextForTextView (PulseType myPulseType) {
		string pulseTypeString = string.Format(Catalog.GetString(
					"PulseType: {0}."), myPulseType.Name);

		string fixedString = "";


		/* 
		 * currently all pulseTypes are non fixed, and it's not possible to create more types (by user), then there are no limitations
		 */

/*
		if(myPulseType.FixedPulse > 0) {
			if(myRunType.TracksLimited) {
				//if it's a run type runsLimited with a fixed value, then don't allow the creation of more runs
				fixedString = string.Format(Catalog.GetString("\nThis run type is fixed to {0} runs, you cannot add more."), myRunType.FixedValue);
			} else {
				//if it's a run type timeLimited with a fixed value, then complain when the total time is higher
				fixedString = string.Format(Catalog.GetString("\nThis run type is fixed to {0} seconds, totaltime cannot be greater."), myRunType.FixedValue);
			}
		}
*/
		return pulseTypeString + fixedString;
	}

	
	private void createTreeView (Gtk.TreeView myTreeView) {
		myTreeView.HeadersVisible=true;
		int count = 0;

		myTreeView.AppendColumn ( Catalog.GetString ("Count"), new CellRendererText(), "text", count++);
		//myTreeView.AppendColumn ( Catalog.GetString ("Time"), new CellRendererText(), "text", count++);

		Gtk.TreeViewColumn timeColumn = new Gtk.TreeViewColumn ();
		timeColumn.Title = Catalog.GetString("Time");
		Gtk.CellRendererText timeCell = new Gtk.CellRendererText ();
		timeCell.Editable = true;
		timeCell.Edited += timeCellEdited;
		timeColumn.PackStart (timeCell, true);
		timeColumn.AddAttribute(timeCell, "text", count ++);
		myTreeView.AppendColumn ( timeColumn );
	}
	
	private void timeCellEdited (object o, Gtk.EditedArgs args)
	{
		Gtk.TreeIter iter;
		store.GetIter (out iter, new Gtk.TreePath (args.Path));
		if(Util.IsNumber(args.NewText)) {
			/* 
			 * currently all pulseTypes are non fixed, and it's not possible to create more types (by user), then there are no limitations
			 */
			
			/*
			//if it's limited by fixed value of seconds
			//and new seconds are bigger than allowed, return
			if(runType.FixedValue > 0 && ! runType.TracksLimited &&
					getTotalTime() //current total time in treeview
					- Convert.ToDouble((string) treeview_subevents.Model.GetValue(iter,1)) //-old cell
					+ Convert.ToDouble(args.NewText) //+new cell
					> runType.FixedValue) {	//bigger than allowed
				return;
			} else {
			*/
				store.SetValue(iter, 1, args.NewText);

				//update the totaltime label
				label_totaltime_value.Text = getTotalTime().ToString() + " " + Catalog.GetString("seconds");
			/*
			}
			*/
		}
		
		//if is not number or if it was -1, the old data will remain
	}

	private double getTotalTime() {
		TreeIter myIter;
		double totalTime = 0;
		bool iterOk = store.GetIterFirst (out myIter);
		if(iterOk) {
			do {
				double myTime = Convert.ToDouble((string) treeview_subevents.Model.GetValue(myIter, 1));
				totalTime += myTime;
			} while (store.IterNext (ref myIter));
		}
		return totalTime;
	}
	
	private void fillTreeView (Gtk.TreeView time, TreeStore store, Pulse myPulse, int pDN)
	{
		if(myPulse.TimesString.Length > 0) {
			string [] timeArray = myPulse.TimesString.Split(new char[] {'='});

			int count = 0;
			foreach (string myTime in timeArray) {
				store.AppendValues ( (count+1).ToString(), Util.TrimDecimals(myTime, pDN) );
				count ++;
			}
		}
	}

	void onSelectionEntry (object o, EventArgs args) {
		TreeModel model;
		TreeIter iter;
		
		if (((TreeSelection)o).GetSelected(out model, out iter)) {
			button_add_before.Sensitive = true;
			button_add_after.Sensitive = true;
			button_delete.Sensitive = true;

			/* 
			 * currently all pulseTypes are non fixed, and it's not possible to create more types (by user), then there are no limitations
			 */
			/*
			//don't allow to add a row before or after 
			//if the runtype is fixed to n runs and we reached n
			if(runType.FixedValue > 0 && runType.TracksLimited) {
				int lastRow = 0;
				do {
					lastRow = Convert.ToInt32 ((string) model.GetValue (iter, 0));
				} while (store.IterNext (ref iter));

				//don't allow if max rows reached
				if(lastRow == runType.FixedValue) {
					button_add_before.Sensitive = false;
					button_add_after.Sensitive = false;
				}
			}
			*/
		}
	}

	void on_button_add_before_clicked (object o, EventArgs args) {
		TreeModel model; 
		TreeIter iter; 
		if (treeview_subevents.Selection.GetSelected (out model, out iter)) {
			int position = Convert.ToInt32( (string) model.GetValue (iter, 0) ) -1; //count starts at '0'
			iter = store.InsertNode(position);
			store.SetValue(iter, 1, "0");
			putRowNumbers(store);
		}
	}
	
	void on_button_add_after_clicked (object o, EventArgs args) {
		TreeModel model; 
		TreeIter iter; 
		if (treeview_subevents.Selection.GetSelected (out model, out iter)) {
			int position = Convert.ToInt32( (string) model.GetValue (iter, 0) ); //count starts at '0'
			iter = store.InsertNode(position);
			store.SetValue(iter, 1, "0");
			putRowNumbers(store);
		}
	}
	
	private void putRowNumbers(TreeStore myStore) {
		TreeIter myIter;
		bool iterOk = myStore.GetIterFirst (out myIter);
		if(iterOk) {
			int count = 1;
			do {
				store.SetValue(myIter, 0, (count++).ToString());
			} while (myStore.IterNext (ref myIter));
		}
	}
		
	void on_button_delete_clicked (object o, EventArgs args) {
		TreeModel model; 
		TreeIter iter; 
		if (treeview_subevents.Selection.GetSelected (out model, out iter)) {
			store.Remove(ref iter);
			putRowNumbers(store);
		
			label_totaltime_value.Text = getTotalTime().ToString() + " " + Catalog.GetString("seconds");

			button_add_before.Sensitive = false;
			button_add_after.Sensitive = false;
			button_delete.Sensitive = false;
		}
	}
	
	void on_button_accept_clicked (object o, EventArgs args)
	{
		//foreach all lines... extrac intervalTimesString
		TreeIter myIter;
		string timeString = "";
		
		bool iterOk = store.GetIterFirst (out myIter);
		if(iterOk) {
			string equal= ""; //first iteration should not appear '='
			do {
				timeString = timeString + equal + (string) treeview_subevents.Model.GetValue (myIter, 1);
				equal = "=";
			} while (store.IterNext (ref myIter));
		}
			
		//calculate other variables needed for pulse creation
		
		myPulse.TotalPulsesNum = Util.GetNumberOfJumps(timeString, false); //don't need a GetNumberOfRuns, this works
		myPulse.TimesString = timeString;

		//save it deleting the old first for having the same uniqueID
		SqlitePulse.Delete(myPulse.UniqueID.ToString());

		myPulse.InsertAtDB(false, Constants.PulseTable);
		/*
		SqlitePulse.Insert(myPulse.UniqueID.ToString(), 
				myPulse.PersonID, myPulse.SessionID, 
				myPulse.Type, myPulse.FixedPulse, totalPulsesNum, 
				timeString, myPulse.Description
				);
				*/

		//close the window
		RepairPulseWindowBox.repair_sub_event.Hide();
		RepairPulseWindowBox = null;
	}

	void on_button_cancel_clicked (object o, EventArgs args)
	{
		RepairPulseWindowBox.repair_sub_event.Hide();
		RepairPulseWindowBox = null;
	}
	
	void on_delete_event (object o, DeleteEventArgs args)
	{
		RepairPulseWindowBox.repair_sub_event.Hide();
		RepairPulseWindowBox = null;
	}
	
	public Button Button_accept 
	{
		set { button_accept = value;	}
		get { return button_accept;	}
	}

}

