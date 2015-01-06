using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace Inertia
{
    public class Settings
    {
        public class Point
        {
            public Point()
            {
                X = 0;
                Y = 0;
                Z = 0;
            }

            public Point(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public Microsoft.DirectX.Vector3 ToVector3()
            {
                return new Microsoft.DirectX.Vector3(X, Y, Z);
            }

            [XmlAttribute("X")]
            public float X = 0;

            [XmlAttribute("Y")]
            public float Y = 0;

            [XmlAttribute("Z")]
            public float Z = 0;
        }

        public class Size
        {
            public Size()
            {
                Width = 0;
                Height = 0;
            }

            public Size(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public System.Drawing.Size ToSize()
            {
                return new System.Drawing.Size(Width, Height);
            }

            [XmlAttribute("Width")]
            public int Width = 0;

            [XmlAttribute("Height")]
            public int Height = 0;
        }

        public class SizeF
        {
            public SizeF()
            {
                Width = 0;
                Height = 0;
            }

            public SizeF(float width, float height)
            {
                Width = width;
                Height = height;
            }

            public System.Drawing.SizeF ToSizeF()
            {
                return new System.Drawing.SizeF(Width, Height);
            }

            [XmlAttribute("Width")]
            public float Width = 0;

            [XmlAttribute("Height")]
            public float Height = 0;
        }

        public class Color
        {
            public Color()
            {
                A = 0;
                R = 0;
                G = 0;
                B = 0;
            }

            public Color(int a, int r, int g, int b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
            }

            public System.Drawing.Color ToColor()
            {
                return System.Drawing.Color.FromArgb(A, R, G, B);
            }

            [XmlAttribute("A")]
            public int A = 0;

            [XmlAttribute("R")]
            public int R = 0;

            [XmlAttribute("G")]
            public int G = 0;

            [XmlAttribute("B")]
            public int B = 0;
        }

        public class Font
        {
            public Font()
            {
            }

            public Font(string familyName, float size, System.Drawing.FontStyle style)
            {
                FamilyName = familyName;
                Size = size;
                Style = style;
            }

            public System.Drawing.Font ToFont()
            {
                return new System.Drawing.Font(FamilyName, Size, Style);
            }

            [XmlAttribute("FamilyName")]
            public string FamilyName = "Arial";

            [XmlAttribute("Size")]
            public float Size = 10;

            [XmlAttribute("Style")]
            public System.Drawing.FontStyle Style = System.Drawing.FontStyle.Bold;
        }

        public Settings()
        {
#if false
            PanelFolders.Add("snap");
            PanelFolders.Add("cabinets");
            PanelFolders.Add("cpanel");
            PanelFolders.Add("scoring");
            PanelFolders.Add("commands");
            Write(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "settings.xml"));
#endif
        }

        public static Settings Read()
        {
            Settings result;

            string filename = String.Empty;
            if (Environment.MachineName.Length != 0)
            {
                string machineFilename = Environment.MachineName + ".InertiaConfig.xml";
                if (File.Exists(machineFilename))
                {
                    filename = machineFilename;
                }
            }
            if (filename.Length == 0)
            {
                filename = "InertiaConfig.xml";
            }

            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            TextReader textReader = new StreamReader(filename);
            result = (Settings)serializer.Deserialize(textReader);
            textReader.Close();

            return result;
        }

        public void Write(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            TextWriter textWriter = new StreamWriter(filename);
            serializer.Serialize(textWriter, this);
            textWriter.Close();
        }

        bool m_Windowed = false;
	    public bool Windowed
	    {
		    get { return m_Windowed; }
		    set { m_Windowed = value; }
	    }

        Settings.Size m_Resolution = new Settings.Size(0,0);
        public Settings.Size Resolution
        {
            get { return m_Resolution; }
            set { m_Resolution = value; }
        }

        int m_ScreenRotationDegrees = 0;
        public int ScreenRotationDegrees
        {
            get { return m_ScreenRotationDegrees; }
            set { m_ScreenRotationDegrees = value; }
        }

        float m_MonitorAspect = 0.75f;
        public float MonitorAspect
        {
            get { return m_MonitorAspect; }
            set { m_MonitorAspect = value; }
        }

        Settings.Color m_BackgroundColor = new Settings.Color(255, 0, 0, 0);
        public Settings.Color BackgroundColor
        {
            get { return m_BackgroundColor; }
            set { m_BackgroundColor = value; }
        }

        Settings.Font m_TitleFont = new Settings.Font("Arial Narrow", 18, System.Drawing.FontStyle.Bold);
        public Settings.Font TitleFont
        {
            get { return m_TitleFont; }
            set { m_TitleFont = value; }
        }

        Settings.Color m_TitleFontColor = new Settings.Color(255, 160, 160, 160);
        public Settings.Color TitleFontColor
        {
            get { return m_TitleFontColor; }
            set { m_TitleFontColor = value; }
        }

        Settings.Font m_GameInfoFont = new Settings.Font("Lucida Console", 16, System.Drawing.FontStyle.Regular);
        public Settings.Font GameInfoFont
        {
            get { return m_GameInfoFont; }
            set { m_GameInfoFont = value; }
        }

        Settings.Color m_GameInfoFontColor = new Settings.Color(255, 50, 205, 50);
        public Settings.Color GameInfoFontColor
        {
            get { return m_GameInfoFontColor; }
            set { m_GameInfoFontColor = value; }
        }

        Settings.Color m_GameInfoBackgroundColor = new Settings.Color(255, 32, 42, 32);
        public Settings.Color GameInfoBackgroundColor
        {
            get { return m_GameInfoBackgroundColor; }
            set { m_GameInfoBackgroundColor = value; }
        }

        Inertia.Settings.Point m_CameraPosition = new Inertia.Settings.Point(0, 255, -300);
        public Inertia.Settings.Point CameraPosition
        {
            get { return m_CameraPosition; }
            set { m_CameraPosition = value; }
        }

        Inertia.Settings.Point m_CameraTarget = new Inertia.Settings.Point(0, 135, 0);
        public Inertia.Settings.Point CameraTarget
        {
            get { return m_CameraTarget; }
            set { m_CameraTarget = value; }
        }

        List<string> m_PanelFolders = new List<string>();
        public List<string> PanelFolders
        {
            get { return m_PanelFolders; }
            set { m_PanelFolders = value; }
        }

        Settings.Color m_PanelBorderColor = new Settings.Color(255, 32, 32, 32);
        public Settings.Color PanelBorderColor
        {
            get { return m_PanelBorderColor; }
            set { m_PanelBorderColor = value; }
        }

        float m_PanelTopHeight = 2;
        public float PanelTopHeight
        {
            get { return m_PanelTopHeight; }
            set { m_PanelTopHeight = value; }
        }

        int m_PanelBorderSize = 3;
        public int PanelBorderSize
        {
            get { return m_PanelBorderSize; }
            set { m_PanelBorderSize = value; }
        }

        int m_ImageMargin = 10;
        public int ImageMargin
        {
            get { return m_ImageMargin; }
            set { m_ImageMargin = value; }
        }

        Settings.Color m_ImageBorderColor = new Settings.Color(255, 32, 32, 32);
        public Settings.Color ImageBorderColor
        {
            get { return m_ImageBorderColor; }
            set { m_ImageBorderColor = value; }
        }

        float m_ImageBorderThickness = 2;
        public float ImageBorderThickness
        {
            get { return m_ImageBorderThickness; }
            set { m_ImageBorderThickness = value; }
        }

        Settings.SizeF m_PanelMinSize = new Settings.SizeF(300, 195);
        public Settings.SizeF PanelMinSize
        {
            get { return m_PanelMinSize; }
            set { m_PanelMinSize = value; }
        }

        Settings.SizeF m_PanelMaxSize = new Settings.SizeF(300, 195);
        public Settings.SizeF PanelMaxSize
        {
            get { return m_PanelMaxSize; }
            set { m_PanelMaxSize = value; }
        }

        float m_DepthFogDensity = 0.003f;
        public float DepthFogDensity
        {
            get { return m_DepthFogDensity; }
            set { m_DepthFogDensity = value; }
        }

        float m_PlaneReflectionFogDensity = 0.06f;
        public float PlaneReflectionFogDensity
        {
            get { return m_PlaneReflectionFogDensity; }
            set { m_PlaneReflectionFogDensity = value; }
        }

        float m_PlaneReflectionPower = 0.6f;
        public float PlaneReflectionPower
        {
            get { return m_PlaneReflectionPower; }
            set { m_PlaneReflectionPower = value; }
        }

        float m_ReflectionMapAlpha = 0.8f;
        public float ReflectionMapAlpha
        {
            get { return m_ReflectionMapAlpha; }
            set { m_ReflectionMapAlpha = value; }
        }

        string m_MameXmlFile = "mame.xml";
        public string MameXmlFile
        {
            get { return m_MameXmlFile; }
            set { m_MameXmlFile = value; }
        }

        string m_MAMEParms = "-rotate";
        public string MAMEParms
        {
            get { return m_MAMEParms; }
            set { m_MAMEParms = value; }
        }

        bool m_EnableVideoWriteOnCoin = false;
        public bool EnableVideoWriteOnCoin
        {
            get { return m_EnableVideoWriteOnCoin; }
            set { m_EnableVideoWriteOnCoin = value; }
        }

        int m_VideoDemoDelay = 10;
        public int VideoDemoDelay
        {
            get { return m_VideoDemoDelay; }
            set { m_VideoDemoDelay = value; }
        }

        Settings.Size m_TextPanelResolution = new Settings.Size(512, 512);
        public Settings.Size TextPanelResolution
        {
            get { return m_TextPanelResolution; }
            set { m_TextPanelResolution = value; }
        }
    }
}
