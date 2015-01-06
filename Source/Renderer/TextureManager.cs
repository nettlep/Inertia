using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Xml;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Inertia
{
    public class STexture
    {
        /// <summary>
        /// Direct3D Texture object
        /// </summary>
        Texture m_Texture;
        public Microsoft.DirectX.Direct3D.Texture Texture
        {
            get { return m_Texture; }
            set { m_Texture = value; }
        }

        /// <summary>
        /// Text, if any, currently rendered on the surface
        /// </summary>
        string m_Text = String.Empty;
        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        /// <summary>
        /// Is this used as a render target?
        /// </summary>
        bool m_RenderTarget = false;
        public bool RenderTarget
        {
            get { return m_RenderTarget; }
            set { m_RenderTarget = value; }
        }

        /// <summary>
        /// User-specified value that can be used to track available textures. For example,
        /// Render Targets will use this because they are constantly re-used.
        /// Use ClearInUse() to reset all InUse values.
        /// </summary>
        bool m_InUse = false;
        public bool InUse
        {
            get { return m_InUse; }
            set { m_InUse = value; }
        }

        /// <summary>
        /// Scroll offset (in pixels) into the InfoText
        /// </summary>
        int m_ScrollOffset = 0;
        public int ScrollOffset
        {
            get { return m_ScrollOffset; }
            set { m_ScrollOffset = value; }
        }

        /// <summary>
        /// Direct3D Surface object
        /// </summary>
        Surface m_Surface;
        public Microsoft.DirectX.Direct3D.Surface Surface
        {
            get { return m_Surface; }
            set { m_Surface = value; }
        }

        /// <summary>
        /// Direct3D Texture object
        /// </summary>
        CubeTexture m_CubeTexture;
        public Microsoft.DirectX.Direct3D.CubeTexture CubeTexture
        {
            get { return m_CubeTexture; }
            set { m_CubeTexture = value; }
        }

        /// <summary>
        /// Filename for the image to load into the texture
        /// </summary>
        string m_Filename;
        public string Filename
        {
            get { return m_Filename; }
            set { m_Filename = value; }
        }

        /// <summary>
        /// ID of the texture, use GetTexture to retrieve texture object
        /// </summary>
        int m_ID;
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }

        /// <summary>
        /// Texture dimensions
        /// </summary>
        Vector2 m_Size;
        public Vector2 Size
        {
            get { return m_Size; }
            set { m_Size = value; }
        }
    };

    public class TextureManager
    {
        // D3D Device
        Device m_D3DDevice = null;

        // Textures
        Dictionary<int, STexture> m_Textures = new Dictionary<int, STexture>();
        int m_nextTextureID = 100;

        public TextureManager(Device device)
        {
            m_D3DDevice = device;
        }

        /// <summary>
        /// Creates a texture into the 3D session from an image and returns the texture's ID
        /// </summary>
        /// <returns>Texture's ID</returns>
        public bool SetTexture(int id, Texture texture, Size size)
        {
            if (id == -1) return false;
            if (texture == null) return false;

            STexture stex = GetTexture(id);
            stex.Texture = texture;
            stex.Size = new Vector2(size.Width, size.Height);

            return true;
        }

        /// <summary>
        /// Creates a render-target texture
        /// </summary>
        /// <param name="size">Size to create</param>
        /// <returns>Render target's ID</returns>
        public int AddRenderTarget(Size size)
        {
            // We're going to add a new texture to the list
            STexture stex = new STexture();
            stex.Filename = "";

            // Assign it an ID
            stex.ID = m_nextTextureID++;

            // Create a texture from it
            stex.Texture = new Texture(m_D3DDevice, size.Width, size.Height, 1, Usage.RenderTarget, Manager.Adapters[0].CurrentDisplayMode.Format, Pool.Default);
            stex.Surface = stex.Texture.GetSurfaceLevel(0);
            stex.RenderTarget = true;

            // Texture info
            stex.Size = new Vector2(size.Width, size.Height);

            // Add it to the dictionary
            m_Textures.Add(stex.ID, stex);
            return stex.ID;
        }

        /// <summary>
        /// Creates a texture into the 3D session from an image and returns the texture's ID
        /// </summary>
        /// <param name="img">Image to create texture from</param>
        /// <returns>Texture's ID</returns>
        public int AddTexture(Image img)
        {
            if (img == null) return -1;

            // We're going to add a new texture to the list
            STexture stex = new STexture();
            stex.Filename = "";

            // Assign it an ID
            stex.ID = m_nextTextureID++;

            // Create a texture from it
            stex.Texture = Texture.FromBitmap(m_D3DDevice, (Bitmap)img, 0, Pool.Managed);

            // Texture info
            stex.Size = new Vector2(img.Size.Width, img.Size.Height);

            // Add it to the dictionary
            m_Textures.Add(stex.ID, stex);

            return stex.ID;
        }

        /// <summary>
        /// Creates a texture into the 3D session from an image and returns the texture's ID
        /// </summary>
        /// <returns>Texture's ID</returns>
        public int AddTexture(Texture texture, Size size)
        {
            if (texture == null) return -1;

            // We're going to add a new texture to the list
            STexture stex = new STexture();
            stex.Filename = "";

            // Assign it an ID
            stex.ID = m_nextTextureID++;

            // Create a texture from it
            stex.Texture = texture;

            // Texture info
            stex.Size = new Vector2(size.Width, size.Height);

            // Add it to the dictionary
            m_Textures.Add(stex.ID, stex);

            return stex.ID;
        }

        /// <summary>
        /// Creates a surface into the 3D session from an image and returns the surface's ID
        /// </summary>
        /// <returns>Texture's ID</returns>
        public int AddTexture(Surface surface, Size size)
        {
            if (surface == null) return -1;

            // We're going to add a new texture to the list
            STexture stex = new STexture();
            stex.Filename = "";

            // Assign it an ID
            stex.ID = m_nextTextureID++;

            // Create a texture from it
            stex.Texture = null;
            stex.Surface = surface;

            // Texture info
            stex.Size = new Vector2(size.Width, size.Height);

            // Add it to the dictionary
            m_Textures.Add(stex.ID, stex);

            return stex.ID;
        }

        /// <summary>
        /// Remove a texture from the manager
        /// </summary>
        /// <returns>Texture's ID</returns>
        public bool RemoveTexture(int id)
        {
            if (!m_Textures.ContainsKey(id)) return false;

            return m_Textures.Remove(id);
        }

        /// <summary>
        /// Loads a texture into the 3D session from an image file and returns the texture's ID
        /// </summary>
        /// <param name="filename">Image filename to load</param>
        /// <returns>Texture's ID</returns>
        public int LoadTexture(string filename)
        {
            // We're going to add a new texture to the list
            STexture stex = new STexture();
            stex.Filename = filename;

            // Load the bitmap
            Image img = null;
            try
            {
                img = Image.FromFile(filename);
            }
            catch (System.Exception)
            {
                // we'll catch this error just below
            }

            if (img == null) return -1;

            // Assign it an ID
            stex.ID = m_nextTextureID++;

            // Create a texture from it
            stex.Texture = Texture.FromBitmap(m_D3DDevice, (Bitmap)img, 0, Pool.Managed);

            // Texture info
            stex.Size = new Vector2(img.Size.Width, img.Size.Height);

            // Add it to the dictionary
            m_Textures.Add(stex.ID, stex);

            return stex.ID;
        }

        /// <summary>
        /// Loads a cube texture into the 3D session from an image file and returns the texture's ID
        /// </summary>
        /// <param name="filename">Cube-map image filename to load</param>
        /// <returns>Texture's ID</returns>
        public int LoadCubeTexture(string filename)
        {
            // We're going to add a new texture to the list
            STexture stex = new STexture();
            stex.Filename = filename;

            // Load the bitmap
            Image img = Image.FromFile(filename);
            if (img == null) return -1;

            // Assign it an ID
            stex.ID = m_nextTextureID++;

            // Create a texture from it
            int cubeEdgeSize = 256;
            stex.CubeTexture = new CubeTexture(m_D3DDevice, cubeEdgeSize, 0, Usage.None, Format.X8R8G8B8, Pool.Managed);

            Rectangle dstRect = new Rectangle(0, 0, cubeEdgeSize, cubeEdgeSize);
            int srcWidth = img.Width / 4;
            int x0 = srcWidth * 0;
            int x1 = srcWidth * 1;
            int x2 = srcWidth * 2;
            int x3 = srcWidth * 3;

            int srcHeight = img.Height / 3;
            int y0 = srcHeight * 0;
            int y1 = srcHeight * 1;
            int y2 = srcHeight * 2;

            // +Y
            using (Surface srf = stex.CubeTexture.GetCubeMapSurface(CubeMapFace.PositiveY, 0))
            {
                using (Graphics dst = srf.GetGraphics())
                {
                    Rectangle srcRect = new Rectangle(x1, y0, srcWidth, srcHeight);
                    dst.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                    srf.ReleaseGraphics();
                }
            }
            // -X
            using (Surface srf = stex.CubeTexture.GetCubeMapSurface(CubeMapFace.NegativeX, 0))
            {
                using (Graphics dst = srf.GetGraphics())
                {
                    Rectangle srcRect = new Rectangle(x0, y1, srcWidth, srcHeight);
                    dst.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                    srf.ReleaseGraphics();
                }
            }
            // +Z
            using (Surface srf = stex.CubeTexture.GetCubeMapSurface(CubeMapFace.PositiveZ, 0))
            {
                using (Graphics dst = srf.GetGraphics())
                {
                    Rectangle srcRect = new Rectangle(x1, y1, srcWidth, srcHeight);
                    dst.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                    srf.ReleaseGraphics();
                }
            }
            // +X
            using (Surface srf = stex.CubeTexture.GetCubeMapSurface(CubeMapFace.PositiveX, 0))
            {
                using (Graphics dst = srf.GetGraphics())
                {
                    Rectangle srcRect = new Rectangle(x2, y1, srcWidth, srcHeight);
                    dst.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                    srf.ReleaseGraphics();
                }
            }
            // -Z
            using (Surface srf = stex.CubeTexture.GetCubeMapSurface(CubeMapFace.NegativeZ, 0))
            {
                using (Graphics dst = srf.GetGraphics())
                {
                    Rectangle srcRect = new Rectangle(x3, y1, srcWidth, srcHeight);
                    dst.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                    srf.ReleaseGraphics();
                }
            }
            // -Y
            using (Surface srf = stex.CubeTexture.GetCubeMapSurface(CubeMapFace.NegativeY, 0))
            {
                using (Graphics dst = srf.GetGraphics())
                {
                    Rectangle srcRect = new Rectangle(x1, y2, srcWidth, srcHeight);
                    dst.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
                    srf.ReleaseGraphics();
                }
            }

            stex.CubeTexture.GenerateMipSubLevels();

            // Texture info
            stex.Size = new Vector2(cubeEdgeSize, cubeEdgeSize);

            // Add it to the dictionary
            m_Textures.Add(stex.ID, stex);

            return stex.ID;
        }

        /// <summary>
        /// Loads a texture from any format (jpg, png, bmp, gif) into the 3D session and returns the texture ID
        /// </summary>
        /// <param name="baseName">Base filename (without extension)</param>
        /// <returns>Texture's ID</returns>
        public int LoadFromAnyFormat(string baseName, bool cubeMap)
        {
            // Attempt to load the image (in any of a variety of formats)
            string[] extensions = { "jpg", "png", "bmp", "gif" };
            foreach (string extension in extensions)
            {
                string imageFilename = baseName + "." + extension;
                if (File.Exists(imageFilename))
                {
                    int id = -1;
                    if (cubeMap)
                    {
                        id = LoadCubeTexture(imageFilename);
                    }
                    else
                    {
                        id = LoadTexture(imageFilename);
                    }
                    if (id != -1) return id;
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves a texture from the manager based on the given Texture ID
        /// </summary>
        /// <param name="id">ID of the texture to return</param>
        /// <returns>Texture object (or null if not found)</returns>
        public STexture GetTexture(int id)
        {
            STexture stex = new STexture();
            stex.ID = 0;
            if (!m_Textures.ContainsKey(id)) return stex;

            m_Textures.TryGetValue(id, out stex);
            return stex;
        }

        public void OnDeviceReset()
        {
            foreach (STexture stex in m_Textures.Values)
            {
                if (stex.RenderTarget)
                {
                    // Recreate the render target
                    stex.Texture = new Texture(m_D3DDevice, (int) stex.Size.X, (int) stex.Size.Y, 1, Usage.RenderTarget, Manager.Adapters[0].CurrentDisplayMode.Format, Pool.Default);
                    stex.Surface = stex.Texture.GetSurfaceLevel(0);
                }
            }
        }

        /// <summary>
        /// Locates a render target that has text already rendered to it
        /// </summary>
        /// <param name="text">Text string to compare with what is already rendered on the panel</param>
        /// <returns>STexture object, or null if not found</returns>
        public STexture FindRenderTarget(string text, int scrollOffset)
        {
            foreach (STexture stex in m_Textures.Values)
            {
                if (stex.RenderTarget && !stex.InUse && stex.Text == text && stex.ScrollOffset == scrollOffset)
                {
                    stex.InUse = true;
                    return stex;
                }
            }

            return null;
        }

        /// <summary>
        /// Locates an available render target
        /// </summary>
        /// <returns>STexture object, or null if no available render target texture object was found</returns>
        public STexture FindRenderTarget()
        {
            foreach (STexture stex in m_Textures.Values)
            {
                if (stex.RenderTarget && !stex.InUse)
                {
                    stex.InUse = true;
                    return stex;
                }
            }

            return null;
        }

        public void ClearInUse()
        {
            foreach (STexture stex in m_Textures.Values)
            {
                stex.InUse = false;
            }
        }
    }
}
