using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace Inertia
{
    public static class Program
    {
        public static Inertia.Settings Settings;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Set our working directory to the location of the binary
                Environment.CurrentDirectory = Application.StartupPath;

                // Read our settings
                Settings = Inertia.Settings.Read();

                // First things first, hide that mouse!
                if (Settings.Windowed == false)
                {
                    Cursor.Hide();
                    Cursor.Position = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width/2, Screen.PrimaryScreen.Bounds.Height-1);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                InertiaForm form = new InertiaForm();
                Application.Run(form);
            }
            catch (System.Exception ex)
            {
                // Restore the mouse cursor
                Cursor.Show();

                // Display the error
                string str = String.Empty;
                str += ex.Message + Environment.NewLine;
                str += Environment.NewLine;
                str += ex.StackTrace;
                System.Diagnostics.Debug.WriteLine(str);
                MessageBox.Show(str, ex.GetType().ToString());
            }

            // Launch explorer if it's not already running
            if (!ExplorerRunning())
            {
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.Start();
            }
        }

        public static bool ExplorerRunning()
        {
            // Is explorer running?
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (process.ProcessName.CompareTo("explorer") == 0)
                    {
                        return true;
                    }
                }
            }
            catch (System.Exception)
            {
                // do nothing
            }

            return false;
        }
    }
}
