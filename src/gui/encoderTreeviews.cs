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
 * Copyright (C) 2004-2014   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.IO; 
using System.IO.Ports;
using Gtk;
using Gdk;
using Glade;
using System.Collections;
using Mono.Unix;


public partial class ChronoJumpWindow 
{

	/* TreeView stuff */	

	//returns curves num
	//capture has single and multiple selection in order to save curves... Analyze only shows data.
	private int createTreeViewEncoderCapture(string contents) {
		bool showStartAndDuration = encoderCaptureOptionsWin.check_show_start_and_duration.Active;

		string [] columnsString = {
			Catalog.GetString("n") + "\n",
			Catalog.GetString("Start") + "\n (s)",
			Catalog.GetString("Duration") + "\n (s)",
			Catalog.GetString("Distance") + "\n (cm)",
			"v" + "\n (m/s)",
			"vmax" + "\n (m/s)",
			"t->vmax" + "\n (s)",
			"p" + "\n (W)",
			"pmax" + "\n (W)",
			"t->pmax" + "\n (s)",
			"pmax/t->pmax" + "\n (W/s)"
		};

		encoderCaptureCurves = new ArrayList ();

		string line;
		int curvesCount = 0;
		using (StringReader reader = new StringReader (contents)) {
			line = reader.ReadLine ();	//headers
			LogB.Information(line);
			do {
				line = reader.ReadLine ();
				LogB.Information(line);
				if (line == null)
					break;

				curvesCount ++;

				string [] cells = line.Split(new char[] {','});
				cells = fixDecimals(cells);

				encoderCaptureCurves.Add (new EncoderCurve (
							false,				//user need to mark to save them
							cells[0],	//id 
							//cells[1],	//seriesName
							//cells[2], 	//exerciseName
							//cells[3], 	//massBody
							//cells[4], 	//massExtra
							cells[5], cells[6], cells[7], //start, duration, height 
							cells[8], cells[9], cells[10], //meanSpeed, maxSpeed, maxSpeedT
							cells[11], cells[12], cells[13], //meanPower, peakPower, peakPowerT
							cells[14]			//peakPower / peakPowerT
							));

			} while(true);
		}

		encoderCaptureListStore = new Gtk.ListStore (typeof (EncoderCurve));
		
		foreach (EncoderCurve curve in encoderCaptureCurves) 
			encoderCaptureListStore.AppendValues (curve);

		treeview_encoder_capture_curves.Model = encoderCaptureListStore;

		/*
		if(ecconLast == "c")
			treeview_encoder_capture_curves.Selection.Mode = SelectionMode.Single;
		else
			treeview_encoder_capture_curves.Selection.Mode = SelectionMode.Multiple;
			*/
		treeview_encoder_capture_curves.Selection.Mode = SelectionMode.None;

		treeview_encoder_capture_curves.HeadersVisible=true;
		
		
		//create first column (checkbox)	
		CellRendererToggle crt = new CellRendererToggle();
		crt.Visible = true;
		crt.Activatable = true;
		crt.Active = true;
		crt.Toggled += ItemToggled;
		Gtk.TreeViewColumn column = new Gtk.TreeViewColumn ();

		column.Title = Catalog.GetString("Saved");
		column.PackStart (crt, true);
		column.SetCellDataFunc (crt, new Gtk.TreeCellDataFunc (RenderRecord));
		treeview_encoder_capture_curves.AppendColumn (column);

		int i=0;
		foreach(string myCol in columnsString) {
			Gtk.TreeViewColumn aColumn = new Gtk.TreeViewColumn ();
			CellRendererText aCell = new CellRendererText();
			aColumn.Title=myCol;
			aColumn.PackStart (aCell, true);

			switch(i){	
				case 0:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderN));
					break;
				case 1:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderStart));
					break;
				case 2:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderDuration));
					break;
				case 3:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderHeight));
					break;
				case 4:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMeanSpeed));
					break;
				case 5:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMaxSpeed));
					break;
				case 6:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMaxSpeedT));
					break;
				case 7:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMeanPower));
					break;
				case 8:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderPeakPower));
					break;
				case 9:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderPeakPowerT));
					break;
				case 10:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderPP_PPT));
					break;
			}
					
			if( ! ( (i == 1 || i == 2) && ! showStartAndDuration ) )
				treeview_encoder_capture_curves.AppendColumn (aColumn);
			i++;
		}
		return curvesCount;
	}
	
	//rowNum starts at zero
	void saveOrDeleteCurveFromCaptureTreeView(int rowNum, EncoderCurve curve, bool save) 
	{
		LogB.Information("saving? " + save.ToString() + "; rownum:" + rowNum.ToString());
		if(save)
			encoderSaveSignalOrCurve("curve", rowNum +1);
		else {
			double msStart = Convert.ToDouble(curve.Start);
			double msEnd = -1;
			if(ecconLast == "c")
				msEnd = Convert.ToDouble(curve.Start) + 
					Convert.ToDouble(curve.Duration);
			else {
				EncoderCurve curveNext = 
					treeviewEncoderCaptureCurvesGetCurve(rowNum +2,false);
				msEnd = Convert.ToDouble(curveNext.Start) + 
					Convert.ToDouble(curveNext.Duration);
			}

			ArrayList signalCurves = SqliteEncoder.SelectSignalCurve(false,
					Convert.ToInt32(encoderSignalUniqueID), -1, 
					msStart, msEnd);
			foreach(EncoderSignalCurve esc in signalCurves)
				delete_encoder_curve(esc.curveID);
		}
	}

	void ItemToggled(object o, ToggledArgs args) {
		TreeIter iter;
		int column = 0;
		if (encoderCaptureListStore.GetIterFromString (out iter, args.Path)) 
		{
			int rowNum = Convert.ToInt32(args.Path); //starts at zero
			
			//on "ecS" don't pass the 2nd row, pass always the first
			//then need to move the iter to previous row
			TreePath path = new TreePath(args.Path);
			if(ecconLast != "c" && ! Util.IsEven(rowNum)) {
				rowNum --;
				path.Prev();
				//there's no "IterPre", for this reason we use this path method:
				encoderCaptureListStore.GetIter (out iter, path);
			
				/*
				 * caution, note args.Path has not changed; but path, iter and rowNum have decreased
				 * do not use args.Path from now
				 */
			}

			EncoderCurve curve = (EncoderCurve) encoderCaptureListStore.GetValue (iter, column);
			//get previous value
			bool val = curve.Record;

			//change value
			//this changes value, but checkbox will be changed on RenderRecord. Was impossible to do here.
			((EncoderCurve) encoderCaptureListStore.GetValue (iter, column)).Record = ! val;
				
			//this makes RenderRecord work on changed row without having to put mouse there
			encoderCaptureListStore.EmitRowChanged(path,iter);

			saveOrDeleteCurveFromCaptureTreeView(rowNum, curve, ! val);
			
			/* temporarily removed message
			 *
			string message = "";
			if(! val)
				message = Catalog.GetString("Saved");
			else
				message = Catalog.GetString("Removed");
			if(ecconLast ==	"c")
				label_encoder_curve_action.Text = message + " " + (rowNum +1).ToString();
			else
				label_encoder_curve_action.Text = message + " " + (decimal.Truncate((rowNum +1) /2) +1).ToString();
				*/


			//on ec, ecS need to [un]select second row
			if (ecconLast=="ec" || ecconLast =="ecS") {
				path.Next();
				encoderCaptureListStore.IterNext (ref iter);

				//change value
				((EncoderCurve) encoderCaptureListStore.GetValue (iter, column)).Record = ! val;

				//this makes RenderRecord work on changed row without having to put mouse there
				encoderCaptureListStore.EmitRowChanged(path,iter);
			}
			
			updateUserCurvesLabelsAndCombo();

			callPlotCurvesGraphDoPlot();
		}
	}

	//allNone: true (save all), false (unsave all)
	void encoderCaptureSaveCurvesAllNoneBest(Constants.EncoderAutoSaveCurve saveOption)
	{
		int bestRow = 0;
		if(saveOption == Constants.EncoderAutoSaveCurve.BESTMEANPOWER) {
			//get the concentric curves
			EncoderSignal encoderSignal = new EncoderSignal(treeviewEncoderCaptureCurvesGetCurves(AllEccCon.CON));
			bestRow = encoderSignal.FindPosOfBestMeanPower();
			
			//convert from c to ec. eg.
			//three concentric curves: c[0], c[1], c[2]
			//coming from three ecc-con: e[0], c[1], e[2], c[3], e[4], c[5]
			//if from first list, c[2] is the best, then on second list it will be the ec curve: e[4],c[5]
			//always multiply *2
			if(ecconLast != "c")
				bestRow *= 2;
		}


		int i = 0; //on "c" and ! "c": i is every row
		string sep = "";
		string messageRows = "";
		
		TreeIter iter;
		bool iterOk = encoderCaptureListStore.GetIterFirst(out iter);
		if(! iterOk)
			return;

		bool changeTo;
		while(iterOk) {
			TreePath path = encoderCaptureListStore.GetPath(iter);
			
			EncoderCurve curve = (EncoderCurve) encoderCaptureListStore.GetValue (iter, 0);
			if(
					(! curve.Record && saveOption == Constants.EncoderAutoSaveCurve.ALL) ||
					(! curve.Record && saveOption == Constants.EncoderAutoSaveCurve.BESTMEANPOWER && i == bestRow) ||
					(curve.Record && saveOption == Constants.EncoderAutoSaveCurve.BESTMEANPOWER && i != bestRow) ||
					(curve.Record && saveOption == Constants.EncoderAutoSaveCurve.NONE) ) 
			{ 
				changeTo = ! curve.Record;
				
				//change value
				((EncoderCurve) encoderCaptureListStore.GetValue (iter, 0)).Record = changeTo;

				//this makes RenderRecord work on changed row without having to put mouse there
				encoderCaptureListStore.EmitRowChanged(path,iter);

				//on "ecS" don't pass the 2nd row, pass always the first
				saveOrDeleteCurveFromCaptureTreeView(i, curve, changeTo);
				
				if(ecconLast != "c") {
					path.Next();
					encoderCaptureListStore.IterNext (ref iter);
				
					//change value
					((EncoderCurve) encoderCaptureListStore.GetValue (iter, 0)).Record = changeTo;

					//this makes RenderRecord work on changed row without having to put mouse there
					encoderCaptureListStore.EmitRowChanged(path,iter);
				}
					
				messageRows += sep + (i+1).ToString();
				sep = ", ";
			} else {
				//if we don't change rows
				//but is ec
				//the advance now one row (the 'e')
				//and later it will advance the 'c'
				if(ecconLast != "c") {
					encoderCaptureListStore.IterNext (ref iter);
				}
			}

			i ++;
			if(ecconLast != "c")
				i ++;

			iterOk = encoderCaptureListStore.IterNext (ref iter);
		}
		//combo_encoder_capture_show_save_curve_button();
		
		/* temporarily removed message
		 *
		string message = "";
		if(saveOption == Constants.EncoderAutoSaveCurve.NONE)
			message = Catalog.GetString("Removed");
		else
			message = Catalog.GetString("Saved");
		label_encoder_curve_action.Text = message + " " + messageRows;
		*/

			
		updateUserCurvesLabelsAndCombo();
			
		callPlotCurvesGraphDoPlot();
	}
	
	//saved curves (when load), or recently deleted curves should modify the encoderCapture treeview
	//used also on bells close
	void encoderCaptureSelectBySavedCurves(int msCentral, bool selectIt) {
		TreeIter iter;
		TreeIter iterPre;
		bool iterOk = encoderCaptureListStore.GetIterFirst(out iter);
		while(iterOk) {
			TreePath path = encoderCaptureListStore.GetPath(iter);
			EncoderCurve curve = (EncoderCurve) encoderCaptureListStore.GetValue (iter, 0);
			
			bool found = false;
			string eccon = findEccon(true);
			if(eccon == "c") {
				if(Convert.ToDouble(curve.Start) <= msCentral && 
						Convert.ToDouble(curve.Start) + Convert.ToDouble(curve.Duration) >= msCentral) 
				{
					((EncoderCurve) encoderCaptureListStore.GetValue (iter, 0)).Record = selectIt;

					//this makes RenderRecord work on changed row without having to put mouse there
					encoderCaptureListStore.EmitRowChanged(path,iter);
				}
			}
			else { // if(eccon == "ecS")
				iterPre = iter; //to point at the "e" curve
				iterOk = encoderCaptureListStore.IterNext (ref iter);
				EncoderCurve curve2 = (EncoderCurve) encoderCaptureListStore.GetValue (iter, 0);

				LogB.Information("msCentral, start, end" + msCentral.ToString() + " " + curve.Start + " " + 
						(Convert.ToDouble(curve2.Start) + Convert.ToDouble(curve2.Duration)).ToString());

				if(Convert.ToDouble(curve.Start) <= msCentral && 
						Convert.ToDouble(curve2.Start) + Convert.ToDouble(curve2.Duration) >= msCentral) 
				{
					((EncoderCurve) encoderCaptureListStore.GetValue (iterPre, 0)).Record = selectIt;
					((EncoderCurve) encoderCaptureListStore.GetValue (iter, 0)).Record = selectIt;

					//this makes RenderRecord work on changed row without having to put mouse there
					encoderCaptureListStore.EmitRowChanged(path,iterPre);
					encoderCaptureListStore.EmitRowChanged(path,iter);
				}
			}

			iterOk = encoderCaptureListStore.IterNext (ref iter);
		}
		//combo_encoder_capture_show_save_curve_button();
			
		callPlotCurvesGraphDoPlot();
	}

	/*	
	void combo_encoder_capture_show_save_curve_button () {
		label_encoder_curve_action.Text = "";

		TreeIter iter;
		bool iterOk = encoderCaptureListStore.GetIterFirst(out iter);
		while(iterOk) {
			if(((EncoderCurve) encoderCaptureListStore.GetValue (iter, 0)).Record) {
				encoderButtonsSensitive(encoderSensEnum.SELECTEDCURVE);
				return;
			}
			iterOk = encoderCaptureListStore.IterNext (ref iter);
		}
		encoderButtonsSensitive(encoderSensEnum.DONEYESSIGNAL);
	}
	*/



	string [] treeviewEncoderAnalyzeHeaders = {
		Catalog.GetString("Repetition") + "\n",
		Catalog.GetString("Series") + "\n",
		Catalog.GetString("Exercise") + "\n",
		Catalog.GetString("Extra weight") + "\n (Kg)",
		Catalog.GetString("Total weight") + "\n (Kg)",
		Catalog.GetString("Start") + "\n (s)",
		Catalog.GetString("Duration") + "\n (s)",
		Catalog.GetString("Distance") + "\n (cm)",
		"v" + "\n (m/s)",
		"vmax" + "\n (m/s)",
		"t->vmax" + "\n (s)",
		"p" + "\n (W)",
		"pmax" + "\n (W)",
		"t->pmax" + "\n (s)",
		"pmax/t->pmax" + "\n (W/s)"
	};

	bool lastTreeviewEncoderAnalyzeIsNeuromuscular = false;

	private int createTreeViewEncoderAnalyze(string contents) {
		string [] columnsString = treeviewEncoderAnalyzeHeaders;

		ArrayList encoderAnalyzeCurves = new ArrayList ();

		//write exercise and extra weight data
		ArrayList curvesData = new ArrayList();
		string exerciseName = "";
		double totalMass = 0; 
		if(check_encoder_analyze_signal_or_curves.Active) {	//current signal
			exerciseName = UtilGtk.ComboGetActive(combo_encoder_exercise);
			totalMass = findMass(Constants.MassType.DISPLACED);
		} else {						//user curves
			curvesData = SqliteEncoder.Select(
					false, -1, currentPerson.UniqueID, currentSession.UniqueID, -1,
					"curve", EncoderSQL.Eccons.ALL, 
					true, true);
		}

		string line;
		int curvesCount = 0;
		using (StringReader reader = new StringReader (contents)) {
			line = reader.ReadLine ();	//headers
			LogB.Information(line);
			do {
				line = reader.ReadLine ();
				LogB.Information(line);
				if (line == null)
					break;

				curvesCount ++;

				string [] cells = line.Split(new char[] {','});
				cells = fixDecimals(cells);
				
				
				if(! check_encoder_analyze_signal_or_curves.Active) {	//user curves
					/*
					 * better don't do this to avoid calling SQL in both treads
					EncoderSQL eSQL = (EncoderSQL) curvesData[curvesCount];
					exerciseName = eSQL.exerciseName;
					totalMass = eSQL.extraWeight;
					*/
					exerciseName = cells[2];
					//cells[3]: massBody
					//cells[4]: massExtra

					totalMass = Convert.ToDouble(cells[3]) * getExercisePercentBodyWeightFromName (exerciseName) / 100.0
						+ Convert.ToDouble(cells[4]);
				}

				encoderAnalyzeCurves.Add (new EncoderCurve (
							cells[0], 
							cells[1],	//seriesName 
							exerciseName,
							Convert.ToDouble(cells[4]),
							totalMass,
							cells[5], cells[6], cells[7], 
							cells[8], cells[9], cells[10], 
							cells[11], cells[12], cells[13],
							cells[14]
							));

			} while(true);
		}

		encoderAnalyzeListStore = new Gtk.ListStore (typeof (EncoderCurve));
		foreach (EncoderCurve curve in encoderAnalyzeCurves) 
			encoderAnalyzeListStore.AppendValues (curve);

		treeview_encoder_analyze_curves.Model = encoderAnalyzeListStore;

		treeview_encoder_analyze_curves.Selection.Mode = SelectionMode.None;

		treeview_encoder_analyze_curves.HeadersVisible=true;

		int i=0;
		foreach(string myCol in columnsString) {
			Gtk.TreeViewColumn aColumn = new Gtk.TreeViewColumn ();
			CellRendererText aCell = new CellRendererText();
			aColumn.Title=myCol;
			aColumn.PackStart (aCell, true);

			//crt1.Foreground = "red";
			//crt1.Background = "blue";
		
		
			switch(i){	
				case 0:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderNAnalyze));
					break;
				case 1:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderSeries));
					break;
				case 2:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderExercise));
					break;
				case 3:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderExtraWeight));
					break;
				case 4:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderDisplacedWeight));
					break;
				case 5:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderStart));
					break;
				case 6:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderDuration));
					break;
				case 7:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderHeight));
					break;
				case 8:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMeanSpeed));
					break;
				case 9:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMaxSpeed));
					break;
				case 10:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMaxSpeedT));
					break;
				case 11:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderMeanPower));
					break;
				case 12:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderPeakPower));
					break;
				case 13:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderPeakPowerT));
					break;
				case 14:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (RenderPP_PPT));
					break;
			}
			
			treeview_encoder_analyze_curves.AppendColumn (aColumn);
			i++;
		}

		lastTreeviewEncoderAnalyzeIsNeuromuscular = false; 

		return curvesCount;
	}

	string [] treeviewEncoderAnalyzeNeuromuscularHeaders = {
		"n" + "\n",
		"e1 range" + "\n (mm)",
		"e1 t" + "\n (ms)",
		"e1 fmax" + "\n (N)",
		"e1 rfd avg" + "\n (N/s)",
		"e1 i" + "\n (N*s/Kg)",
		"ca range" + "\n (mm)",
		"cl t" + "\n (ms)",
		"cl rfd avg" + "\n (N/s/Kg)",
		"cl i" + "\n (N*s/Kg)",
		"cl f avg" + "\n (N/Kg)",
		"cl vf" + "\n (N)",
		"cl f max" + "\n (N)",
		"cl s avg" + "\n (m/s)",
		"cl s max" + "\n (m/s)",
		"cl p avg" + "\n (W)",
		"cl p max" + "\n (W)"
	};

	private int createTreeViewEncoderAnalyzeNeuromuscular(string contents) {
		string [] columnsString = treeviewEncoderAnalyzeNeuromuscularHeaders;

		ArrayList encoderAnalyzeNm = new ArrayList ();

		string line;
		int curvesCount = 0;
		using (StringReader reader = new StringReader (contents)) {
			line = reader.ReadLine ();	//headers
			LogB.Information(line);
			do {
				line = reader.ReadLine ();
				LogB.Information(line);
				if (line == null)
					break;

				curvesCount ++;

				string [] cells = line.Split(new char[] {','});
				encoderAnalyzeNm.Add (new EncoderNeuromuscularData (cells));

			} while(true);
		}

		encoderAnalyzeListStore = new Gtk.ListStore (typeof (EncoderNeuromuscularData));
		foreach (EncoderNeuromuscularData nm in encoderAnalyzeNm) 
			encoderAnalyzeListStore.AppendValues (nm);

		treeview_encoder_analyze_curves.Model = encoderAnalyzeListStore;

		treeview_encoder_analyze_curves.Selection.Mode = SelectionMode.None;

		treeview_encoder_analyze_curves.HeadersVisible=true;

		int i=0;
		foreach(string myCol in columnsString) {
			
			Gtk.TreeViewColumn aColumn = new Gtk.TreeViewColumn ();
			CellRendererText aCell = new CellRendererText();
			aColumn.Title=myCol;
			aColumn.PackStart (aCell, true);

			//crt1.Foreground = "red";
			//crt1.Background = "blue";
		
			switch(i){	
				case 0:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_jump_num));
					break;
				case 1:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_e1_range));
					break;
				case 2:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_e1_t));
					break;
				case 3:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_e1_fmax));
					break;
				case 4:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_e1_rfd_avg));
					break;
				case 5:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_e1_i));
					break;
				case 6:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_ca_range));
					break;
				case 7:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_t));
					break;
				case 8:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_rfd_avg));
					break;
				case 9:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_i));
					break;
				case 10:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_f_avg));
					break;
				case 11:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_vf));
					break;
				case 12:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_f_max));
					break;
				case 13:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_s_avg));
					break;
				case 14:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_s_max));
					break;
				case 15:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_p_avg));
					break;
				case 16:
					aColumn.SetCellDataFunc (aCell, new Gtk.TreeCellDataFunc (Render_cl_p_max));
					break;
			}
			
			treeview_encoder_analyze_curves.AppendColumn (aColumn);
			i++;
		}
		
		lastTreeviewEncoderAnalyzeIsNeuromuscular = true; 
		
		return curvesCount;
	}



	/* start rendering capture and analyze cols */

	private string assignColor(double found, bool higherActive, bool lowerActive, double higherValue, double lowerValue) 
	{
		//more at System.Drawing.Color (Monodoc)
		string colorGood= "ForestGreen"; 
		string colorBad= "red";
		string colorNothing= "";	
		//colorNothing will use default color on system, previous I used black,
		//but if the color of the users theme is not 000000, then it looked too different

		if(higherActive && found >= higherValue)
			return colorGood;
		else if(lowerActive && found <= lowerValue)
			return colorBad;
		else
			return colorNothing;
	}

	private string assignColor(double found, bool higherActive, bool lowerActive, int higherValue, int lowerValue) 
	{
		return assignColor(found, higherActive, lowerActive, 
				Convert.ToDouble(higherValue), Convert.ToDouble(lowerValue));
	}
	
	private void RenderRecord (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) {
		(cell as Gtk.CellRendererToggle).Active = ((EncoderCurve) model.GetValue (iter, 0)).Record;
	}

	private void RenderN (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		//do this in order to have ecconLast useful for RenderN when capturing
		if(capturingCsharp == encoderCaptureProcess.CAPTURING)
			ecconLast = findEccon(false);

		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		if(ecconLast == "c")
			(cell as Gtk.CellRendererText).Text = 
				String.Format(UtilGtk.TVNumPrint(curve.N,1,0),Convert.ToInt32(curve.N));
		else if (ecconLast=="ec" || ecconLast =="ecS") {
			string phase = "e";
			bool isEven = Util.IsEven(Convert.ToInt32(curve.N));
			if(isEven)
				phase = "c";
				
			(cell as Gtk.CellRendererText).Text = 
				decimal.Truncate((Convert.ToInt32(curve.N) +1) /2).ToString() + phase;
		} else {	//(ecconLast=="ce" || ecconLast =="ceS") {
			string phase = "c";
			bool isEven = Util.IsEven(Convert.ToInt32(curve.N));
			if(isEven)
				phase = "e";
				
			(cell as Gtk.CellRendererText).Text = 
				decimal.Truncate((Convert.ToInt32(curve.N) +1) /2).ToString() + phase;
		}
	}
	//from analyze, don't checks ecconLast
	private void RenderNAnalyze (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		
		if(check_encoder_analyze_signal_or_curves.Active && findEccon(false) == "ecS") 
		{
			string phase = "e";
			bool isEven = Util.IsEven(Convert.ToInt32(curve.N));
			if(isEven)
				phase = "c";

			(cell as Gtk.CellRendererText).Text = 
				decimal.Truncate((Convert.ToInt32(curve.N) +1) /2).ToString() + phase;
		}
		else if(check_encoder_analyze_signal_or_curves.Active && findEccon(false) == "ceS") 
		{
			string phase = "c";
			bool isEven = Util.IsEven(Convert.ToInt32(curve.N));
			if(isEven)
				phase = "e";

			(cell as Gtk.CellRendererText).Text = 
				decimal.Truncate((Convert.ToInt32(curve.N) +1) /2).ToString() + phase;
		} else
			(cell as Gtk.CellRendererText).Text = curve.N;
	}

	private void RenderSeries (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = curve.Series;
	}

	private void RenderExercise (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = curve.Exercise;
	}

	private void RenderExtraWeight (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(curve.ExtraWeight.ToString(),3,0),Convert.ToInt32(curve.ExtraWeight));
	}

	private void RenderDisplacedWeight (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(curve.DisplacedWeight.ToString(),3,0),Convert.ToInt32(curve.DisplacedWeight));
	}

	private void RenderStart (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		double myStart = Convert.ToDouble(curve.Start)/1000; //ms->s
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(myStart.ToString(),6,3),myStart); 
	}
	
	private void RenderDuration (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		double myDuration = Convert.ToDouble(curve.Duration)/1000; //ms->s
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(myDuration.ToString(),5,3),myDuration); 
	}
	
	private void RenderHeight (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		string heightToCm = (Convert.ToDouble(curve.Height)/10).ToString();
		string myColor = assignColor(
				Convert.ToDouble(heightToCm),
				repetitiveConditionsWin.EncoderHeightHigher, 
				repetitiveConditionsWin.EncoderHeightLower, 
				repetitiveConditionsWin.EncoderHeightHigherValue,
				repetitiveConditionsWin.EncoderHeightLowerValue);
		if(myColor != "")
			(cell as Gtk.CellRendererText).Foreground = myColor;
		else
			(cell as Gtk.CellRendererText).Foreground = null;	//will show default color

		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(heightToCm,5,1),Convert.ToDouble(heightToCm));
	}
	
	private void RenderMeanSpeed (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		string myColor = assignColor(
				Convert.ToDouble(curve.MeanSpeed),
				repetitiveConditionsWin.EncoderMeanSpeedHigher, 
				repetitiveConditionsWin.EncoderMeanSpeedLower, 
				repetitiveConditionsWin.EncoderMeanSpeedHigherValue,
				repetitiveConditionsWin.EncoderMeanSpeedLowerValue);
		if(myColor != "")
			(cell as Gtk.CellRendererText).Foreground = myColor;
		else
			(cell as Gtk.CellRendererText).Foreground = null;	//will show default color

		//no need of UtilGtk.TVNumPrint, always has 1 digit on left of decimal
		(cell as Gtk.CellRendererText).Text = 
			String.Format("{0,8:0.000}",Convert.ToDouble(curve.MeanSpeed));
	}

	private void RenderMaxSpeed (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		string myColor = assignColor(
				Convert.ToDouble(curve.MaxSpeed),
				repetitiveConditionsWin.EncoderMaxSpeedHigher, 
				repetitiveConditionsWin.EncoderMaxSpeedLower, 
				repetitiveConditionsWin.EncoderMaxSpeedHigherValue,
				repetitiveConditionsWin.EncoderMaxSpeedLowerValue);
		if(myColor != "")
			(cell as Gtk.CellRendererText).Foreground = myColor;
		else
			(cell as Gtk.CellRendererText).Foreground = null;	//will show default color

		//no need of UtilGtk.TVNumPrint, always has 1 digit on left of decimal
		(cell as Gtk.CellRendererText).Text = 
			String.Format("{0,8:0.000}",Convert.ToDouble(curve.MaxSpeed));
	}
	
	private void RenderMaxSpeedT (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		double time = Convert.ToDouble(curve.MaxSpeedT)/1000; //ms->s
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(time.ToString(),5,3),time);
	}

	private void RenderMeanPower (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		string myColor = assignColor(
				Convert.ToDouble(curve.MeanPower),
				repetitiveConditionsWin.EncoderPowerHigher, 
				repetitiveConditionsWin.EncoderPowerLower, 
				repetitiveConditionsWin.EncoderPowerHigherValue,
				repetitiveConditionsWin.EncoderPowerLowerValue);
		if(myColor != "")
			(cell as Gtk.CellRendererText).Foreground = myColor;
		else
			(cell as Gtk.CellRendererText).Foreground = null;	//will show default color

		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(curve.MeanPower,7,1),Convert.ToDouble(curve.MeanPower));
	}

	private void RenderPeakPower (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		string myColor = assignColor(
				Convert.ToDouble(curve.PeakPower),
				repetitiveConditionsWin.EncoderPeakPowerHigher, 
				repetitiveConditionsWin.EncoderPeakPowerLower, 
				repetitiveConditionsWin.EncoderPeakPowerHigherValue,
				repetitiveConditionsWin.EncoderPeakPowerLowerValue);
		if(myColor != "")
			(cell as Gtk.CellRendererText).Foreground = myColor;
		else
			(cell as Gtk.CellRendererText).Foreground = null;	//will show default color

		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(curve.PeakPower,7,1),Convert.ToDouble(curve.PeakPower));
	}

	private void RenderPeakPowerT (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		double myPPT = Convert.ToDouble(curve.PeakPowerT)/1000; //ms->s
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(myPPT.ToString(),5,3),myPPT);
	}

	private void RenderPP_PPT (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderCurve curve = (EncoderCurve) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = 
			String.Format(UtilGtk.TVNumPrint(curve.PP_PPT,6,1),Convert.ToDouble(curve.PP_PPT));
	}
	
	/* end of rendering capture and analyze cols */

	/* start rendering neuromuscular cols */

	private void Render_jump_num (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.n.ToString();
	}

	private void Render_e1_range (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.e1_range.ToString();
	}

	private void Render_e1_t (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.e1_t.ToString();
	}

	private void Render_e1_fmax (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.e1_fmax.ToString();
	}

	private void Render_e1_rfd_avg (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.e1_rfd_avg.ToString();
	}

	private void Render_e1_i (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.e1_i.ToString();
	}

	private void Render_ca_range (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.ca_range.ToString();
	}

	private void Render_cl_t (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_t.ToString();
	}

	private void Render_cl_rfd_avg (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_rfd_avg.ToString();
	}

	private void Render_cl_i (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_i.ToString();
	}

	private void Render_cl_f_avg (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_f_avg.ToString();
	}

	private void Render_cl_vf (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_vf.ToString();
	}

	private void Render_cl_f_max (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_f_max.ToString();
	}

	private void Render_cl_s_avg (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_s_avg.ToString();
	}

	private void Render_cl_s_max (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_s_max.ToString();
	}

	private void Render_cl_p_avg (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_p_avg.ToString();
	}

	private void Render_cl_p_max (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		EncoderNeuromuscularData nm = (EncoderNeuromuscularData) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = nm.cl_p_max.ToString();
	}


	/* end of rendering neuromuscular cols */
	
	
	private string [] fixDecimals(string [] cells) {
		//start, width, height
		for(int i=5; i <= 7; i++)
			cells[i] = Util.TrimDecimals(Convert.ToDouble(Util.ChangeDecimalSeparator(cells[i])),1);
		
		//meanSpeed,maxSpeed,maxSpeedT, meanPower,peakPower,peakPowerT
		for(int i=8; i <= 13; i++)
			cells[i] = Util.TrimDecimals(Convert.ToDouble(Util.ChangeDecimalSeparator(cells[i])),3);
		
		//pp/ppt
		int pp_ppt = 14;
		cells[pp_ppt] = Util.TrimDecimals(Convert.ToDouble(Util.ChangeDecimalSeparator(cells[pp_ppt])),1); 
		return cells;
	}
	
	//the bool is for ecc-concentric
	//there two rows are selected
	//if user clicks on 2n row, and bool is true, first row is the returned curve
	private EncoderCurve treeviewEncoderCaptureCurvesGetCurve(int row, bool onEccConTakeFirst) 
	{
		if(onEccConTakeFirst && ecconLast != "c") {
			bool isEven = (row % 2 == 0); //check if it's even (in spanish "par")
			if(isEven)
				row --;
		}

		TreeIter iter = new TreeIter();
		bool iterOk = encoderCaptureListStore.GetIterFirst(out iter);
		if(iterOk) {
			int count=1;
			do {
				if(count==row) 
					return (EncoderCurve) treeview_encoder_capture_curves.Model.GetValue (iter, 0);
				count ++;
			} while (encoderCaptureListStore.IterNext (ref iter));
		}
		EncoderCurve curve = new EncoderCurve();
		return curve;
	}

	private enum AllEccCon { ALL, ECC, CON }

	private ArrayList treeviewEncoderCaptureCurvesGetCurves(AllEccCon option) 
	{
		TreeIter iter;
		ArrayList curves = new ArrayList();
			
		bool iterOk = encoderCaptureListStore.GetIterFirst(out iter);
		if(! iterOk)
			return curves;

		bool oddRow = true;
		while(iterOk) {
			if(ecconLast != "c" && option == AllEccCon.CON && oddRow) {
				oddRow = ! oddRow;
				iterOk = encoderCaptureListStore.IterNext (ref iter);
				continue;
			}
			if(ecconLast != "c" && option == AllEccCon.ECC && ! oddRow) {
				oddRow = ! oddRow;
				iterOk = encoderCaptureListStore.IterNext (ref iter);
				continue;
			}
				
			EncoderCurve curve = (EncoderCurve) encoderCaptureListStore.GetValue (iter, 0);
			curves.Add(curve);

			oddRow = ! oddRow;
			iterOk = encoderCaptureListStore.IterNext (ref iter);
		}

		return curves;
	}
	
	// ---------helpful methods -----------
	
	ArrayList getTreeViewCurves(Gtk.ListStore ls) {
		TreeIter iter = new TreeIter();
		ls.GetIterFirst ( out iter ) ;
		ArrayList array = new ArrayList();
		do {
			EncoderCurve ec = (EncoderCurve) ls.GetValue (iter, 0);
			array.Add(ec);
		} while (ls.IterNext (ref iter));
		return array;
	}

	ArrayList getTreeViewNeuromuscular(Gtk.ListStore ls) {
		TreeIter iter = new TreeIter();
		ls.GetIterFirst ( out iter ) ;
		ArrayList array = new ArrayList();
		do {
			EncoderNeuromuscularData nm = (EncoderNeuromuscularData) ls.GetValue (iter, 0);
			array.Add(nm);
		} while (ls.IterNext (ref iter));
		return array;
	}

	/* end of TreeView stuff */	

}
