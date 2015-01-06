using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Xml;

namespace Inertia
{
    public class Game : IComparable
    {
        List<Panel> m_Panels = new List<Panel>();
        public List<Panel> Panels
        {
            get { return m_Panels; }
            set { m_Panels = value; }
        }

        String m_BaseName;
        public System.String BaseName
        {
            get { return m_BaseName; }
            set { m_BaseName = value; }
        }

        String m_Description;
        public System.String Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        String m_Year;
        public System.String Year
        {
            get { return m_Year; }
            set { m_Year = value; }
        }

        String m_Manufacturer;
        public System.String Manufacturer
        {
            get { return m_Manufacturer; }
            set { m_Manufacturer = value; }
        }

        String m_IsBIOS;
        public System.String IsBIOS
        {
            get { return m_IsBIOS; }
            set { m_IsBIOS = value; }
        }

        String m_DriverStatus;
        public System.String DriverStatus
        {
            get { return m_DriverStatus; }
            set { m_DriverStatus = value; }
        }

        bool m_Loaded = false;
        public bool Loaded
        {
            get { return m_Loaded; }
            set { m_Loaded = value; }
        }

        public int CompareTo(Object o)
        {
            return Description.CompareTo(((Game)o).Description);
        }

        public static List<Game> LoadGames(XmlDocument xml)
        {
            // Our ROM list
            List<Game> result = new List<Game>();

            // Load all the ROMs
            DirectoryInfo dir = new DirectoryInfo("roms");
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                // Give the application some time
                Application.DoEvents();

                Game game = new Game();

                // Remove the extension
                game.BaseName = Path.GetFileNameWithoutExtension(file.Name);

                // Must be a zip file
                if (Path.GetExtension(file.FullName).ToUpper().CompareTo(".ZIP") != 0)
                {
                    continue;
                }

                foreach (XmlNode gameNode in xml.DocumentElement.ChildNodes)
                {
                    // Must be a GAME node
                    if (gameNode.Name != "game") continue;

                    // Get the game attributes
                    string gameName = string.Empty;
                    string isBIOS = string.Empty;
                    foreach (XmlAttribute gameAttr in gameNode.Attributes)
                    {
                        // Collect the attributes
                        switch (gameAttr.Name)
                        {
                            case "name":
                                gameName = gameAttr.Value;
                                break;

                            case "isbios":
                                isBIOS = gameAttr.Value;
                                break;
                        }
                    }

                    // If this isn't our ROM, skip it
                    if (gameName != game.BaseName) continue;

                    // Collect the information
                    game.IsBIOS = isBIOS;

                    // Scan through the game properties
                    foreach (XmlNode gameProperty in gameNode.ChildNodes)
                    {
                        if (gameProperty.Name == "description")
                        {
                            game.Description = gameProperty.InnerText;
                        }
                        else if (gameProperty.Name == "year")
                        {
                            game.Year = gameProperty.InnerText;
                        }
                        else if (gameProperty.Name == "manufacturer")
                        {
                            game.Manufacturer = gameProperty.InnerText;
                        }
                        else if (gameProperty.Name == "driver")
                        {
                            // Collect driver attributes
                            string driverStatus = string.Empty;
                            foreach (XmlAttribute driverAttr in gameProperty.Attributes)
                            {
                                // Collect the attributes
                                switch (driverAttr.Name)
                                {
                                    case "status":
                                        driverStatus = driverAttr.Value;
                                        break;
                                }
                            }
                            game.DriverStatus = driverStatus;
                        }
                    }
                }

                // Skip BIOS ROMs
                if (game.IsBIOS == "yes") continue;

                // Skip empty entries
                if (game.Description == null) continue;

                // Cleanup the description by removing any bracketed stuff at the end
                int idx = game.Description.IndexOfAny(new char[] { '[', '(', '{' });
                if (idx != -1)
                {
                    game.Description = game.Description.Substring(0, idx);
                    game.Description.Trim();
                }

                // Add the ROMinfo to the list
                result.Add(game);
            }

            result.Sort();

            return result;
        }

        public bool LoadTextures(TextureManager tm)
        {
            // If we're already loaded, bail
            if (!Loaded)
            {
                try
                {
                    foreach(string folder in Program.Settings.PanelFolders)
                    {
                        Panel panel = new Panel();
                        string basePath = Path.Combine(folder, BaseName);
                        string txtFile = basePath + ".txt";

                        // Is there a text file?
                        if (File.Exists(txtFile))
                        {
                            using (TextReader tr = new StreamReader(txtFile))
                            {
                                panel.InfoText = tr.ReadToEnd();
                                tr.Close();
                            }
                        }
                        else
                        {
                            panel.TextureID = tm.LoadFromAnyFormat(basePath, false);
                        }

                        if (folder.ToLower() == "snap")
                        {
                            panel.IsSnapPanel = true;

                            string aviFile = basePath + ".avi";

                            // Is there a video file?
                            if (File.Exists(aviFile))
                            {
                                panel.VideoFile = aviFile;
                            }
                        }

                        if (panel.IsTextPanel() || panel.TextureID != -1)
                        {
                            Panels.Add(panel);
                        }
                    }

                    Loaded = true;
                }
                catch (System.Exception)
                {
                    // Reset the panels - we'll try again later
                    Panels.Clear();
                }
            }

            return Loaded;
        }
    }
}
