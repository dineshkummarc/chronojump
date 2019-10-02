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
 * Copyright (C) 2019   Xavier de Blas <xaviblas@gmail.com>
 * Copyright (C) 2019  	Xavier Padullés <x.padulles@gmail.com>
 */

using System;
using Gdk; //Pixbuf
using Gtk;
using Glade;
using GLib; //for Value
using System.Collections.Generic; //List<T>
//using Mono.Unix; TODO: when uncomment need to change POTFILES.in


public class ForceSensorExerciseWindow
{
	[Widget] Gtk.Window force_sensor_exercise;
	[Widget] Gtk.Label label_header;
	/*
	   [Widget] Gtk.Box hbox_error;
	   [Widget] Gtk.Label label_error;
	   */
	[Widget] Gtk.Entry entry_name;
	[Widget] Gtk.Notebook notebook_main;

	//each of the rows of the table
	[Widget] Gtk.Label label_force;
	[Widget] Gtk.TextView textview_force_explanation;
	[Widget] Gtk.RadioButton radio_force_sensor;
	[Widget] Gtk.RadioButton radio_force_resultant;	

	[Widget] Gtk.Label label_fixation;
	[Widget] Gtk.TextView textview_fixation_explanation;
	[Widget] Gtk.RadioButton radio_fixation_elastic;
	[Widget] Gtk.RadioButton radio_fixation_not_elastic;

	[Widget] Gtk.Label label_mass;
	[Widget] Gtk.TextView textview_mass_explanation;
	[Widget] Gtk.RadioButton radio_mass_add;
	[Widget] Gtk.RadioButton radio_mass_subtract;
	[Widget] Gtk.RadioButton radio_mass_nothing;
	[Widget] Gtk.HBox hbox_body_mass_add;
	[Widget] Gtk.SpinButton spin_body_mass_add;

	[Widget] Gtk.Label label_angle;
	[Widget] Gtk.TextView textview_angle_explanation;
	[Widget] Gtk.SpinButton spin_angle;

	[Widget] Gtk.Notebook notebook_desc_examples;
	[Widget] Gtk.Label label_notebook_desc_examples_desc;
	[Widget] Gtk.Label label_notebook_desc_examples_examples;
	[Widget] Gtk.TextView textview_description;
	[Widget] Gtk.TextView textview_examples;

	[Widget] Gtk.Button button_next_or_accept;
	[Widget] Gtk.Button button_back;

	[Widget] Gtk.Image image_cancel;
	[Widget] Gtk.Image image_next_or_accept;
	[Widget] Gtk.Image image_back;

	private enum Pages { FORCE, FIXATION, MASS, ANGLE }
	private enum Options { FORCE_SENSOR, FORCE_RESULTANT, FIXATION_ELASTIC, FIXATION_NOT_ELASTIC, MASS_ADD, MASS_SUBTRACT, MASS_NOTHING, ANGLE }

	static ForceSensorExerciseWindow ForceSensorExerciseWindowBox;

	/*
	   public int uniqueID; 			//used on encoder & forceSensor edit exercise
	   public string nameUntranslated;		//used on encoder edit exercise
	   */

	public ForceSensorExerciseWindow (string title, string textHeader)
	{
		Glade.XML gladeXML;
		gladeXML = Glade.XML.FromAssembly (Util.GetGladePath() + "force_sensor_exercise.glade", "force_sensor_exercise", "chronojump");
		gladeXML.Autoconnect(this);

		//put an icon to window
		UtilGtk.IconWindow(force_sensor_exercise);

		force_sensor_exercise.Resizable = false;
		setTitle(title);
		label_header.Text = textHeader;

		initializeGuiAtCreation();

		//HideOnAccept = true;
		//DestroyOnAccept = false;
	}

	static public ForceSensorExerciseWindow Show (string title, string textHeader)
	{
		if (ForceSensorExerciseWindowBox == null) {
			ForceSensorExerciseWindowBox = new ForceSensorExerciseWindow(title, textHeader);
		} else {
			ForceSensorExerciseWindowBox.setTitle(title);
			ForceSensorExerciseWindowBox.label_header.Text = textHeader;
		}

		ForceSensorExerciseWindowBox.initializeGuiAtShow();
		ForceSensorExerciseWindowBox.force_sensor_exercise.Show ();

		return ForceSensorExerciseWindowBox;
	}

	private void setTitle(string title)
	{
		if(title != "")
			force_sensor_exercise.Title = "Chronojump - " + title;
	}

	private void initializeGuiAtCreation()
	{
		// 1. show title label at each notebook_main page on bold
		label_force.Text = "<b>" + label_force.Text + "</b>";
		label_fixation.Text = "<b>" + label_fixation.Text + "</b>";
		label_mass.Text = "<b>" + label_mass.Text + "</b>";
		label_angle.Text = "<b>" + label_angle.Text + "</b>";

		label_force.UseMarkup = true;
		label_fixation.UseMarkup = true;
		label_mass.UseMarkup = true;
		label_angle.UseMarkup = true;

		// 2. textviews of explanations of each page
		textview_force_explanation.Buffer.Text = getTopExplanations(Pages.FORCE);
		textview_fixation_explanation.Buffer.Text = getTopExplanations(Pages.FIXATION);
		textview_mass_explanation.Buffer.Text = getTopExplanations(Pages.MASS);
		// done below textview_angle_explanation.Buffer.Text = getTopExplanations(Pages.ANGLE);

		// 3. icons
		image_cancel.Pixbuf = new Pixbuf (null, Util.GetImagePath(false) + "image_cancel.png");
		image_next_or_accept.Pixbuf = new Pixbuf (null, Util.GetImagePath(false) + "arrow_forward.png");
		image_back.Pixbuf = new Pixbuf (null, Util.GetImagePath(false) + "arrow_back.png");
	}

	private void initializeGuiAtShow()
	{
		managePage(Pages.FORCE);
		ForceSensorExerciseWindowBox.notebook_main.CurrentPage = Convert.ToInt32(Pages.FORCE);
		spin_body_mass_add.Value = 100;
	}

	//just to have shorter code
	//and to be able to upload this without bothering the translators at the moment
	private string ss (string s)
	{
		//return Catalog.GetString(s);
		return s;
	}

	private string getTopExplanations (Pages p)
	{
		string str;
		if(p == Pages.FORCE)
			str = ss("In some cases the force registered by the Force Sensor is not directly the force that the person is exerting.");
		else if(p == Pages.FIXATION)
			str = ss("How the force is transmitted to the sensor");
		else if(p == Pages.MASS)
			str = ss("Depending on the exercise and configuration of the test, the total mass (mass of the person and the extra load) can affect to the sensor measuring. Select how to manage this effect.");
		else { //if(p == Pages.ANGLE)
			if(radio_force_resultant.Active && radio_mass_add.Active)
				str = ss("In current exercise configuration, it is necessary to enter the angle in which the sensor is measuring.");
			else
				str = ss("In current exercise configuration, angle is merely descriptive (not used in calculations).");
		}

		return str;
	}

	private string getDescription (Options o)
	{
		string str;
		if(o == Options.FORCE_SENSOR)
			str = ss("When you are interested only in the force transmitted to the force sensor. This option do NOT take into account the effect of elastic elements or the mass or the acceleration of a mass.");
		else if(o == Options.FORCE_RESULTANT)
			str = ss("When you want the resultant of all the forces exerted by the person. This value is the vector module of the resultant force vector. This option allows to take in account the effect of the elastic elements or the acceleration of a mass.");
		else if(o == Options.FIXATION_ELASTIC)
			str = ss("If exerting a force, some element is significantly elongated it means that you are using elastic elements. Knowing the characteristics of the elastic elements allows to calculate positions, velocities and accelerations during the exercise.");
		else if(o == Options.FIXATION_NOT_ELASTIC)
			str = ss("If exerting a force there's no significant movement you are using not elastic elements.");
		else if(o == Options.MASS_ADD)
			str = ss("When the mass don't affect the sensor data but it must be added to it.");
		else if(o == Options.MASS_SUBTRACT)
			str = ss("In some cases the weight if the mass is supported by the sensor but it is not a force that the subject is exerting. In this case, the sensor will be tared before starting the test.");
		else if(o == Options.MASS_NOTHING)
			str = ss("In some cases the weight is transmitted to the sensor and it is also supported by the measured limb. If the effect of the mass is not significant, use this option also.");
		else //if(o == Options.ANGLE)
			str = ss("0 means horizontally") + "\n" +
				ss("90 means vertically with the person above the sensor") + "\n" +
				ss("-90 means vertically with the person below the sensor");

		return str;
	}

	private string getExample (Options o)
	{
		string str;
		if(o == Options.FORCE_SENSOR)
			str = "1.- " + ss("Isometric Leg Extension.") +
				"\n2.- " + ss("Upper limb movements against a rubber if the displaced mass is considered insignificant.");
		else if(o == Options.FORCE_RESULTANT)
			str = "1.- " + ss("Isometric squat with the force sensor fixed between the floor and the body.") +
				"\n2.- " + ss("Movements where a significant mass is accelerated.") +
				"\n3.- " + ss("Horizontal movements where the sensor don't measure the gravitational vertical forces...)");
		else if(o == Options.FIXATION_ELASTIC)
			str =  ss("Rubber bands, springs, flexible material ...");
		else if(o == Options.FIXATION_NOT_ELASTIC)
			str = "1.- " + ss("In an isometric squat with the force sensor fixed between the floor and the body, increasing the mass don't affect the measure of the sensor because the weight is supported by the lower limbs, not the sensor.") +
				"\n2.- " + ss("Running in a threadmill against a rubber. The sensor is measuring the force that a rubber is transmitting horizontally to a subject running in a threadmill. The body weight is added to the total force exerted by the subject.");
		else if(o == Options.MASS_ADD)
			str = "1.- " + ss("In an isometric squat with the force sensor fixed between the floor and the body, increasing the mass don't affect the measure of the sensor because the weight is supported by the lower limbs, not the sensor.") +
				"\n2.- " + ss("Running in a threadmill against a rubber. The sensor is measuring the force that a rubber is transmitting horizontally to a subject running in a threadmill. The body weight is added to the total force exerted by the subject.");
		else if(o == Options.MASS_SUBTRACT)
			str = ss("Hamstring test where the heel of the person is suspended in a cinch attached to the sensor. The weight of the leg is affecting the measure of the force transmitted to the sensor but this is not a force exerted by the subject.");
		else if(o == Options.MASS_NOTHING)
			str = "1.- " + ss("Nordic hamstring. In a Nordic hamstring with the sensor attached to the ankle, the weight affects the values of the sensor but this weight is supported by the hamstrings we are measuring.") +
				"\n2.- " + ss("Pulling on a TRX. Pulling from a TRX implies overcome the body weight. This body weight is also measured by the sensor.");
		else //if(o == Options.ANGLE)
			str = "";

		return str;
	}

	private void set_notebook_desc_example_labels(Options o)
	{
		string str;

		if(o == Options.FORCE_SENSOR)
			str = ss("Raw data");
		else if(o == Options.FORCE_RESULTANT)
			str = ss("Resultant force");
		else if(o == Options.FIXATION_ELASTIC)
			str = ss("Elastic");
		else if(o == Options.FIXATION_NOT_ELASTIC)
			str = ss("Not Elastic");
		else if(o == Options.MASS_ADD)
			str = ss("Add mass");
		else if(o == Options.MASS_SUBTRACT)
			str = ss("Subtract mass");
		else if(o == Options.MASS_NOTHING)
			str = ss("Mass is included");
		else //if(o == Options.ANGLE)
			str = ss("Description");

		label_notebook_desc_examples_desc.Text = str;
		label_notebook_desc_examples_examples.Text = ss("Examples of:") + " " + str;
	}

	private void managePage(int i)
	{
		//convert to int to enum
		Pages p = (Pages)Enum.ToObject(typeof(Pages) , i);
		managePage(p);
	}
	private void managePage(Pages p)
	{
		string desc;
		string ex;

		//default for most of the pages
		button_next_or_accept.Sensitive = true;
		button_back.Sensitive = true;
		notebook_desc_examples.GetNthPage(1).Show();

		if(p == Pages.FORCE)
		{
			button_back.Sensitive = false;

			if(radio_force_sensor.Active) {
				desc = getDescription(Options.FORCE_SENSOR);
				ex = getExample(Options.FORCE_SENSOR);
				set_notebook_desc_example_labels(Options.FORCE_SENSOR);
			} else {
				desc = getDescription(Options.FORCE_RESULTANT);
				ex = getExample(Options.FORCE_RESULTANT);
				set_notebook_desc_example_labels(Options.FORCE_RESULTANT);
			}
		}
		else if(p == Pages.FIXATION)
		{

			if(radio_fixation_elastic.Active) {
				desc = getDescription(Options.FIXATION_ELASTIC);
				ex = getExample(Options.FIXATION_ELASTIC);
				set_notebook_desc_example_labels(Options.FIXATION_ELASTIC);
			} else {
				desc = getDescription(Options.FIXATION_NOT_ELASTIC);
				ex = getExample(Options.FIXATION_NOT_ELASTIC);
				set_notebook_desc_example_labels(Options.FIXATION_NOT_ELASTIC);
			}
		}
		else if(p == Pages.MASS)
		{

			if(radio_mass_add.Active) {
				desc = getDescription(Options.MASS_ADD);
				ex = getExample(Options.MASS_ADD);
				set_notebook_desc_example_labels(Options.MASS_ADD);
			} else if(radio_mass_subtract.Active) {
				desc = getDescription(Options.MASS_SUBTRACT);
				ex = getExample(Options.MASS_SUBTRACT);
				set_notebook_desc_example_labels(Options.MASS_SUBTRACT);
			} else { // (radio_mass_nothing.Active)
				desc = getDescription(Options.MASS_NOTHING);
				ex = getExample(Options.MASS_NOTHING);
				set_notebook_desc_example_labels(Options.MASS_NOTHING);
			}
			hbox_body_mass_add.Sensitive = radio_mass_add.Active;
		}
		else // if(p == Pages.ANGLE)
		{
			button_next_or_accept.Sensitive = false;
			textview_angle_explanation.Buffer.Text = getTopExplanations(Pages.ANGLE);

			desc = getDescription(Options.ANGLE);
			ex = getExample(Options.ANGLE);
			set_notebook_desc_example_labels(Options.ANGLE);

			notebook_desc_examples.CurrentPage = 0;
			notebook_desc_examples.GetNthPage(1).Hide();
		}

		textview_description.Buffer.Text = desc;
		textview_examples.Buffer.Text = ex;
	}


	private void on_button_next_clicked (object o, EventArgs args)
	{
		if(notebook_main.CurrentPage == Convert.ToInt32(Pages.FORCE) && radio_force_sensor.Active)
			notebook_main.CurrentPage = Convert.ToInt32(Pages.ANGLE);
		else if(notebook_main.CurrentPage < Convert.ToInt32(Pages.ANGLE))
			notebook_main.CurrentPage ++;
		else
			return;

		managePage(notebook_main.CurrentPage);
	}
	private void on_button_back_clicked (object o, EventArgs args)
	{
		if(notebook_main.CurrentPage == Convert.ToInt32(Pages.ANGLE) && radio_force_sensor.Active)
			notebook_main.CurrentPage = Convert.ToInt32(Pages.FORCE);
		else if(notebook_main.CurrentPage > Convert.ToInt32(Pages.FORCE))
			notebook_main.CurrentPage --;
		else
			return;

		managePage(notebook_main.CurrentPage);
	}

	private void on_radio_force_toggled (object o, EventArgs args)
	{
		managePage(Pages.FORCE);
	}
	private void on_radio_fixation_toggled (object o, EventArgs args)
	{
		managePage(Pages.FIXATION);
	}
	private void on_radio_mass_toggled (object o, EventArgs args)
	{
		managePage(Pages.MASS);
	}

	private void on_entries_changed (object o, EventArgs args)
	{
		Gtk.Entry entry_name = o as Gtk.Entry;
		if (o == null)
			return;

		entry_name.Text = Util.MakeValidSQL(entry_name.Text);
	}

	void on_button_cancel_clicked (object o, EventArgs args)
	{
		ForceSensorExerciseWindowBox.force_sensor_exercise.Hide();
		ForceSensorExerciseWindowBox = null;
	}

	private void on_delete_event (object o, DeleteEventArgs args)
	{
		LogB.Information("calling on_delete_event");

		args.RetVal = true;

		ForceSensorExerciseWindowBox.force_sensor_exercise.Hide();
		ForceSensorExerciseWindowBox = null;
	}


	~ForceSensorExerciseWindow() {}
}

