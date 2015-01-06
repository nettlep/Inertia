using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inertia
{
    public class Panel
    {
        int m_TextureID = -1;
        public int TextureID
        {
            get { return m_TextureID; }
            set { m_TextureID = value; }
        }

        String m_InfoText = string.Empty;
        public System.String InfoText
        {
            get { return m_InfoText; }
            set { m_InfoText = value; }
        }

        String m_VideoFile = string.Empty;
        public System.String VideoFile
        {
            get { return m_VideoFile; }
            set { m_VideoFile = value; }
        }

        int m_ScrollOffset = 0;
        public int ScrollOffset
        {
            get { return m_ScrollOffset; }
            set { m_ScrollOffset = value; }
        }

        int m_MaxScroll = 0;
        public int MaxScroll
        {
            get { return m_MaxScroll; }
            set { m_MaxScroll = value; }
        }

        bool m_IsSnapPanel = false;
        public bool IsSnapPanel
        {
            get { return m_IsSnapPanel; }
            set { m_IsSnapPanel = value; }
        }

        DateTime ?m_SelectionTime = null;
        public System.DateTime ?SelectionTime
        {
            get { return m_SelectionTime; }
            set { m_SelectionTime = value; }
        }

        public bool IsTextPanel()
        {
            return m_InfoText.Length != 0;
        }
    }
}
