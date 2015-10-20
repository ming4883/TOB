//css_import Settings
using System;
using System.Windows.Forms;
using System.Drawing;

namespace TOB
{
	
	class UI
	{
		static GroupBox _gb;
		static Form _frm;
		public delegate void Action();
		public static Action OnStreamingStart;
		public static Action OnStreamingSync;
		public static Action OnStreamingStop;
		public static Action OnCaptureStart;
		
		static void InitLocal(Control root)
		{
			GroupBox gb = new GroupBox() {
				Dock = DockStyle.Top,
				Height = 60,
				Text = "Local",
				Padding = new Padding (4),
			};
			root.Controls.Add (gb);
			
			{
				Button bt = new Button() {
					Dock = DockStyle.Left,
					Text = "STOP",
					Width = 100,
				};
				bt.Click += (s, e) => {
					OnStreamingStop();
				};
				gb.Controls.Add (bt);
			}
			{
				Button bt = new Button() {
					Dock = DockStyle.Left,
					Text = "CAPTURE",
					Width = 100,
				};
				bt.Click += (s, e) => {
					OnCaptureStart();
				};
				gb.Controls.Add (bt);
			}
		}
		
		static void InitRemote(Control root)
		{
			GroupBox gb = new GroupBox() {
				Dock = DockStyle.Top,
				Height = 130,
				Text = "Remote",
				Padding = new Padding (4),
			};
			root.Controls.Add (gb);
			
			{
				Panel p = new Panel() {
					Dock = DockStyle.Top,
					Height = 35,
				};
				gb.Controls.Add (p);
			
				if(Settings.DevMode)
				{
					Button bt = new Button() {
						Dock = DockStyle.Left,
						Text = "SYNC",
						Width = 100,
					};
					bt.Click += (s, e) => {
						OnStreamingSync();
					};
					p.Controls.Add (bt);
				}
				{
					Button bt = new Button() {
						Dock = DockStyle.Left,
						Text = "STOP",
						Width = 100,
					};
					bt.Click += (s, e) => {
						OnStreamingStop();
					};
					p.Controls.Add (bt);
				}
				{
					Button bt = new Button() {
						Dock = DockStyle.Left,
						Text = "PLAY",
						Width = 100,
					};
					bt.Click += (s, e) => {
						OnStreamingStart();
					};
					p.Controls.Add (bt);
				}
			}
			
			#region QUALITY & AUDIO_SOURCE
			{
				Panel p = new Panel() {
					Dock = DockStyle.Top,
					Height = 35,
				};
				gb.Controls.Add (p);
				{
					ComboBox cb = new ComboBox() {
						Dock = DockStyle.Left,
						DropDownStyle = ComboBoxStyle.DropDownList,
						Width = 90,
					};
					p.Controls.Add (cb);
					cb.Items.Add("Network");
					cb.Items.Add("MIC-in");
					cb.SelectedIndex = Settings.AUDIO_SOURCE;
					cb.SelectionChangeCommitted += (s, e) =>
					{
						Settings.AUDIO_SOURCE = cb.SelectedIndex;
					};
				}
				{
					Label la = new Label() {
						Text = "Audio ",
						Dock = DockStyle.Left,
						TextAlign = ContentAlignment.MiddleLeft,
						AutoSize = true,
					};
					p.Controls.Add (la);
				}
				{
					NumericUpDown  ud = new NumericUpDown() {
						Dock = DockStyle.Left,
						Maximum = 100,
						Minimum = 1,
						Value = Settings.QUALITY,
						Width = 60,
					};
					p.Controls.Add (ud);
					ud.ValueChanged += (s, e) =>
					{
						Settings.QUALITY = (int)ud.Value;
					};
				}
				{
					Label la = new Label() {
						Text = "Quality ",
						Dock = DockStyle.Left,
						TextAlign = ContentAlignment.MiddleLeft,
						AutoSize = true,
					};
					p.Controls.Add (la);
				}
			}
			#endregion
			
			#region IP
			{
				Panel p = new Panel() {
					Dock = DockStyle.Top,
					Height = 35,
				};
				gb.Controls.Add (p);
				{
					NumericUpDown  ud = new NumericUpDown() {
						Dock = DockStyle.Left,
						Maximum = 255,
						Minimum = 0,
						Value = Settings.IP4,
						Width = 60,
					};
					p.Controls.Add (ud);
					ud.ValueChanged += (s, e) =>
					{
						Settings.IP4 = (int)ud.Value;
					};
					
				}
				{
					Label la = new Label() {
						Text = ".",
						Dock = DockStyle.Left,
						TextAlign = ContentAlignment.MiddleLeft,
						AutoSize = true,
					};
					p.Controls.Add (la);
				}
				{
					NumericUpDown  ud = new NumericUpDown() {
						Dock = DockStyle.Left,
						Maximum = 255,
						Minimum = 0,
						Value = Settings.IP3,
						Width = 60,
					};
					ud.ValueChanged += (s, e) =>
					{
						Settings.IP3 = (int)ud.Value;
					};
					p.Controls.Add (ud);
				}
				{
					Label la = new Label() {
						Text = ".",
						Dock = DockStyle.Left,
						TextAlign = ContentAlignment.MiddleLeft,
						AutoSize = true,
					};
					p.Controls.Add (la);
				}
				{
					NumericUpDown  ud = new NumericUpDown() {
						Dock = DockStyle.Left,
						Maximum = 255,
						Minimum = 0,
						Value = Settings.IP2,
						Width = 60,
					};
					p.Controls.Add (ud);
					ud.ValueChanged += (s, e) =>
					{
						Settings.IP2 = (int)ud.Value;
					};
				}
				{
					Label la = new Label() {
						Text = ".",
						Dock = DockStyle.Left,
						TextAlign = ContentAlignment.MiddleLeft,
						AutoSize = true,
					};
					p.Controls.Add (la);
				}
				{
					NumericUpDown  ud = new NumericUpDown() {
						Dock = DockStyle.Left,
						Maximum = 255,
						Minimum = 0,
						Value = Settings.IP1,
						Width = 60,
					};
					ud.ValueChanged += (s, e) =>
					{
						Settings.IP1 = (int)ud.Value;
					};
					p.Controls.Add (ud);
				}
				{
					Label la = new Label() {
						Text = "IP ",
						Dock = DockStyle.Left,
						TextAlign = ContentAlignment.MiddleLeft,
						AutoSize = true,
					};
					p.Controls.Add (la);
				}
			}
			#endregion
		}
		
		static void InitGeneral (Control root)
		{
			GroupBox gb = new GroupBox() {
				Dock = DockStyle.Top,
				Text = "Broadcast",
				Height = 60,
				Padding = new Padding (4),
			};
			root.Controls.Add (gb);
			_gb = gb;
			
			{
				CheckBox ck = new CheckBox() {
					Dock = DockStyle.Top,
					Text = "Fullscreen",
					Checked = Settings.FULLSCREEN,
					Height = 35,
					//CheckAlign = ContentAlignment.MiddleRight,
				};
				gb.Controls.Add (ck);
				ck.CheckedChanged += (s, e) =>
				{
					Settings.FULLSCREEN = ck.Checked;
				};
			}
		}
		
		static public void Init ()
		{
			Settings.FULLSCREEN = !Settings.DevMode;
			
			var frm = new Form() {
				Text = "Tob Broadcast",
				Size = new Size (480, 320),
				Padding = new Padding (2),
				Font = new Font ("Consolas", 10.0f),
				FormBorderStyle = FormBorderStyle.FixedToolWindow,
				MaximizeBox = false,
			};
			
			frm.SuspendLayout();
			
			if (Settings.DevMode)
				InitLocal (frm);
			
			InitRemote (frm);
			
			InitGeneral (frm);
			
			frm.ResumeLayout();
			_frm = frm;
			
			Application.Run (frm);
		}
		
		static public void SetStatus(string text)
		{
			var task = new MethodInvoker(()=>
			{
				if (string.IsNullOrWhiteSpace(text))
					_gb.Text = "Broadcast";
				else
					_gb.Text = "Broadcast - " + text;
			});
			
			if (_gb.InvokeRequired)
				_gb.Invoke(task);
			else
				task();
		}
		
		static public void Focus()
		{
			var task = new MethodInvoker(()=>
			{
				if (_frm.WindowState == FormWindowState.Minimized)
					_frm.WindowState = FormWindowState.Normal;
				
				_frm.Activate();
			});
			
			if (_frm.InvokeRequired)
				_frm.Invoke(task);
			else
				task();
		}
		
		static public void Warn(string msg)
		{
			MessageBox.Show(msg);
		}
		
		public class PlaybackForm : Form
		{
			const int DefaultW = 640;
			const int DefaultH = 480;
			bool _FullScreen = false;
			Label _Msg = null;
			Panel _Content = null;
			
			public PlaybackForm()
			{
				this.FormBorderStyle = FormBorderStyle.None;
				this.BackColor = Color.Black;
				this.KeyPreview = true;
				//this.ShowInTaskbar = false;
				
				this.SuspendLayout();
				
				{
					_Content = new Panel()
					{
						BackColor = Color.Black,
						Dock = DockStyle.Fill,
					};
					Controls.Add (_Content);
				}
				
				{
					_Msg = new Label()
					{
						BackColor = Color.White,
						ForeColor = Color.Black,
						Font = new Font ("Consolas", 12.0f),
						Text = "F11 - toggle Fullscreen; Esc - STOP", 
						Dock = DockStyle.Top,
					};
					Controls.Add (_Msg);
					_Msg.Visible = false;
				}
				
				this.ResumeLayout();
				
				this.FormClosing += (s, e) =>
				{
					e.Cancel = true;
				};
				this.KeyDown += (s, e) =>
				{
					if (e.KeyCode == Keys.F11)
					{
						_Msg.Visible = false;
						ToggleFullScreen();
					}
					else if(e.KeyCode == Keys.Escape)
					{
						_Msg.Visible = false;
						OnStreamingStop();
					}
					else
					{
						_Msg.Visible = true;
					}
				};
			}
			
			public IntPtr HWND
			{
				get
				{
					return _Content.Handle;
				}
			}
			
			void ToggleFullScreen()
			{
				if (_FullScreen)
				{
					int w = DefaultW;
					int h = DefaultH;
					Bounds = new Rectangle ((Screen.PrimaryScreen.Bounds.Width - w) / 2, (Screen.PrimaryScreen.Bounds.Height - h) / 2, w, h);
				}
				else
				{
					Bounds = new Rectangle (0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
				}
				
				_FullScreen = !_FullScreen;
			}
			
			public void ShowWithOptions(bool fullscreen)
			{
				int w = DefaultW;
				int h = DefaultH;
				
				if (fullscreen)
				{
					w = Screen.PrimaryScreen.Bounds.Width;
					h = Screen.PrimaryScreen.Bounds.Height;
				}
				
				Show();
				Bounds = new Rectangle ((Screen.PrimaryScreen.Bounds.Width - w) / 2, (Screen.PrimaryScreen.Bounds.Height - h) / 2, w, h);
				
				_FullScreen = fullscreen;
			}
		}
		
		static public PlaybackForm ShowPlaybackForm(bool fullscreen)
		{
			PlaybackForm frm = new PlaybackForm();
			frm.ShowWithOptions (fullscreen);
			return frm;
		}
	}
	

}	// namespace TOB
