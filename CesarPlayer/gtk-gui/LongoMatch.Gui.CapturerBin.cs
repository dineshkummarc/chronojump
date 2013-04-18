
// This file has been generated by the GUI designer. Do not modify.
namespace LongoMatch.Gui
{
	public partial class CapturerBin
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.HBox capturerhbox;

		private global::Gtk.DrawingArea logodrawingarea;

		private global::Gtk.HBox hbox2;

		private global::Gtk.HBox buttonsbox;

		private global::Gtk.Button recbutton;

		private global::Gtk.Button pausebutton;

		private global::Gtk.Button stopbutton;

		private global::Gtk.Button snapshotbutton;

		private global::Gtk.Label timelabel;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget LongoMatch.Gui.CapturerBin
			global::Stetic.BinContainer.Attach (this);
			this.Name = "LongoMatch.Gui.CapturerBin";
			// Container child LongoMatch.Gui.CapturerBin.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 0;
			// Container child vbox1.Gtk.Box+BoxChild
			this.capturerhbox = new global::Gtk.HBox ();
			this.capturerhbox.Name = "capturerhbox";
			this.capturerhbox.Spacing = 6;
			this.vbox1.Add (this.capturerhbox);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.capturerhbox]));
			w1.Position = 0;
			// Container child vbox1.Gtk.Box+BoxChild
			this.logodrawingarea = new global::Gtk.DrawingArea ();
			this.logodrawingarea.Name = "logodrawingarea";
			//this.vbox1.Add (this.logodrawingarea);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.logodrawingarea]));
			w2.Position = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox ();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.buttonsbox = new global::Gtk.HBox ();
			this.buttonsbox.Name = "buttonsbox";
			this.buttonsbox.Spacing = 6;
			// Container child buttonsbox.Gtk.Box+BoxChild
			this.recbutton = new global::Gtk.Button ();
			this.recbutton.TooltipMarkup = "Start or continue capture";
			this.recbutton.Name = "recbutton";
			this.recbutton.UseUnderline = true;
			// Container child recbutton.Gtk.Container+ContainerChild
			global::Gtk.Alignment w3 = new global::Gtk.Alignment (0.5f, 0.5f, 0f, 0f);
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			global::Gtk.HBox w4 = new global::Gtk.HBox ();
			w4.Spacing = 2;
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Image w5 = new global::Gtk.Image ();
			//w5.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-media-record", global::Gtk.IconSize.Dialog);
			w5.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-media-record", global::Gtk.IconSize.Button);
			w4.Add (w5);
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Label w7 = new global::Gtk.Label ();
			w4.Add (w7);
			w3.Add (w4);
			this.recbutton.Add (w3);
			this.buttonsbox.Add (this.recbutton);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.buttonsbox[this.recbutton]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			// Container child buttonsbox.Gtk.Box+BoxChild
			this.pausebutton = new global::Gtk.Button ();
			this.pausebutton.TooltipMarkup = "Pause capture";
			this.pausebutton.Name = "pausebutton";
			this.pausebutton.UseUnderline = true;
			// Container child pausebutton.Gtk.Container+ContainerChild
			global::Gtk.Alignment w12 = new global::Gtk.Alignment (0.5f, 0.5f, 0f, 0f);
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			global::Gtk.HBox w13 = new global::Gtk.HBox ();
			w13.Spacing = 2;
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Image w14 = new global::Gtk.Image ();
			w14.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-media-pause", global::Gtk.IconSize.Dialog);
			w13.Add (w14);
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Label w16 = new global::Gtk.Label ();
			w13.Add (w16);
			w12.Add (w13);
			this.pausebutton.Add (w12);
			this.buttonsbox.Add (this.pausebutton);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.buttonsbox[this.pausebutton]));
			w20.Position = 1;
			w20.Expand = false;
			w20.Fill = false;
			// Container child buttonsbox.Gtk.Box+BoxChild
			this.stopbutton = new global::Gtk.Button ();
			this.stopbutton.TooltipMarkup = "Stop and close capture";
			this.stopbutton.Name = "stopbutton";
			this.stopbutton.UseUnderline = true;
			// Container child stopbutton.Gtk.Container+ContainerChild
			global::Gtk.Alignment w21 = new global::Gtk.Alignment (0.5f, 0.5f, 0f, 0f);
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			global::Gtk.HBox w22 = new global::Gtk.HBox ();
			w22.Spacing = 2;
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Image w23 = new global::Gtk.Image ();
			//w23.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-media-stop", global::Gtk.IconSize.Dialog);
			w23.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-media-stop", global::Gtk.IconSize.Button);
			w22.Add (w23);
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Label w25 = new global::Gtk.Label ();
			w22.Add (w25);
			w21.Add (w22);
			this.stopbutton.Add (w21);
			this.buttonsbox.Add (this.stopbutton);
			global::Gtk.Box.BoxChild w29 = ((global::Gtk.Box.BoxChild)(this.buttonsbox[this.stopbutton]));
			w29.Position = 2;
			w29.Expand = false;
			w29.Fill = false;
			// Container child buttonsbox.Gtk.Box+BoxChild
			this.snapshotbutton = new global::Gtk.Button ();
			this.snapshotbutton.CanFocus = true;
			this.snapshotbutton.Name = "snapshotbutton";
			this.snapshotbutton.UseUnderline = true;
			// Container child snapshotbutton.Gtk.Container+ContainerChild
			global::Gtk.Alignment w30 = new global::Gtk.Alignment (0.5f, 0.5f, 0f, 0f);
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			global::Gtk.HBox w31 = new global::Gtk.HBox ();
			w31.Spacing = 2;
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Image w32 = new global::Gtk.Image ();
			w32.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "stock_about", global::Gtk.IconSize.Button);
			//w31.Add (w32);
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Label w34 = new global::Gtk.Label ();
			w34.LabelProp = global::Mono.Unix.Catalog.GetString ("Take _snaphot");
			w34.UseUnderline = true;
			w31.Add (w34);
			w30.Add (w31);
			this.snapshotbutton.Add (w30);
			this.buttonsbox.Add (this.snapshotbutton);
			global::Gtk.Box.BoxChild w38 = ((global::Gtk.Box.BoxChild)(this.buttonsbox[this.snapshotbutton]));
			w38.Position = 3;
			w38.Expand = false;
			w38.Fill = false;
			this.hbox2.Add (this.buttonsbox);
			global::Gtk.Box.BoxChild w39 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.buttonsbox]));
			w39.Position = 0;
			w39.Expand = false;
			w39.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.timelabel = new global::Gtk.Label ();
			this.timelabel.Name = "timelabel";
			this.timelabel.Xalign = 1f;
			this.timelabel.LabelProp = "Time: 0:00:00";
			this.hbox2.Add (this.timelabel);
			global::Gtk.Box.BoxChild w40 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.timelabel]));
			w40.PackType = ((global::Gtk.PackType)(1));
			w40.Position = 1;
			w40.Expand = false;
			this.vbox1.Add (this.hbox2);
			global::Gtk.Box.BoxChild w41 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hbox2]));
			w41.Position = 2;
			w41.Expand = false;
			w41.Fill = false;
			this.Add (this.vbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.pausebutton.Hide ();
			this.stopbutton.Hide ();
			this.Show ();
			this.logodrawingarea.ExposeEvent += new global::Gtk.ExposeEventHandler (this.OnLogodrawingareaExposeEvent);
			this.recbutton.Clicked += new global::System.EventHandler (this.OnRecbuttonClicked);
			this.pausebutton.Clicked += new global::System.EventHandler (this.OnPausebuttonClicked);
			this.stopbutton.Clicked += new global::System.EventHandler (this.OnStopbuttonClicked);
			this.snapshotbutton.Clicked += new global::System.EventHandler (this.OnSnapshotbuttonClicked);
		}
	}
}
