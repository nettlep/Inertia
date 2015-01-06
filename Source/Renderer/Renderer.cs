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
using Microsoft.DirectX.AudioVideoPlayback;

namespace Inertia
{
    /// <summary>
    /// Renderer class. All rendering (including graphics setup) is managed through this class
    /// </summary>
    public class Renderer : IDisposable
    {
        #region Settings
        float m_PlaneReflectionFogDensity = Program.Settings.PlaneReflectionFogDensity;
        float m_PlaneReflectionPower = Program.Settings.PlaneReflectionPower;
        float m_DepthFogDensity = Program.Settings.DepthFogDensity;
        bool m_Windowed = Program.Settings.Windowed;
        int m_ScreenRotationDegrees = Program.Settings.ScreenRotationDegrees;
        float m_MonitorAspect = Program.Settings.MonitorAspect;
        float m_ReflectionMapAlpha = Program.Settings.ReflectionMapAlpha;
        Color m_BackgroundColor = Program.Settings.BackgroundColor.ToColor();
        Vector3 m_CameraPosition = Program.Settings.CameraPosition.ToVector3();
        Vector3 m_CameraTarget = Program.Settings.CameraTarget.ToVector3();
        float m_ScreenRotationRadians = 0;
        #endregion

        #region Properties
        /// <summary>
        /// Set to true when shutting down the application
        /// </summary>
        bool m_Shutdown = false;
        public bool Shutdown
        {
            get { return m_Shutdown; }
            set { m_Shutdown = value; }
        }

        /// <summary>
        /// Vertex buffers
        /// </summary>
        List<VertexBuffer> m_VertexBuffers = new List<VertexBuffer>();
        int m_VBIndex = 0;
        int m_TitleVB = 0;
        int m_ProgressVB = 0;
        int m_OverlayVB = 0;

        // Video
        public static Video m_Video = null;
        public Texture m_VideoTexture = null;
        public int m_VideoTextureID = -1;
        public bool m_OKToRender = false;
        bool m_VideoStopping = false;
        InertiaForm m_Form = null;
        public bool m_VideoRenderToTexture = false;

        public bool VideoPlaying
        {
            get
            {
                if (m_Video != null && m_Video.Playing)
                {
                    return true;
                }

                return false;
            }
        }

        public void StopVideo()
        {
            if (m_Video == null) return;

            if (VideoPlaying)
            {
                // Signal that we're trying to stop the video so we can early-out
                m_VideoStopping = true;

                // Stop the video
                m_Video.Stop();
                while (VideoPlaying)
                {
                    Application.DoEvents();
                }
            }

            // Make sure we activate, which resets the D3D device
            m_Form.Show();

            Application.DoEvents();
            m_Form.WindowState = FormWindowState.Normal;

            Application.DoEvents();
            m_Form.Activate();

            // Cleanup the video texture
            if (m_VideoTexture != null)
            {
                m_VideoTexture.Dispose();
                m_VideoTexture = null;
            }

            // Due to a known bug in the AudioVideoPlabyack, we cannot properly dispose of the
            // object. This will cause problems after long-term use and cause the application
            // to crash likely on exit.
            if (m_Video != null && !m_VideoRenderToTexture)
            {
                m_Video.Dispose();
            }
            m_Video = null;
        }

        public void PlayVideo(string filename)
        {
            m_VideoStopping = false;
            m_Video = Video.FromFile(filename);
            m_Video.Ending += new EventHandler(VideoEnding);
            m_Video.Starting += new EventHandler(VideoStarting);
            m_Video.Stopping += new EventHandler(VideoStopping);

            // This starts video playback within a texture
            if (m_VideoRenderToTexture)
            {
                m_Video.TextureReadyToRender += new TextureRenderEventHandler(OnTextureReadyToRender);
                m_Video.RenderToTexture(m_D3DDevice);
            }
            // Start the video playback in a full-screen window
            else
            {
                //m_Video.Owner = m_Form;
                m_Video.Owner = m_Form.VideoPictureBox;
                m_Video.Fullscreen = true;
                m_Video.Play();
            }
        }

        /// <summary>
        /// Width of the viewport
        /// </summary>
        int m_PhysicalWidth;
        public int PhysicalWidth
        {
            get { return m_PhysicalWidth; }
        }

        /// <summary>
        /// Height of the viewport
        /// </summary>
        int m_PhysicalHeight;
        public int PhysicalHeight
        {
            get { return m_PhysicalHeight; }
        }

        /// <summary>
        /// Rotated width of the viewport
        /// </summary>
        int m_ViewportWidth;
        public int ViewportWidth
        {
            get { return m_ViewportWidth; }
        }

        /// <summary>
        /// Rotated height of the viewport
        /// </summary>
        int m_ViewportHeight;
        public int ViewportHeight
        {
            get { return m_ViewportHeight; }
        }

        /// <summary>
        /// Aspect ratio, taking into account the viewport and physical display
        /// </summary>
        float m_PixAspect = 1;
        public float PixAspect
        {
            get { return m_PixAspect; }
        }

        /// <summary>
        /// Returns true if the device needs to be reset
        /// </summary>
        public bool NeedsReset
        {
            get
            { 
                bool result = m_D3DDevice != null && m_D3DDevice.CheckCooperativeLevel() == false;
                return result;
            }
        }

        /// <summary>
        /// Texture manager - stores and manages all textures for the 3D session
        /// </summary>
        TextureManager m_TextureManager = null;
        public Inertia.TextureManager TextureManager
        {
            get { return m_TextureManager; }
            set { m_TextureManager = value; }
        }
        #endregion

        #region Data members
        // Device information
        bool m_Antialiased = false;
        Device m_D3DDevice = null;
        Caps m_DeviceCaps;
        IndexBuffer m_RectIB = null;
        Effect m_Effect = null;
        PresentParameters m_PresentationParameters = null;
        Microsoft.DirectX.Direct3D.Font m_TitleFont;
        Microsoft.DirectX.Direct3D.Font m_GameInfoFont;
        Sprite m_Sprite;
        Surface m_BackBuffer;

        // View/Projection matrices
        Microsoft.DirectX.Matrix m_PerspectiveViewMatrix;
        Microsoft.DirectX.Matrix m_PerspectiveProjectionMatrix;
        Microsoft.DirectX.Matrix m_OrthoViewMatrix;
        Microsoft.DirectX.Matrix m_OrthoProjectionMatrix;
        #endregion

        /// <summary>
        /// Construct a rendering device
        /// </summary>
        /// <param name="form">Form to render to</param>
        /// <param name="width">Width - zero causes automatic use of the form or screen dimensions (depending on 'windowed' flag)</param>
        /// <param name="width">Height - zero causes automatic use of the form or screen dimensions (depending on 'windowed' flag)</param>
        public Renderer(InertiaForm form, int width, int height)
        {
            // viewport parameters
            m_PhysicalWidth = width;
            m_PhysicalHeight = height;
            m_Form = form;

            // viewport dimensions
            if (m_PhysicalWidth <= 0 || m_PhysicalHeight <= 0)
            {
                if (m_Windowed)
                {
                    m_PhysicalWidth = form.ClientRectangle.Width;
                    m_PhysicalHeight = form.ClientRectangle.Height;
                }
                else
                {
                    m_PhysicalWidth = Screen.PrimaryScreen.Bounds.Width;
                    m_PhysicalHeight = Screen.PrimaryScreen.Bounds.Height;
                }
            }

            // If the display is rotated, then our width/height are swapped
            if (m_ScreenRotationDegrees == 90 || m_ScreenRotationDegrees == -90)
            {
                m_ViewportWidth = PhysicalHeight;
                m_ViewportHeight = PhysicalWidth;
            }
            else
            {
                m_ViewportWidth = PhysicalWidth;
                m_ViewportHeight = PhysicalHeight;
            }

            // The adapter
            int adapter = Manager.Adapters.Default.Adapter;

            m_DeviceCaps = Manager.GetDeviceCaps(adapter, DeviceType.Hardware);

            // Current video format
            Format currentFormat = Manager.Adapters[0].CurrentDisplayMode.Format;

            // Setup our presentation parameters
            m_PresentationParameters = new PresentParameters();

            m_PresentationParameters.EnableAutoDepthStencil = true;
            m_PresentationParameters.AutoDepthStencilFormat = DepthFormat.D24S8;
            m_PresentationParameters.SwapEffect = SwapEffect.Discard;

            m_PresentationParameters.DeviceWindow = form;
            m_PresentationParameters.DeviceWindowHandle = form.Handle;

            m_PresentationParameters.BackBufferCount = 1;
            m_PresentationParameters.BackBufferFormat = currentFormat;
            m_PresentationParameters.PresentationInterval = PresentInterval.One;

            m_PresentationParameters.Windowed = m_Windowed;
            if (!m_PresentationParameters.Windowed)
            {
                m_PresentationParameters.BackBufferWidth = PhysicalWidth;
                m_PresentationParameters.BackBufferHeight = PhysicalHeight;
            }

            if (m_Antialiased && Manager.CheckDeviceMultiSampleType(adapter, DeviceType.Hardware, currentFormat, m_Windowed, MultiSampleType.FourSamples))
            {
                m_PresentationParameters.MultiSample = MultiSampleType.FourSamples;
            }

            // Initialize the rendering hardware
            CreateFlags flags = 0;
            flags |= CreateFlags.MultiThreaded;

            // Hardware vertex processing or software?
            if (m_DeviceCaps.DeviceCaps.SupportsHardwareTransformAndLight)
            {
                flags |= CreateFlags.HardwareVertexProcessing;

                // Does the device support a pure device?
                if (m_DeviceCaps.DeviceCaps.SupportsPureDevice)
                {
                    flags |= CreateFlags.PureDevice;
                }
            }
            else
            {
                flags |= CreateFlags.SoftwareVertexProcessing;
            }

            // Create the device
            m_D3DDevice = new Device(adapter, DeviceType.Hardware, form, flags, m_PresentationParameters);

            // Register an event-handler for DeviceReset and call it to continue our setup
            m_D3DDevice.DeviceReset += new System.EventHandler(OnDeviceReset);

            // Create our texture manager
            m_TextureManager = new TextureManager(m_D3DDevice);

            // Get our effects
            string compilerErrors = String.Empty;
            m_Effect = Effect.FromFile(m_D3DDevice, @"inertia\default.fx", null, "", 0, null, out compilerErrors);
            if (m_Effect == null)
            {
                if (compilerErrors.Length != 0)
                {
                    throw new Exception(compilerErrors);
                }
                else
                {
                    throw new Exception("Failed to compile effect");
                }
            }

            // Register a vertex buffer for use with rendering text
            m_TitleVB = RegisterVB(Program.Settings.TitleFontColor.A, 0, 0, 0);
            m_ProgressVB = RegisterVB(0.5f, 1, 1, 1);
            m_OverlayVB = RegisterVB(1, 1, 1, 1);

            // Index buffer
            m_RectIB = new IndexBuffer(typeof(int), 6, m_D3DDevice, Usage.WriteOnly, Pool.Managed);
            GraphicsStream gStream = m_RectIB.Lock(0, 0, LockFlags.None);
            gStream.Write(0);
            gStream.Write(1);
            gStream.Write(2);
            gStream.Write(1);
            gStream.Write(3);
            gStream.Write(2);
            m_RectIB.Unlock();

            // Calculate the final pixel aspect ratio, taking into account the aspect ratio of the physical monitor,
            // the viewport (which may be rotated) and the window, if used.
            //
            // Notes about aspect:
            //
            //  Standard PC wide-screen monitors are 16:10, which works out to an aspect ratio of 1.6
            //  when mounted horizontally, or 0.625 when mounted vertically
            //
            //  My MAME 19" monitor is 16" wide by 12" tall, which works out to 1.333 when mounted horizontally, or 0.75 when
            //  mounted vertically.
            float screenAspect = (float)Screen.PrimaryScreen.Bounds.Width / (float)Screen.PrimaryScreen.Bounds.Height;
            m_PixAspect = m_MonitorAspect / screenAspect;

            if (!m_Windowed && m_ScreenRotationDegrees != 0 && m_ScreenRotationDegrees != 180)
            {
                m_PixAspect = 1 / m_PixAspect;
            }

            // Initialize the device
            SetDefaultRenderStates();

            // Setup our fonts
            m_TitleFont = new Microsoft.DirectX.Direct3D.Font(m_D3DDevice, Program.Settings.TitleFont.ToFont());
            m_GameInfoFont = new Microsoft.DirectX.Direct3D.Font(m_D3DDevice, Program.Settings.GameInfoFont.ToFont());
            m_Sprite = new Sprite(m_D3DDevice);

            // Using a render target requires that we know our back-buffer
            m_BackBuffer = m_D3DDevice.GetBackBuffer(0, 0, BackBufferType.Mono);
        }

        public void Dispose()
        {
            StopVideo();
        }

        /// <summary>
        /// Reset the device to recover from a lost state
        /// </summary>
        public void ResetLostDevice()
        {
            // If we're shutting down, don't try to reset the device
            if (Shutdown) return;

            // Restore our resolution
            m_PresentationParameters.BackBufferWidth = PhysicalWidth;
            m_PresentationParameters.BackBufferHeight = PhysicalHeight;

            // Try to reset
            try
            {
                m_D3DDevice.Reset(m_PresentationParameters);
            }
            catch (DeviceLostException)
            {
            	// Do nothing
            }
        }

        /// <summary>
        /// Event handler for a reset device
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public void OnDeviceReset(Object o, EventArgs e)
        {
            SetDefaultRenderStates();

            m_BackBuffer = m_D3DDevice.GetBackBuffer(0, 0, BackBufferType.Mono);

            m_TextureManager.OnDeviceReset();
        }

        /// <summary>
        /// Adjust a SizeF to account for aspect (always returns a rectangle that will fit within the original rectangle)
        /// </summary>
        /// <param name="size">Input size</param>
        /// <returns>Adjusted size</returns>
        public Vector2 AdjustAspect(Vector2 scale)
        {
            Vector2 s = scale;
            if (m_PixAspect > 1)
            {
                return new Vector2(s.X / m_PixAspect, s.Y);
            }
            else
            {
                return new Vector2(s.X, s.Y * m_PixAspect);
            }
        }

        /// <summary>
        /// Return the largest aspect-adjusted size that will fit within a given size
        /// </summary>
        /// <param name="rect">Input size</param>
        /// <param name="maxSize">Maximum size</param>
        /// <returns>Adjusted size</returns>
        public Vector2 FitWithAspect(Vector2 scale, Vector2 maxScale)
        {
            // Adjust the size for aspect
            Vector2 aspectAdjustedSize = AdjustAspect(scale);
            return FitMaintainAspect(AdjustAspect(scale), maxScale);
        }

        /// <summary>
        /// Return the largest aspect-adjusted size that will fit within a given size
        /// </summary>
        /// <param name="rect">Input size</param>
        /// <param name="maxSize">Maximum size</param>
        /// <returns>Adjusted size</returns>
        public Vector2 FitMaintainAspect(Vector2 scale, Vector2 maxScale)
        {
            Vector2 newScale = scale;

            // Fit to width first
            newScale.Y *= maxScale.X / newScale.X;
            newScale.X = maxScale.X;

            // Fit by height?
            if (newScale.Y > maxScale.Y)
            {
                newScale.X *= maxScale.Y / newScale.Y;
                newScale.Y = maxScale.Y;
            }

            return newScale;
        }

        /// <summary>
        /// Set default render states
        /// </summary>
        public void SetDefaultRenderStates()
        {
            // Render states
            m_D3DDevice.RenderState.ZBufferEnable = true;
            m_D3DDevice.RenderState.ZBufferFunction = Compare.LessEqual;
            m_D3DDevice.RenderState.ZBufferWriteEnable = true;
            m_D3DDevice.RenderState.Lighting = false;
            m_D3DDevice.RenderState.AntiAliasedLineEnable = false;
            m_D3DDevice.RenderState.MultiSampleAntiAlias = m_Antialiased;
            m_D3DDevice.RenderState.AlphaBlendEnable = true;
            m_D3DDevice.RenderState.SourceBlend = Microsoft.DirectX.Direct3D.Blend.SourceAlpha;
            m_D3DDevice.RenderState.DestinationBlend = Microsoft.DirectX.Direct3D.Blend.InvSourceAlpha;
            m_D3DDevice.RenderState.AlphaBlendOperation = Microsoft.DirectX.Direct3D.BlendOperation.Add;
            m_D3DDevice.RenderState.CullMode = Cull.None;
        }

        /// <summary>
        /// Register a vertex buffer for a rectangle
        /// </summary>
        /// <returns>ID of the vertex buffer</returns>
        public int RegisterVB()
        {
            return RegisterVB(1, 1, 1, 1);
        }

        /// <summary>
        /// Register a vertex buffer for a rectangle
        /// </summary>
        /// <param name="a">alpha</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <returns>ID of the vertex buffer</returns>
        public int RegisterVB(float a, float r, float g, float b)
        {
            // Do we need to allocate a new VB?
            if (m_VBIndex >= m_VertexBuffers.Count)
            {
                m_VertexBuffers.Add(new VertexBuffer(typeof(SVertexPNDT), 4, m_D3DDevice, Usage.WriteOnly, SVertexPNDT.FVF_Flags, Pool.Managed));
            }

            // Setup the vertices
            VertexBuffer vb = m_VertexBuffers[m_VBIndex];
            GraphicsStream gStream = vb.Lock(0, 0, LockFlags.None);
            gStream.Write(new SVertexPNDT(-0.5f, 1, 0, 0, 0, -1, a, r, g, b, 0, 0));
            gStream.Write(new SVertexPNDT(+0.5f, 1, 0, 0, 0, -1, a, r, g, b, 1, 0));
            gStream.Write(new SVertexPNDT(-0.5f, 0, 0, 0, 0, -1, a, r, g, b, 0, 1));
            gStream.Write(new SVertexPNDT(+0.5f, 0, 0, 0, 0, -1, a, r, g, b, 1, 1));
            vb.Unlock();

            return m_VBIndex++;
        }

        /// <summary>
        /// Begin a rendering frame
        /// </summary>
        public void Begin()
        {
            // Need to reset?
            if (NeedsReset) return;

            m_D3DDevice.BeginScene();
            m_D3DDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, m_BackgroundColor, 10000, 0);

            // Set the matrices
            m_D3DDevice.Transform.World = Microsoft.DirectX.Matrix.Identity;
            m_D3DDevice.Transform.View = Microsoft.DirectX.Matrix.Identity;
            m_D3DDevice.Transform.Projection = Microsoft.DirectX.Matrix.Identity;

            m_PerspectiveViewMatrix = Microsoft.DirectX.Matrix.LookAtLH(m_CameraPosition, m_CameraTarget, new Vector3(0, 1, 0));
            m_PerspectiveProjectionMatrix = Microsoft.DirectX.Matrix.PerspectiveFovLH((float)Math.PI / 4, (float)PhysicalWidth / (float)PhysicalHeight, 1, 10000);

            m_OrthoViewMatrix = Microsoft.DirectX.Matrix.Identity;
            m_OrthoProjectionMatrix = Microsoft.DirectX.Matrix.OrthoLH(PhysicalWidth, PhysicalHeight, 1, 10000);

            // Rotate the display
            m_ScreenRotationRadians = -m_ScreenRotationDegrees * (float)Math.PI / 180;
            m_PerspectiveViewMatrix.Multiply(Microsoft.DirectX.Matrix.RotationZ(m_ScreenRotationRadians));
            m_OrthoViewMatrix.Multiply(Microsoft.DirectX.Matrix.RotationZ(m_ScreenRotationRadians));

            // Reset our VB list
            m_VBIndex = 0;
        }

        /// <summary>
        /// End a rendering frame and present the display
        /// </summary>
        public void End()
        {
            try
            {
                // Need to reset?
                if (NeedsReset) return;

                // End the scene
                m_D3DDevice.EndScene();

                // Present it
                m_D3DDevice.Present();
            }
            catch (DeviceLostException)
            {
                // Device lost
            }
        }

        /// <summary>
        /// Draw a rectangle (optionally textured, with cubic reflection map)
        /// </summary>
        /// <param name="vb">Vertex buffer ID (see RegisterVB)</param>
        /// <param name="textureID">Texture ID (use -1 for no texture)</param>
        /// <param name="reflectionTextureID">Reflection cubic texture id (use -1 for no texture)</param>
        /// <param name="objToWorld">Object-to-world transform</param>
        /// <param name="normalRotation">Transform for matrices</param>
        /// <param name="planeReflected">Flag to specify if this is reflected on the zero-plane</param>
        /// <param name="filtered">Flag to specify if texture should be filtered</param>
        public void DrawRect(int vb, int textureID, int reflectionTextureID, Microsoft.DirectX.Matrix objToWorld, Microsoft.DirectX.Matrix normalRotation, bool planeReflected, bool filtered)
        {
            // Setup the effect
            m_Effect.Technique = "Default";

            // Get the texture
            if (textureID != -1)
            {
                STexture tex = TextureManager.GetTexture(textureID);
                m_Effect.SetValue("Tex0", tex.Texture);
                m_Effect.SetValue("textured", true);
            }
            else
            {
                m_Effect.SetValue("textured", false);
            }

            if (reflectionTextureID != -1)
            {
                STexture refTex = TextureManager.GetTexture(reflectionTextureID);
                m_Effect.SetValue("Tex1", refTex.CubeTexture);
                m_Effect.SetValue("reflectionMapped", true);
            }
            else
            {
                m_Effect.SetValue("reflectionMapped", false);
            }

            m_Effect.SetValue("matWorld", objToWorld);
            m_Effect.SetValue("matView", m_PerspectiveViewMatrix);
            m_Effect.SetValue("matProjection", m_PerspectiveProjectionMatrix);
            m_Effect.SetValue("matNormals", normalRotation);
            m_Effect.SetValue("depthFogDensity", m_DepthFogDensity);
            m_Effect.SetValue("reflectionFogDensity", m_PlaneReflectionFogDensity);
            m_Effect.SetValue("reflectionPower", m_PlaneReflectionPower);
            m_Effect.SetValue("filtered", filtered);
            m_Effect.SetValue("planeReflected", planeReflected);
            m_Effect.SetValue("reflectionAlpha", m_ReflectionMapAlpha);
            m_Effect.SetValue("cameraPosition", new Vector4(m_CameraPosition.X, m_CameraPosition.Y, m_CameraPosition.Z, 1));

            // Setup the vertices & indices
            m_D3DDevice.VertexFormat = SVertexPNDT.FVF_Flags;
            m_D3DDevice.SetStreamSource(0, m_VertexBuffers[vb], 0);
            m_D3DDevice.Indices = m_RectIB;

            // Draw!
            m_Effect.Begin(0);
            m_Effect.BeginPass(0);
            m_D3DDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            m_Effect.EndPass();
            m_Effect.End();
        }

        public void DrawTitleText(string description)
        {
            Rectangle r = new Rectangle(0, 0, ViewportWidth, Program.Settings.TitleFont.ToFont().Height * 8);

            Microsoft.DirectX.Matrix m = Microsoft.DirectX.Matrix.Identity;
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(0.5f, -0.5f, 0)));
            m.Multiply(Microsoft.DirectX.Matrix.Scaling(new Vector3(r.Width, r.Height, 1)));
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(-ViewportWidth / 2, ViewportHeight / 2 - Program.Settings.TitleFont.ToFont().Height, 1)));

            m_Effect.Technique = "Default";
            m_Effect.SetValue("reflectionMapped", false);
            m_Effect.SetValue("textured", false);
            m_Effect.SetValue("matWorld", m);
            m_Effect.SetValue("matView", m_OrthoViewMatrix);
            m_Effect.SetValue("matProjection", m_OrthoProjectionMatrix);
            m_Effect.SetValue("depthFogDensity", 0);
            m_Effect.SetValue("reflectionFogDensity", 0);
            m_Effect.SetValue("reflectionPower", 1);
            m_Effect.SetValue("planeReflected", false);
            m_Effect.SetValue("reflectionMapped", false);
            m_Effect.SetValue("cameraPosition", new Vector4(m_CameraPosition.X, m_CameraPosition.Y, m_CameraPosition.Z, 1));

            // Setup the vertices & indices
            m_D3DDevice.VertexFormat = SVertexPNDT.FVF_Flags;
            m_D3DDevice.SetStreamSource(0, m_VertexBuffers[m_TitleVB], 0);
            m_D3DDevice.Indices = m_RectIB;

            // Draw!
            m_Effect.Begin(0);
            m_Effect.BeginPass(0);
            DisableZWrite();
            m_D3DDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            EnableZWrite();
            m_Effect.EndPass();
            m_Effect.End();

            m_Sprite.Begin(SpriteFlags.None);

            // Text rendering has its own transforms, so we need to apply the rotation here
            Microsoft.DirectX.Matrix xform = Microsoft.DirectX.Matrix.RotationZ(-m_ScreenRotationRadians);
            if (m_ScreenRotationDegrees == -90)
            {
                xform.Multiply(Microsoft.DirectX.Matrix.Translation(0, ViewportWidth, 0));
            }
            else if (m_ScreenRotationDegrees == 90)
            {
                xform.Multiply(Microsoft.DirectX.Matrix.Translation(ViewportHeight, 0, 0));
            }
            else if (m_ScreenRotationDegrees == 180)
            {
                xform.Multiply(Microsoft.DirectX.Matrix.Translation(ViewportWidth, ViewportHeight, 0));
            }

            m_Sprite.Transform = xform;
            Color c = Color.FromArgb(Program.Settings.TitleFontColor.A, Program.Settings.TitleFontColor.R, Program.Settings.TitleFontColor.G, Program.Settings.TitleFontColor.B);
            m_TitleFont.DrawText(m_Sprite, description, r, DrawTextFormat.Center | DrawTextFormat.VerticalCenter | DrawTextFormat.WordBreak, c);
            m_Sprite.End();
            m_Sprite.Transform = Microsoft.DirectX.Matrix.Identity;
        }

        public void DrawProgressBar(float progress)
        {
            Microsoft.DirectX.Matrix m = Microsoft.DirectX.Matrix.Identity;
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(0.5f, -0.5f, 0)));
            m.Multiply(Microsoft.DirectX.Matrix.Scaling(new Vector3(ViewportWidth * progress, 3, 1)));
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(-ViewportWidth / 2, ViewportHeight / 2 - Program.Settings.TitleFont.ToFont().Height * 3, 1)));

            m_Effect.Technique = "Default";
            m_Effect.SetValue("reflectionMapped", false);
            m_Effect.SetValue("textured", false);
            m_Effect.SetValue("matWorld", m);
            m_Effect.SetValue("matView", m_OrthoViewMatrix);
            m_Effect.SetValue("matProjection", m_OrthoProjectionMatrix);
            m_Effect.SetValue("depthFogDensity", 0);
            m_Effect.SetValue("reflectionFogDensity", 0);
            m_Effect.SetValue("reflectionPower", 1);
            m_Effect.SetValue("planeReflected", false);
            m_Effect.SetValue("reflectionMapped", false);
            m_Effect.SetValue("cameraPosition", new Vector4(m_CameraPosition.X, m_CameraPosition.Y, m_CameraPosition.Z, 1));

            // Setup the vertices & indices
            m_D3DDevice.VertexFormat = SVertexPNDT.FVF_Flags;
            m_D3DDevice.SetStreamSource(0, m_VertexBuffers[m_ProgressVB], 0);
            m_D3DDevice.Indices = m_RectIB;

            // Draw!
            m_Effect.Begin(0);
            m_Effect.BeginPass(0);
            DisableZWrite();
            m_D3DDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            EnableZWrite();
            m_Effect.EndPass();
            m_Effect.End();
        }

        private void VideoEnding(Object o, EventArgs e)
        {
            if (m_Video != null)
            {
                m_Video.Stop();
            }
        }

        private void VideoStopping(Object o, EventArgs e)
        {
            // Nothing to do
        }

        private void VideoStarting(Object o, EventArgs e)
        {
            // Nothing to do
        }

        private void OnTextureReadyToRender(object sender, TextureRenderEventArgs e)
        {
            using (e.Texture)
            {
                SurfaceDescription ds = e.Texture.GetLevelDescription(0);
                using (Surface systemSurface = m_D3DDevice.CreateOffscreenPlainSurface(ds.Width, ds.Height, ds.Format, Pool.SystemMemory))
                {
                    if (m_VideoTexture != null)
                    {
                        m_VideoTexture.Dispose(); m_VideoTexture = null;
                    }

                    m_VideoTexture = new Texture(m_D3DDevice, ds.Width, ds.Height, 1, Usage.Dynamic, ds.Format, Pool.Default);

                    m_VideoTextureID = m_TextureManager.AddTexture(m_VideoTexture, new Size(ds.Width, ds.Height));

                    using (Surface videoSurface = e.Texture.GetSurfaceLevel(0))
                    {
                        using (Surface textureSurface = m_VideoTexture.GetSurfaceLevel(0))
                        {
                            m_D3DDevice.GetRenderTargetData(videoSurface, systemSurface);
                            m_D3DDevice.UpdateSurface(systemSurface, textureSurface);
                        }
                    }

                    if (Shutdown == false)
                    {
                        // Wait for the scene to be rendered
                        m_OKToRender = true;
                        while (m_OKToRender && !m_VideoStopping && m_Video.Playing)
                        {
                            Application.DoEvents();
                        }
                    }

                    // Remove the video texture
                    m_TextureManager.RemoveTexture(m_VideoTextureID);
                    m_VideoTextureID = -1;
                }
            }
        }

        public void DrawOverlay(int textureID, Size size)
        {
            if (textureID == -1) return;

            Microsoft.DirectX.Matrix m = Microsoft.DirectX.Matrix.Identity;
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(0, -0.5f, 0)));
            m.Multiply(Microsoft.DirectX.Matrix.Scaling(new Vector3(size.Width, size.Height, 1)));
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(0, 0, 1)));

            STexture tex = TextureManager.GetTexture(textureID);

            m_Effect.Technique = "Default";
            m_Effect.SetValue("reflectionMapped", false);
            m_Effect.SetValue("textured", true);
            m_Effect.SetValue("Tex0", tex.Texture);
            m_Effect.SetValue("matWorld", m);
            m_Effect.SetValue("matView", m_OrthoViewMatrix);
            m_Effect.SetValue("matProjection", m_OrthoProjectionMatrix);
            m_Effect.SetValue("depthFogDensity", 0);
            m_Effect.SetValue("reflectionFogDensity", 0);
            m_Effect.SetValue("reflectionPower", 1);
            m_Effect.SetValue("planeReflected", false);
            m_Effect.SetValue("reflectionMapped", false);
            m_Effect.SetValue("cameraPosition", new Vector4(m_CameraPosition.X, m_CameraPosition.Y, m_CameraPosition.Z, 1));

            // Setup the vertices & indices
            m_D3DDevice.VertexFormat = SVertexPNDT.FVF_Flags;
            m_D3DDevice.SetStreamSource(0, m_VertexBuffers[m_OverlayVB], 0);
            m_D3DDevice.Indices = m_RectIB;

            // Draw!
            m_Effect.Begin(0);
            m_Effect.BeginPass(0);
            DisableZWrite();
            m_D3DDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            EnableZWrite();
            m_Effect.EndPass();
            m_Effect.End();
        }

        public int DrawGameInfo(string infoText, STexture stex, int scrollOffset)
        {
            // Draw our game info to the game info texture
            m_D3DDevice.SetRenderTarget(0, stex.Surface);
            m_D3DDevice.BeginScene();
            m_D3DDevice.Clear(ClearFlags.Target, Program.Settings.GameInfoBackgroundColor.ToColor(), 0, 0);
            Rectangle r = new Rectangle(0, -scrollOffset, (int)stex.Size.X, (int)stex.Size.Y + scrollOffset);
            m_Sprite.Begin(SpriteFlags.None);

            Color c = Program.Settings.GameInfoFontColor.ToColor();
            int maxHeight = m_GameInfoFont.DrawText(m_Sprite, infoText, r, DrawTextFormat.Left | DrawTextFormat.Top | DrawTextFormat.WordBreak, c);
            m_Sprite.End();
            m_D3DDevice.EndScene();

            // Restore the back buffer render target
            m_D3DDevice.SetRenderTarget(0, m_BackBuffer);

            return maxHeight;
        }

        public void DisableZWrite()
        {
            m_D3DDevice.RenderState.ZBufferWriteEnable = false;
        }

        public void EnableZWrite()
        {
            m_D3DDevice.RenderState.ZBufferWriteEnable = true;
        }

    }
}
