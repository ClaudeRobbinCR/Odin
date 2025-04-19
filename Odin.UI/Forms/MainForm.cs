using Odin.UI;
using Odin.Services; // Ensure using statements match your project names
using Odin.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics.CodeAnalysis; // For SuppressMessage

namespace Odin.UI.Forms // Ensure this namespace matches
{
    public partial class MainForm : Form
    {
        // Services and Presenter (injected as before)
        private MainPresenter? presenter; // Made nullable as it's set after constructor
        private readonly GammaService gammaService;
        private readonly DimmerService dimmerService;
        // private readonly ReminderService reminderService; // Removed as per user's comment

        // --- Style Constants ---
        private readonly Color DarkBackground = Color.FromArgb(25, 25, 30);
        private readonly Color LightForeground = Color.FromArgb(200, 220, 255);
        private readonly Color AccentColor = Color.FromArgb(0, 180, 255); // A cyan/blue accent
        private readonly Font MonoFont = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font AsciiFont = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point);

        // --- ASCII Art --- (Adjust as desired)
        // Simple Eye:
        // private const string AsciiArt = @"( o . o )";

        // More elaborate example (ensure your label size accommodates it):
        private const string AsciiArt = @"
  .--.          .--.
 ( (`\\.'      './´) )
  '.    `--'--´    .'
    /   .--__--.   \
   |  /    /\    \  |
   \ |    /  \    | /
    \\'.--/    \--.'//
     `'.____'`'____.´'
        `--´  `--´ ";


        // UI Elements
        // These are declared in MainForm.Designer.cs


        // Events for Presenter (same as before)
        // Fix CS8618: Initialize events to null!
        public event EventHandler<bool> NightLightToggled = null!;
        public event EventHandler<bool> DimmerToggled = null!;
        public event EventHandler<bool> BreakReminderToggled = null!;
        public event EventHandler<float> ColorTemperatureChanged = null!;
        public event EventHandler<float> DimLevelChanged = null!;
        public event EventHandler<int> BreakIntervalChanged = null!;
        public event EventHandler<CancelEventArgs> ViewClosing = null!;
        public event EventHandler ViewLoaded = null!;

        // Properties for Presenter access (same as before)
        public bool IsNightLightOn => nightLightCheckBox.Checked;
        public bool IsDimmerOn => dimmerCheckBox.Checked;
        public bool IsBreakReminderOn => breakReminderCheckBox.Checked;

        // Fix WFO1000: Add DesignerSerializationVisibility attribute
        // Also add Browsable(false) to hide from property grid if desired
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public float ColorTemperature { get => colorTemperatureTrackBar.Value / 100f; set => SetTrackBarValue(colorTemperatureTrackBar, value); }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public float DimLevel { get => dimmerTrackBar.Value / 100f; set => SetTrackBarValue(dimmerTrackBar, value); }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int BreakIntervalMinutes { get => (int)breakIntervalNumeric.Value; set => SetNumericValue(breakIntervalNumeric, value); }

        // Helper methods to safely set control values
        // Implementations are in MainForm.Designer.cs


        // --- Add this method ---
        // Method for Program.cs to get the created NotifyIcon
        public NotifyIcon? GetTrayIcon()
        {
            // Ensure SetupTrayIcon() has been called before accessing trayIcon
            // It's called in the constructor, so should be safe unless constructor fails
            return this.trayIcon;
        }
        // --- End of added method ---


        // Constructor remains the same
        public MainForm(GammaService gammaSvc, DimmerService dimmerSvc /* Remove ReminderService from here if DI handles it fully in Program.cs, or keep if needed */)
        {
            // Assign injected services (ensure ReminderService isn't expected here if manually created in Program.cs)
             this.gammaService = gammaSvc ?? throw new ArgumentNullException(nameof(gammaSvc));
             this.dimmerService = dimmerSvc ?? throw new ArgumentNullException(nameof(dimmerSvc));
            // this.reminderService = reminderSvc; // Only if passed in

            InitializeComponent(); // Calls designer code (sets form properties)
            CreateControls();      // Actually create the controls
            SetupTrayIcon();       // Creates tray icon

            // Presenter initialization might happen in Program.cs now after full DI setup

            // Wire up UI events (now that controls exist)
            WireUpViewEvents(); // Encapsulate event wiring
        }

        // Method to set the presenter after MainForm is created
        public void SetPresenter(MainPresenter mainPresenter)
        {
            this.presenter = mainPresenter ?? throw new ArgumentNullException(nameof(mainPresenter));
        }

         private void WireUpViewEvents()
         {
            this.Load += (s, e) => ViewLoaded?.Invoke(this, EventArgs.Empty);
            this.FormClosing += (s, e) => ViewClosing?.Invoke(this, e);
            // ... other event wiring ...
            nightLightCheckBox.CheckedChanged += (s, e) => NightLightToggled?.Invoke(this, nightLightCheckBox.Checked);
            dimmerCheckBox.CheckedChanged += (s, e) => DimmerToggled?.Invoke(this, dimmerCheckBox.Checked);
            breakReminderCheckBox.CheckedChanged += (s, e) => BreakReminderToggled?.Invoke(this, breakReminderCheckBox.Checked);
            colorTemperatureTrackBar.Scroll += (s, e) => ColorTemperatureChanged?.Invoke(this, ColorTemperature);
            dimmerTrackBar.Scroll += (s, e) => DimLevelChanged?.Invoke(this, DimLevel);
            breakIntervalNumeric.ValueChanged += (s, e) => BreakIntervalChanged?.Invoke(this, BreakIntervalMinutes);
            minimizeButton.Click += (s, e) => { this.Hide(); if (trayIcon != null) trayIcon.Visible = true; };
         }


        // This method is typically generated by the WinForms designer.
        // Its implementation is in MainForm.Designer.cs
        // private void InitializeComponent(); // Removed declaration


        // CreateControls method (restored)
        private void CreateControls()
        {
            int currentY = 20;
            int labelWidth = 160; // Increased width for longer labels
            int controlX = labelWidth + 30;
            int controlWidth = this.ClientSize.Width - controlX - 30; // Make controls wider

            // --- ASCII Art Label ---
            asciiArtLabel = new Label
            {
                Text = AsciiArt,
                Font = AsciiFont,
                ForeColor = AccentColor, // Use accent color for art
                BackColor = Color.Transparent, // Inherit form background
                Location = new Point(15, currentY),
                Size = new Size(this.ClientSize.Width - 30, 120), // Adjust size as needed for your art
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(asciiArtLabel);
            currentY += asciiArtLabel.Height + 15;

            // --- Controls Styling Function ---
            Action<Control> styleControl = (ctrl) =>
            {
                ctrl.Font = MonoFont;
                ctrl.ForeColor = LightForeground;
                if (ctrl is CheckBox || ctrl is Label)
                {
                    ctrl.BackColor = Color.Transparent; // Make background transparent
                }
                else
                {
                    ctrl.BackColor = Color.FromArgb(40, 40, 50); // Slightly lighter dark for control background
                }

                 // Specific styling
                 if (ctrl is TrackBar tb)
                 {
                    // Basic trackbar styling is very limited in WinForms
                    tb.BackColor = DarkBackground; // Match form background
                 }
                 if (ctrl is Button btn)
                 {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = AccentColor;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.BackColor = Color.FromArgb(50, 50, 60);
                 }
                 if (ctrl is NumericUpDown nud)
                 {
                    nud.BorderStyle = BorderStyle.FixedSingle; // Simple border
                 }
            };

            // Night Light Toggle
            nightLightCheckBox = new CheckBox { Text = "Night Light Protocol", Location = new Point(20, currentY), AutoSize = true };
            styleControl(nightLightCheckBox);
            this.Controls.Add(nightLightCheckBox);
            currentY += 30;

            // Night Light Intensity
            var tempLabel = new Label { Text = "Color Temperature:", Location = new Point(20, currentY + 3), Width = labelWidth };
            styleControl(tempLabel);
            this.Controls.Add(tempLabel);
            colorTemperatureTrackBar = new TrackBar { Location = new Point(controlX, currentY), Width = controlWidth, Minimum = 0, Maximum = 100, Value = 70, TickFrequency = 10, TickStyle = TickStyle.BottomRight };
            styleControl(colorTemperatureTrackBar);
            this.Controls.Add(colorTemperatureTrackBar);
            currentY += 45;

            // Screen Dimmer Toggle
            dimmerCheckBox = new CheckBox { Text = "Stealth Dimmer Field", Location = new Point(20, currentY), AutoSize = true };
            styleControl(dimmerCheckBox);
            this.Controls.Add(dimmerCheckBox);
            currentY += 30;

            // Dimmer Intensity
            var dimLabel = new Label { Text = "Dim Factor:", Location = new Point(20, currentY + 3), Width = labelWidth };
            styleControl(dimLabel);
            this.Controls.Add(dimLabel);
            dimmerTrackBar = new TrackBar { Location = new Point(controlX, currentY), Width = controlWidth, Minimum = 0, Maximum = 95, Value = 30, TickFrequency = 10, TickStyle = TickStyle.BottomRight };
            styleControl(dimmerTrackBar);
            this.Controls.Add(dimmerTrackBar);
            currentY += 45;

            // Break Reminder Toggle
            breakReminderCheckBox = new CheckBox { Text = "Cognitive Break Cycle", Location = new Point(20, currentY), AutoSize = true };
            styleControl(breakReminderCheckBox);
            this.Controls.Add(breakReminderCheckBox);
            currentY += 30;

            // Break Interval
            var intervalLabel = new Label { Text = "Cycle Interval (min):", Location = new Point(20, currentY + 3), Width = labelWidth };
            styleControl(intervalLabel);
            this.Controls.Add(intervalLabel);
            breakIntervalNumeric = new NumericUpDown { Location = new Point(controlX, currentY), Width = 70, Minimum = 1, Maximum = 120, Value = 20, Increment = 5 };
            styleControl(breakIntervalNumeric);
            this.Controls.Add(breakIntervalNumeric);
            currentY += 40;

            // Status Label
            statusLabel = new Label { Text = "System Status: Nominal", Location = new Point(20, currentY), Width = this.ClientSize.Width - 40, ForeColor = Color.Gray }; // Subdued status text
            styleControl(statusLabel); // Apply base style
            statusLabel.BackColor = Color.Transparent; // Ensure transparent
            this.Controls.Add(statusLabel);
            currentY += 30;

            // Minimize Button
            minimizeButton = new Button { Text = "Minimize", Location = new Point(this.ClientSize.Width / 2 - 60, currentY), Size = new Size(120, 30) };
            styleControl(minimizeButton);
            this.Controls.Add(minimizeButton);
        }

        // SetupTrayIcon method (restored)
        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            // Style the context menu if desired (requires custom drawing or libraries)
            trayMenu.Items.Add("Show Interface", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); });
            trayMenu.Items.Add("Exit Odin", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon
            {
                // Use a custom icon! Add an .ico file to Odin.UI > Properties > Resources
                // Icon = global::Odin.UI.Properties.Resources.YourTrayIcon,
                Icon = SystemIcons.Application, // Placeholder
                Text = "Odin Eye Protector",
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); };
        }

        // Method for Presenter to show errors (restored)
        public void ShowError(string message)
        {
             if (this.InvokeRequired) { this.Invoke((MethodInvoker)delegate { ShowError(message); }); return; }
             // Use a themed message box if you integrate a library, otherwise standard MessageBox
             MessageBox.Show(this, message, "Interface Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Changed Icon
        }

        // Method for Presenter to update status (restored)
        public void UpdateStatus(string message)
        {
             if (statusLabel == null) return;
             if (statusLabel.InvokeRequired) { statusLabel.Invoke((MethodInvoker)delegate { UpdateStatus(message); }); return; }
             statusLabel.Text = $"Status: {message}";
        }

        // Override OnLoad (restored)
        protected override void OnLoad(EventArgs e)
        {
             base.OnLoad(e);
             // Optional: Start minimized to tray
             // this.WindowState = FormWindowState.Minimized;
             // this.Hide();
             // trayIcon.Visible = true;
        }

        // OnFormClosed override (restored)
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
             if (trayIcon != null)
             {
                 trayIcon.Visible = false;
                 trayIcon.Dispose();
             }
             base.OnFormClosed(e);
        }

        // OnFormClosing override (restored and corrected signature)
        protected override void OnFormClosing(FormClosingEventArgs e) // Corrected parameter type
        {
            // Example: Minimize on 'X' click
            // if (e.CloseReason == CloseReason.UserClosing) {
            //     e.Cancel = true;
            //     this.Hide();
            //     if (trayIcon != null) trayIcon.Visible = true;
            // } else {
                 ViewClosing?.Invoke(this, e); // Allow presenter to save etc.
            // }
             base.OnFormClosing(e);
        }

        // SetTrackBarValue helper (restored)
        private void SetTrackBarValue(TrackBar tb, float value) => tb.Value = Math.Clamp((int)(value * 100), tb.Minimum, tb.Maximum);

        // SetNumericValue helper (restored)
        private void SetNumericValue(NumericUpDown nud, int value) => nud.Value = Math.Clamp(value, (int)nud.Minimum, (int)nud.Maximum);
    }
}