namespace Odin.UI.Forms // Ensure namespace matches
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            // Dispose custom resources added in MainForm.cs if needed
            // (e.g., if fonts were created there and not static)
             if (disposing)
             {
                 // Dispose services passed via constructor ONLY if MainForm is responsible for them
                 // (Typically handled by DI container or Program.cs on application exit)
                 // gammaService?.Dispose(); // Example - uncomment if needed & applicable
                 // dimmerService?.Dispose();
                 // reminderService?.Dispose();

                 // Dispose TrayIcon if it exists
                 trayIcon?.Dispose();
                 trayMenu?.Dispose();

                 // Dispose Fonts if created dynamically per instance (unlikely here)
                 // MonoFont?.Dispose();
                 // AsciiFont?.Dispose();
             }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // This method is called by the constructor in MainForm.cs BEFORE CreateControls()
            // It typically sets up form-level properties that were set in the designer.
            // Since we are doing most setup in MainForm.cs's constructor and CreateControls,
            // this method is minimal here but necessary for the WinForms structure.

            this.components = new System.ComponentModel.Container(); // Required for components like NotifyIcon/Timer if added via designer
            this.SuspendLayout();
            //
            // MainForm Properties (already set in MainForm.cs InitializeComponent, but designer adds them here too)
            // Redundant lines can be removed if desired, but harmless.
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 480); // Match size set in MainForm.cs
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Odin Control Interface"; // Match text set in MainForm.cs
            // this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon"))); // Example if icon added via designer resources

            this.ResumeLayout(false);
            // PerformLayout() is called in MainForm.cs InitializeComponent after this

        }

        #endregion

        // Declare private fields for all controls created in MainForm.cs
        // These MUST match the variable names used in MainForm.cs
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.CheckBox nightLightCheckBox;
        private System.Windows.Forms.TrackBar colorTemperatureTrackBar;
        private System.Windows.Forms.CheckBox dimmerCheckBox;
        private System.Windows.Forms.TrackBar dimmerTrackBar;
        private System.Windows.Forms.CheckBox breakReminderCheckBox;
        private System.Windows.Forms.NumericUpDown breakIntervalNumeric;
        private System.Windows.Forms.Button minimizeButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label asciiArtLabel;

        // We don't declare the services (gammaService etc.) here,
        // as they are passed via the constructor in MainForm.cs

    }
}