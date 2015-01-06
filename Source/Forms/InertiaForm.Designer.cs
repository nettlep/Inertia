namespace Inertia
{
    partial class InertiaForm
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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InertiaForm));
            this.LoadingWorker = new System.ComponentModel.BackgroundWorker();
            this.LoadingScreenWorker = new System.ComponentModel.BackgroundWorker();
            this.VideoPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // LoadingWorker
            // 
            this.LoadingWorker.WorkerSupportsCancellation = true;
            this.LoadingWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.LoadingWorker_DoWork);
            // 
            // VideoPictureBox
            // 
            this.VideoPictureBox.Location = new System.Drawing.Point(12, 12);
            this.VideoPictureBox.Name = "VideoPictureBox";
            this.VideoPictureBox.Size = new System.Drawing.Size(100, 50);
            this.VideoPictureBox.TabIndex = 0;
            this.VideoPictureBox.TabStop = false;
            this.VideoPictureBox.Visible = false;
            // 
            // InertiaForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(501, 475);
            this.Controls.Add(this.VideoPictureBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "InertiaForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Inertia";
            this.Deactivate += new System.EventHandler(this.InertiaForm_Deactivate);
            this.Load += new System.EventHandler(this.InertiaForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.InertiaForm_Paint);
            this.Activated += new System.EventHandler(this.InertiaForm_Activated);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.InertiaForm_KeyUp);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InertiaForm_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InertiaForm_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.ComponentModel.BackgroundWorker LoadingWorker;
        private System.ComponentModel.BackgroundWorker LoadingScreenWorker;
        public System.Windows.Forms.PictureBox VideoPictureBox;
    }
}

