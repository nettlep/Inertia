using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Inertia
{
    public partial class LoadingForm : Form
    {
        DateTime m_StartTime = DateTime.Now;
        Bitmap m_BackgroundBitmap;
        static System.Threading.Thread m_LoadingScreenThread;
        static bool m_Loaded = false;

        public LoadingForm()
        {
            // First things first, hide that mouse!
            if (Program.Settings.Windowed == false)
            {
                Cursor.Hide();
            }

            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            if (Program.Settings.Windowed == false)
            {
                Top = 0;
                Left = 0;
                Width = Screen.PrimaryScreen.Bounds.Width;
                Height = Screen.PrimaryScreen.Bounds.Height;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Fixed3D;
                Top = (Screen.PrimaryScreen.Bounds.Height - Height) / 2;
                Left = (Screen.PrimaryScreen.Bounds.Width - Width) / 2;
            }

            m_BackgroundBitmap = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
        }

        private void LoadingForm_Paint(object sender, PaintEventArgs e)
        {
            TimeSpan diff = DateTime.Now - m_StartTime;
            double theta = diff.TotalMilliseconds / 5;

            using (Graphics bitmapGraphics = Graphics.FromImage(m_BackgroundBitmap))
            {
                Rectangle pacmanRect = ClientRectangle;
                pacmanRect.X = pacmanRect.Width / 2 - 15;
                pacmanRect.Y = pacmanRect.Height / 2 - 15;
                pacmanRect.Width = 30;
                pacmanRect.Height = 30;

                bitmapGraphics.FillRectangle(Brushes.Black, ClientRectangle);
                bitmapGraphics.FillPie(Brushes.Yellow, pacmanRect, (int)theta, 300);
            }

            e.Graphics.DrawImage(m_BackgroundBitmap, ClientRectangle);
        }

        public static void Start()
        {
            m_LoadingScreenThread = new System.Threading.Thread(LoadingScreenThread);
            m_LoadingScreenThread.Start();
        }

        public static void Stop()
        {
            // Shut down the loading screen thread
            m_Loaded = true;
        }

        private static void LoadingScreenThread()
        {
            // Show the form
            using (LoadingForm loadingForm = new LoadingForm())
            {
                loadingForm.Show();

                // Wait for it to be stopped
                while (m_Loaded == false)
                {
                    loadingForm.Invalidate();
                    Application.DoEvents();
                }

                // Close the form
                loadingForm.Close();
            }
        }

    }
}
