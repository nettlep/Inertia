using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.DirectX;

namespace Inertia
{
    public class AnimationDefinition
    {
        public enum ETimeAdjust
        {
            Linear,
            Accelerated,
            Decelerated
        }

        public class Point
        {
            string m_X = string.Empty;
            [XmlAttribute("X")]
            public string X
	        {
		        get { return m_X; }
		        set { m_X = value; }
	        }

            string m_Y = string.Empty;
            [XmlAttribute("Y")]
            public string Y
	        {
		        get { return m_Y; }
		        set { m_Y = value; }
	        }

            string m_Z = string.Empty;
            [XmlAttribute("Z")]
            public string Z
	        {
		        get { return m_Z; }
		        set { m_Z = value; }
	        }
        }

        public class MotionPointSet
        {
            ETimeAdjust m_TimeAdjust = ETimeAdjust.Decelerated;
            [XmlAttribute("TimeAdjust")]
            public Inertia.AnimationDefinition.ETimeAdjust TimeAdjust
            {
                get { return m_TimeAdjust; }
                set { m_TimeAdjust = value; }
            }

            int m_DurationMS = 1000;
            [XmlAttribute("DurationMS")]
            public int DurationMS
            {
                get { return m_DurationMS; }
                set { m_DurationMS = value; }
            }

            Point m_Position0 = new Point();
            [XmlElement("Position0")]
            public Inertia.AnimationDefinition.Point Position0
	        {
		        get { return m_Position0; }
		        set { m_Position0 = value; }
	        }

            Point m_Position1 = new Point();
            [XmlElement("Position1")]
            public Inertia.AnimationDefinition.Point Position1
	        {
		        get { return m_Position1; }
		        set { m_Position1 = value; }
	        }

            Point m_Position2 = new Point();
            [XmlElement("Position2")]
            public Inertia.AnimationDefinition.Point Position2
	        {
		        get { return m_Position2; }
		        set { m_Position2 = value; }
	        }

            Point m_Position3 = new Point();
            [XmlElement("Position3")]
            public Inertia.AnimationDefinition.Point Position3
	        {
		        get { return m_Position3; }
		        set { m_Position3 = value; }
	        }

            Point m_Rotation0 = new Point();
            [XmlElement("Rotation0")]
            public Inertia.AnimationDefinition.Point Rotation0
            {
                get { return m_Rotation0; }
                set { m_Rotation0 = value; }
            }

            Point m_Rotation1 = new Point();
            [XmlElement("Rotation1")]
            public Inertia.AnimationDefinition.Point Rotation1
            {
                get { return m_Rotation1; }
                set { m_Rotation1 = value; }
            }

            Point m_Rotation2 = new Point();
            [XmlElement("Rotation2")]
            public Inertia.AnimationDefinition.Point Rotation2
            {
                get { return m_Rotation2; }
                set { m_Rotation2 = value; }
            }

            Point m_Rotation3 = new Point();
            [XmlElement("Rotation3")]
            public Inertia.AnimationDefinition.Point Rotation3
            {
                get { return m_Rotation3; }
                set { m_Rotation3 = value; }
            }

            Point m_Scale0 = new Point();
            [XmlElement("Scale0")]
            public Inertia.AnimationDefinition.Point Scale0
            {
                get { return m_Scale0; }
                set { m_Scale0 = value; }
            }

            Point m_Scale1 = new Point();
            [XmlElement("Scale1")]
            public Inertia.AnimationDefinition.Point Scale1
            {
                get { return m_Scale1; }
                set { m_Scale1 = value; }
            }

            Point m_Scale2 = new Point();
            [XmlElement("Scale2")]
            public Inertia.AnimationDefinition.Point Scale2
            {
                get { return m_Scale2; }
                set { m_Scale2 = value; }
            }

            Point m_Scale3 = new Point();
            [XmlElement("Scale3")]
            public Inertia.AnimationDefinition.Point Scale3
            {
                get { return m_Scale3; }
                set { m_Scale3 = value; }
            }

            public static float ParseRandom(string str)
            {
                // Skip to the first parameter
                int idx = str.IndexOf('(');
                if (idx == -1) return 0;
                str = str.Substring(idx + 1);

                // First parameter
                idx = str.IndexOf(',');
                if (idx == -1) return 0;
                string parm0 = str.Substring(0, idx);
                str = str.Substring(idx + 1);

                // Second parameter
                idx = str.IndexOf(')');
                if (idx != -1)
                {
                    str = str.Substring(0, idx);
                }
                string parm1 = str;

                // Cleanup the parameters
                parm0 = parm0.Trim();
                parm1 = parm1.Trim();

                // Finally, get our floats!
                float min;
                if (!float.TryParse(parm0, out min)) return 0;

                float max;
                if (!float.TryParse(parm1, out max)) return 0;

                // Calculate the random value
                return (float)Animator.Rand.NextDouble() * (max - min) + min;
            }

            public static float ParseValue(float defaultValue, string str, float min, float max)
            {
                // Easier to parse this way...
                string cleanString = str.Trim().ToLower();

                // If it's asking for the current value, give them the default
                if (cleanString.Length == 0 || cleanString == "current")
                {
                    return defaultValue;
                }

                // If it's a random value, parse that
                if (cleanString.StartsWith("rand"))
                {
                    return ParseRandom(str);
                }

                if (cleanString.StartsWith("*"))
                {
                    // Parse the multiplier
                    float multiplier = 0;
                    if (!float.TryParse(cleanString.Substring(1), out multiplier)) return 0;

                    // Return the interpolated value
                    return (max - min) * multiplier + min;
                }

                // Just a numeric value
                float result = 0;
                if (float.TryParse(cleanString, out result)) return result;

                return 0;
            }

            public static void ParseCoordinates(float defaultValue, string c0, string c1, string c2, string c3, out float f0, out float f1, out float f2, out float f3)
            {
                // We allow linear interpolation, which means that the first and last values need to be
                // calculated first so that the inner values can be interpolated
                f0 = ParseValue(defaultValue, c0, 0, 0);
                f3 = ParseValue(defaultValue, c3, 0, 0);

                f1 = ParseValue(defaultValue, c1, f0, f3);
                f2 = ParseValue(defaultValue, c2, f0, f3);
            }

            public void ParsePositions(Vector3 defaultValue, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
            {
                ParseCoordinates(defaultValue.X, Position0.X, Position1.X, Position2.X, Position3.X, out p0.X, out p1.X, out p2.X, out p3.X);
                ParseCoordinates(defaultValue.Y, Position0.Y, Position1.Y, Position2.Y, Position3.Y, out p0.Y, out p1.Y, out p2.Y, out p3.Y);
                ParseCoordinates(defaultValue.Z, Position0.Z, Position1.Z, Position2.Z, Position3.Z, out p0.Z, out p1.Z, out p2.Z, out p3.Z);
            }

            public void ParseRotations(Vector3 defaultValue, out Vector3 r0, out Vector3 r1, out Vector3 r2, out Vector3 r3)
            {
                ParseCoordinates(defaultValue.X, Rotation0.X, Rotation1.X, Rotation2.X, Rotation3.X, out r0.X, out r1.X, out r2.X, out r3.X);
                ParseCoordinates(defaultValue.Y, Rotation0.Y, Rotation1.Y, Rotation2.Y, Rotation3.Y, out r0.Y, out r1.Y, out r2.Y, out r3.Y);
                ParseCoordinates(defaultValue.Z, Rotation0.Z, Rotation1.Z, Rotation2.Z, Rotation3.Z, out r0.Z, out r1.Z, out r2.Z, out r3.Z);
            }

            public void ParseScales(Vector3 defaultValue, out Vector3 s0, out Vector3 s1, out Vector3 s2, out Vector3 s3)
            {
                ParseCoordinates(defaultValue.X, Scale0.X, Scale1.X, Scale2.X, Scale3.X, out s0.X, out s1.X, out s2.X, out s3.X);
                ParseCoordinates(defaultValue.Y, Scale0.Y, Scale1.Y, Scale2.Y, Scale3.Y, out s0.Y, out s1.Y, out s2.Y, out s3.Y);
                ParseCoordinates(defaultValue.Z, Scale0.Z, Scale1.Z, Scale2.Z, Scale3.Z, out s0.Z, out s1.Z, out s2.Z, out s3.Z);
            }
        }

        public bool ContainsPanel(int panelID)
        {
            string[] panels = Panels.Split(new char[] { ',' });
            foreach (string panel in panels)
            {
                if (panel.Trim() == panelID.ToString()) return true;
            }
            return false;
        }

        public bool ContainsEntryEvent(string entryEvent)
        {
            string[] events = EntryEvents.Split(new char[] { ',' });
            foreach (string evt in events)
            {
                if (evt.Trim() == entryEvent.Trim()) return true;
            }
            return false;
        }

        public string GetExitEventHandler(string exitEvent)
        {
            string[] events = ExitEvents.Split(new char[] { ',' });
            foreach (string evt in events)
            {
                // Split up the event from the animation definition name
                int idx = evt.IndexOf('(');
                if (idx == -1) continue;

                // Split them apart & clean 'em up
                string eventName = evt.Substring(0, idx).ToLower().Trim();
                string adName = evt.Substring(idx + 1).ToLower().Trim(new char[] { '(', ')', ' ', '\t', '\v', '\f', '\r', '\n' });

                if (eventName == exitEvent.ToLower().Trim()) return adName;
            }
            return String.Empty;
        }

        public bool ContainsExitEvent(string exitEvent)
        {
            string[] events = ExitEvents.Split(new char[] { ',' });
            foreach (string evt in events)
            {
                // Split up the event from the animation definition name
                int idx = evt.IndexOf('(');
                if (idx == -1) continue;

                // Split them apart & clean 'em up
                string eventName = evt.Substring(0, idx).ToLower().Trim();

                // If we have a match, return true
                if (eventName == exitEvent.ToLower().Trim()) return true;
            }
            return false;
        }

        string m_EntryEvents = String.Empty;
        [XmlAttribute("EntryEvents")]
        public string EntryEvents
        {
            get { return m_EntryEvents; }
            set { m_EntryEvents = value; }
        }

        string m_ExitEvents = String.Empty;
        [XmlAttribute("ExitEvents")]
        public string ExitEvents
        {
            get { return m_ExitEvents; }
            set { m_ExitEvents = value; }
        }

        string m_OnExit = String.Empty;
        [XmlAttribute("OnExit")]
        public string OnExit
        {
            get { return m_OnExit; }
            set { m_OnExit = value; }
        }

        string m_Name = String.Empty;
        [XmlAttribute("Name")]
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        string m_Panels = String.Empty;
        [XmlAttribute("Panels")]
        public string Panels
        {
            get { return m_Panels; }
            set { m_Panels = value; }
        }

        string m_NextAnimation = String.Empty;
        [XmlAttribute("NextAnimation")]
        public string NextAnimation
        {
            get { return m_NextAnimation; }
            set { m_NextAnimation = value; }
        }

        List<Inertia.AnimationDefinition.MotionPointSet> m_MotionPoints = new List<Inertia.AnimationDefinition.MotionPointSet>();
        [XmlElement("MotionPoints")]
        public List<Inertia.AnimationDefinition.MotionPointSet> MotionPoints
	    {
		    get { return m_MotionPoints; }
    		set { m_MotionPoints = value; }
	    }
    }
}
