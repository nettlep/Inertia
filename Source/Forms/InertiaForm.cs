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
using System.Management;

namespace Inertia
{
    public partial class InertiaForm : Form
    {
        #region Settings
        int m_PanelBorderSize = Program.Settings.PanelBorderSize;
        Color m_PanelBorderColor = Program.Settings.PanelBorderColor.ToColor();
        SizeF m_PanelMaxSize = Program.Settings.PanelMaxSize.ToSizeF();
        SizeF m_PanelMinSize = Program.Settings.PanelMinSize.ToSizeF();
        float m_ImageBorderThickness = Program.Settings.ImageBorderThickness;
        float m_ImageMargin = Program.Settings.ImageMargin;
        Color m_ImageBorderColor = Program.Settings.ImageBorderColor.ToColor();
        float m_PanelTopHeight = Program.Settings.PanelTopHeight;
        Size m_TextPanelResolution = Program.Settings.TextPanelResolution.ToSize();
        System.Drawing.Font m_GameInfoFont = Program.Settings.GameInfoFont.ToFont();
        #endregion

        #region Properties & data members
        // List of games, with information about each
        List<Game> m_Games = new List<Game>();
        public List<Game> GameList
        {
            get { return m_Games; }
        }

        // Animation
        Animator m_Animator = null;
        int m_CurrentGame = 0;
        int m_CurrentPanel = 0;

        // Windows active state
        bool m_Active = false;

        // Renderer
        Renderer m_Renderer = null;
        int m_InitRenderWidth = 0;
        int m_InitRenderHeight = 0;

        // Textures
        int m_BackgroundTextureID = -1;
        int m_PanelTopTextureID = -1;
        int m_PanelBackTextureID = -1;
        int m_PanelReflectionTextureID = -1;
        int m_UserOverlayTextureID = -1;
        int m_ExitDialogTextureID = -1;
        int m_LoadedCount = 0;

        // Various vertex buffers
        int m_textureRectVB = -1;
        int m_pBorderVB = -1;
        int m_imageEdgeRectVB = -1;

        // Dialog states
        bool m_ExitDialog = false;
        #endregion

        #region Initialization
        /// <summary>
        /// Construct the InertiaForm and initiate background loading
        /// </summary>
        public InertiaForm()
        {
            InitializeComponent();

            // Force all rendering to be done through WM_PAINT messages
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            // Our width/height
            m_InitRenderWidth = Program.Settings.Resolution.Width;
            m_InitRenderHeight = Program.Settings.Resolution.Height;

            // If the user did not specify a size, then set a reasonable default
            if (m_InitRenderWidth <= 0 || m_InitRenderHeight <= 0)
            {
                if (Program.Settings.Windowed)
                {
                    m_InitRenderWidth = 640;
                    m_InitRenderHeight = 480;
                }
                else
                {
                    m_InitRenderWidth = Screen.PrimaryScreen.Bounds.Width;
                    m_InitRenderHeight = Screen.PrimaryScreen.Bounds.Height;
                }
            }

            if (Program.Settings.Windowed)
            {
                // Windowed applications need a border. We have the border disabled, because a
                // border tries to draw on top of our window, which just causes us to lose the
                // D3D device repeatedly.
                FormBorderStyle = FormBorderStyle.Fixed3D;

                // Set the window size so that the client rect is correct
                Width = m_InitRenderWidth + (Width - ClientRectangle.Width);
                Height = m_InitRenderHeight + (Height - ClientRectangle.Height);
            }

            // Init the animator
            m_Animator = new Animator();

            // Our game information
            XmlDocument mameXML = new XmlDocument();

            // Show the loading form
            LoadingForm.Start();

            // Load the mame.xml file
            mameXML.Load(Program.Settings.MameXmlFile);
            if (mameXML.DocumentElement.Name != "mame")
            {
                throw new Exception("mame.xml is missing the root <mame...> node");
            }

            // Quick-load our ROMs
            try
            {
                m_Games = Game.LoadGames(mameXML);
            }
            catch (System.Exception)
            {
                LoadingForm.Stop();
            }

            // If we don't have any games at all, we need to bail
            if (m_Games.Count == 0)
            {
                throw new Exception("No games!");
            }
        }
        #endregion

        #region Form events
        private void InertiaForm_Load(object sender, EventArgs e)
        {
        }

        private void InertiaForm_Paint(object sender, PaintEventArgs e)
        {
            if (m_Renderer == null)
            {
                try
                {
                    // Init the renderer
                    m_Renderer = new Renderer(this, m_InitRenderWidth, m_InitRenderHeight);

                    // Load our images
                    m_BackgroundTextureID = m_Renderer.TextureManager.LoadTexture(@"inertia\GroundPlane.jpg");
                    m_PanelTopTextureID = m_Renderer.TextureManager.LoadTexture(@"inertia\PanelTop.jpg");
                    m_PanelBackTextureID = m_Renderer.TextureManager.LoadTexture(@"inertia\PanelBack.jpg");
                    m_UserOverlayTextureID = m_Renderer.TextureManager.LoadTexture(@"inertia\overlay.png");
                    m_PanelReflectionTextureID = m_Renderer.TextureManager.LoadCubeTexture(@"inertia\CubeMap.jpg");
                    m_ExitDialogTextureID = m_Renderer.TextureManager.LoadTexture(@"inertia\ExitDialog.png");

                    // Setup our geometry
                    m_textureRectVB = m_Renderer.RegisterVB();
                    m_pBorderVB = m_Renderer.RegisterVB(1, (float)m_PanelBorderColor.R / 255, (float)m_PanelBorderColor.G / 255, (float)m_PanelBorderColor.B / 255);
                    m_imageEdgeRectVB = m_Renderer.RegisterVB(1, (float)m_ImageBorderColor.R / 255, (float)m_ImageBorderColor.G / 255, (float)m_ImageBorderColor.B / 255);

                    // Adjust our text panel resolution if necessary, to clip it to the size of the frame buffer
                    if (m_TextPanelResolution.Width > m_Renderer.PhysicalWidth) m_TextPanelResolution.Width = m_Renderer.PhysicalWidth;
                    if (m_TextPanelResolution.Height > m_Renderer.PhysicalHeight) m_TextPanelResolution.Height = m_Renderer.PhysicalHeight;

                    // We're initialized, start loading
                    LoadingWorker.RunWorkerAsync();

                    // Hide the loading form
                    LoadingForm.Stop();
                }
                catch (System.Exception ex)
                {
                    Close();
                    throw ex;
                }
            }
            else
            {
                // If the renderer has been shut down, bail now
                if (m_Renderer.Shutdown) return;

#if false
                // Is it time to start a video playback?
                if (m_Games.Count > m_CurrentGame && m_Games[m_CurrentGame].Panels.Count > m_CurrentPanel)
                {
                    Panel currentPanel = m_Games[m_CurrentGame].Panels[m_CurrentPanel];

                    // Should we consider updating the video playback state of this panel?
                    if (m_Renderer.VideoPlaying == false && currentPanel.VideoFile.Length != 0)
                    {
                        // Initialize the panel selection time
                        if (currentPanel.SelectionTime == null)
                        {
                            currentPanel.SelectionTime = DateTime.Now;
                        }

                        // Is it time to start a video?
                        if (currentPanel.SelectionTime < DateTime.Now.AddSeconds(-Program.Settings.VideoDemoDelay))
                        {
                            // Reset the panel selection time so that when it finishes, it will
                            // automatically restart the timer
                            currentPanel.SelectionTime = null;

                            // Start the video playback
                            m_Renderer.PlayVideo(currentPanel.VideoFile);
                        }
                    }
                }
#endif

                // Draw our interface
                try
                {
                    if (m_Renderer.VideoPlaying == false || m_Renderer.m_OKToRender == true)
                    {
                        DrawInterface();
                        m_Renderer.m_OKToRender = false;
                    }
                }
                catch (System.Exception)
                {
                    // Do nothing - this is just in case something goes haywire
                }
            }

            // Cause a repaint
            Invalidate();
        }

        private void InertiaForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Shutdown rendering
            if (m_Renderer != null)
            {
                m_Renderer.Shutdown = true;
                m_Renderer.Dispose();
            }

            // Cancel loading
            CancelLodingWorker();
        }

        private void InertiaForm_Activated(object sender, EventArgs e)
        {
            m_Active = true;

            if (m_Renderer != null && m_Renderer.NeedsReset)
            {
                m_Renderer.ResetLostDevice();
            }
        }

        private void InertiaForm_Deactivate(object sender, EventArgs e)
        {
            m_Active = false;
        }
        #endregion

        #region Keyboard events
        protected override bool IsInputKey(Keys keys)
        {
            return true;
        }

        private void InertiaForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (m_Renderer.VideoPlaying == true && m_Renderer.m_VideoRenderToTexture)
            {
                m_Renderer.StopVideo();
                e.Handled = true;
                return;
            }

            Game currentGame = m_Games[m_CurrentGame];

            Panel currentPanel = null;
            if (currentGame.Panels.Count > m_CurrentPanel) currentPanel = currentGame.Panels[m_CurrentPanel];

            if (m_ExitDialog && e.KeyCode != Keys.Escape)
            {
                m_ExitDialog = false;
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.R:
                    if (currentPanel != null && currentPanel.IsTextPanel())
                    {
                        // Scroll down one line
                        currentPanel.ScrollOffset -= m_GameInfoFont.Height;
                        if (currentPanel.ScrollOffset < 0) currentPanel.ScrollOffset = 0;

                        // Mark the panel dirty
                        currentPanel.TextureID = -1;
                    }
                    else
                    {
                        // Tell the animation system we're deactivating the current game
                        Animator.TriggerGlobalEvent("GameDeactivate");

                        // Stop the video
                        StopVideo();

                        // Change the game
                        m_CurrentGame -= 1;
                        if (m_CurrentGame < 0) m_CurrentGame += m_Games.Count;

                        // Reset the current panel
                        m_CurrentPanel = 0;

                        // Add the game to the scene
                        Scene.Add(currentGame, m_CurrentGame);
                    }

                    e.Handled = true;
                    break;

                case Keys.Down:
                case Keys.F:
                    if (currentPanel != null && currentPanel.IsTextPanel())
                    {
                        // Scroll up one line
                        currentPanel.ScrollOffset += m_GameInfoFont.Height;
                        int adjustedMaxScroll = currentPanel.MaxScroll - m_TextPanelResolution.Height;
                        if (adjustedMaxScroll < 0) adjustedMaxScroll = 0;

                        if (currentPanel.ScrollOffset > adjustedMaxScroll)
                        {
                            currentPanel.ScrollOffset = adjustedMaxScroll;
                        }

                        // Mark the panel dirty
                        currentPanel.TextureID = -1;
                    }
                    else
                    {
                        // Tell the animation system we're deactivating the current game
                        Animator.TriggerGlobalEvent("GameDeactivate");

                        // Stop the video
                        StopVideo();

                        // Change the game
                        m_CurrentGame += 1;
                        if (m_CurrentGame >= m_Games.Count) m_CurrentGame -= m_Games.Count;

                        // Reset the current panel
                        m_CurrentPanel = 0;

                        // Add the game to the scene
                        Scene.Add(currentGame, m_CurrentGame);
                    }

                    e.Handled = true;
                    break;

                case Keys.Left:
                case Keys.D:
                    Animator.TriggerGlobalEvent("PanelDeactivate", m_CurrentPanel);

                    // Stop the video
                    StopVideo();

                    m_CurrentPanel -= 1;
                    if (m_CurrentPanel < 0) m_CurrentPanel += currentGame.Panels.Count;

                    Animator.TriggerGlobalEvent("PanelActivate", m_CurrentPanel);

                    e.Handled = true;
                    break;

                case Keys.Right:
                case Keys.G:
                    Animator.TriggerGlobalEvent("PanelDeactivate", m_CurrentPanel);

                    // Stop the video
                    StopVideo();

                    m_CurrentPanel += 1;
                    if (m_CurrentPanel >= currentGame.Panels.Count) m_CurrentPanel = 0;

                    Animator.TriggerGlobalEvent("PanelActivate", m_CurrentPanel);

                    e.Handled = true;
                    break;

                case Keys.Enter:
                case Keys.D1:
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    if (!m_ExitDialog)
                    {
                        m_ExitDialog = true;
                    }
                    else
                    {
                        Close();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void InertiaForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (m_Renderer.VideoPlaying == true && m_Renderer.m_VideoRenderToTexture)
            {
                m_Renderer.StopVideo();
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    e.Handled = true;
                    break;

                case Keys.D2:
                    StartGame(true && Program.Settings.EnableVideoWriteOnCoin);
                    break;

                case Keys.Enter:
                case Keys.D1:
                    StartGame(false);
                    break;
            }
        }
        #endregion

        #region Background texture loading
        private void CancelLodingWorker()
        {
            // If the LoadingWorker is already finished, we don't need to do anything
            if (!LoadingWorker.IsBusy) return;

            // Cancel the background worker
            LoadingWorker.CancelAsync();

            // Wait for it to finish
            while (LoadingWorker.IsBusy)
            {
                // We sleep while we wait, we don't call Application.DoEvents() intentionally
                //System.Threading.Thread.Sleep(100);
                Application.DoEvents();
            }
        }

        private void LoadingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // We work from the user's current index outward (in both directions) loading files
            // until we finish the entire list. Each time we load an image, we'll start over
            // in case they moved during that time
            while(true)
            {
                // Done?
                if (m_LoadedCount == m_Games.Count) break;

                // Cancel?
                if (LoadingWorker.CancellationPending) break;
                if (m_Renderer == null || (m_Renderer.NeedsReset && !m_Renderer.Shutdown))
                {
                    System.Threading.Thread.Sleep(10);
                    continue;
                }

                // Cancel?
                if (LoadingWorker.CancellationPending) break;

                // Load our current index
                int index = m_CurrentGame;
                if (index >= m_Games.Count) index -= m_Games.Count;
                if (m_Games[index].Loaded == false)
                {
                    m_Games[index].LoadTextures(m_Renderer.TextureManager);
                    m_LoadedCount++;

                    // Start over
                    continue;
                }

                // Cancel?
                if (LoadingWorker.CancellationPending) break;

                int count = m_Games.Count / 2 + 1;
                for (int i = 0; i < count; ++i)
                {
                    int prev = index - i;
                    if (prev < 0) prev += m_Games.Count;
                    if (m_Games[prev].Loaded == false)
                    {
                        m_Games[prev].LoadTextures(m_Renderer.TextureManager);
                        m_LoadedCount++;

                        // Start over
                        break;
                    }

                    // Cancel?
                    if (LoadingWorker.CancellationPending) break;

                    int next = index + i;
                    if (next >= m_Games.Count) next -= m_Games.Count;
                    if (m_Games[next].Loaded == false)
                    {
                        m_Games[next].LoadTextures(m_Renderer.TextureManager);
                        m_LoadedCount++;

                        // Start over
                        break;
                    }

                    // Cancel?
                    if (LoadingWorker.CancellationPending) break;
                }
            }
        }
        #endregion

        #region Drawing
        private void DrawInterface()
        {
            if (m_Renderer == null)
            {
                return;
            }

            // Do we need to reset?
            if (m_Renderer.NeedsReset)
            {
                // We need to be active in order to reset
                if (!m_Active)
                {
                    return;
                }

                // Try a reset
                m_Renderer.ResetLostDevice();

                // If the reset didn't take, bail and we'll try again later
                if (m_Renderer.NeedsReset)
                {
                    return;
                }
            }

            // We are loading in the background, so we may have landed on a game before
            // all (or any) of its panels are loaded. We'll perform dynamic correction
            // here by adding any missing panels that are loaded.
            Game currentGame = m_Games[m_CurrentGame];
            Scene.Add(currentGame, m_CurrentGame);

            // Animate the entire scene
            Animator.Tick();

            // Be very fastidious about hiding this cursor
            if (Program.Settings.Windowed == false)
            {
                Cursor.Hide();
                Cursor.Position = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height - 1);
            }

            // Go through and gather ready-made RTs for those panels in the scene that need them
            //
            // Note that this process has a side-effect that it sets the STexture.InUse flags so we can
            // later re-use render targets that don't have these flags set (or add new as needed)
            m_Renderer.TextureManager.ClearInUse();
            foreach (SceneObject so in Scene.Objects)
            {
                if (currentGame.Panels.Count <= so.PanelID) continue;

                Panel panel = currentGame.Panels[so.PanelID];

                // Make sure this requires a render target
                if (!panel.IsTextPanel()) continue;

                // See if we already have a render target with this text printed on it
                STexture stex = m_Renderer.TextureManager.FindRenderTarget(panel.InfoText, panel.ScrollOffset);
                if (stex != null)
                {
                    // Found a matching render target, don't bother re-rendering this one
                    panel.TextureID = stex.ID;
                    stex.ScrollOffset = panel.ScrollOffset;
                }
                else
                {
                    // This panel will need a new RT (or a re-rendering of an existing RT)
                    panel.TextureID = -1;
                }
            }

            // Pre-render text panels into render targets (and create new if needed)
            foreach (SceneObject so in Scene.Objects)
            {
                if (currentGame.Panels.Count <= so.PanelID) continue;

                Panel panel = currentGame.Panels[so.PanelID];

                // Make sure this requires a render target
                if (!panel.IsTextPanel()) continue;

                // If we already have a texture, skip
                if (panel.TextureID != -1) continue;

                // See if we already have an available render target
                STexture stex = m_Renderer.TextureManager.FindRenderTarget();
                if (stex == null)
                {
                    int id = m_Renderer.TextureManager.AddRenderTarget(m_TextPanelResolution);
                    stex = m_Renderer.TextureManager.GetTexture(id);
                }

                if (stex != null)
                {
                    panel.TextureID = stex.ID;

                    stex.InUse = true;
                    stex.Text = panel.InfoText;

                    // Is this the current panel?
                    if (so.ObjectID == m_CurrentGame && so.PanelID == m_CurrentPanel)
                    {
                        // Set the scroll offset to our scroll offset
                        stex.ScrollOffset = panel.ScrollOffset;
                    }
                    else
                    {
                        stex.ScrollOffset = 0;
                    }

                    panel.MaxScroll = m_Renderer.DrawGameInfo(panel.InfoText, stex, panel.ScrollOffset);
                }
            }

            // Begin the rendering
            m_Renderer.Begin();

            // Background
            Microsoft.DirectX.Matrix m = Microsoft.DirectX.Matrix.Identity;
            m.Multiply(Microsoft.DirectX.Matrix.Translation(new Vector3(0, -0.5f, 0)));
            m.Multiply(Microsoft.DirectX.Matrix.Scaling(new Vector3(m_Renderer.ViewportWidth, m_Renderer.ViewportWidth, 0)));
            m.Multiply(Microsoft.DirectX.Matrix.RotationX((float) Math.PI / 2));
            m_Renderer.DisableZWrite();
            m_Renderer.DrawRect(m_textureRectVB, m_BackgroundTextureID, -1, m, Microsoft.DirectX.Matrix.Identity, false, true);
            m_Renderer.EnableZWrite();

            // Draw our scene objects
            foreach (SceneObject obj in Scene.Objects)
            {
                DrawReflectivePanel(obj);
            }

            // Draw the title
            m_Renderer.DrawTitleText(currentGame.Description);

            // Draw loading progress bar
            m_Renderer.DrawProgressBar((float)m_LoadedCount / (float)m_Games.Count);

            // Draw the overlay
            m_Renderer.DrawOverlay(m_UserOverlayTextureID, new Size(m_Renderer.ViewportWidth, m_Renderer.ViewportHeight));

            if (m_ExitDialog)
            {
                DrawExitDialog();
            }

            // Done rendering
            m_Renderer.End();
        }

        private Vector2 CalcPanelDimensions(int texture, SizeF scale)
        {
            if (texture == -1)
            {
                return new Vector2(scale.Width, scale.Height);
            }

            // Get the texture
            STexture stex = m_Renderer.TextureManager.GetTexture(texture);

            // We calculate the size of an entire panel, so that when it is drawn with borders and margins,
            // it ends up with an image in the middle that is properly aspect-adjusted
            float panelBorder = (m_PanelBorderSize + m_ImageMargin + m_ImageBorderThickness) * 2;
            Vector2 panelSize = new Vector2(stex.Size.X + panelBorder, stex.Size.Y + panelBorder);

            // Aspect adjust and fit
            return m_Renderer.FitMaintainAspect(panelSize, new Vector2(scale.Width, scale.Height));
        }

        private void DrawReflectivePanel(SceneObject obj)
        {
            Game game = m_Games[obj.ObjectID];
            if (game.Panels.Count <= obj.PanelID) return;

            Panel panel = game.Panels[obj.PanelID];
            bool aspectAdjusted = true;

            int textureID = -1;
            SizeF maxSize = m_PanelMaxSize;
            textureID = panel.TextureID;

            if (m_Renderer.VideoPlaying && m_Renderer.m_VideoTextureID != -1 && panel.IsSnapPanel)
            {
                textureID = m_Renderer.m_VideoTextureID;
            }

            // Text panels do not get aspect adjusted, so they fill the entire panel
            if (panel.IsTextPanel() && !panel.IsSnapPanel)
            {
                aspectAdjusted = false;
            }

            Vector2 sSize = CalcPanelDimensions(textureID, maxSize);

            DrawPanel(textureID, obj.Position, obj.Rotation, obj.Scale, sSize, false, aspectAdjusted);
            DrawPanel(textureID, obj.Position, obj.Rotation, obj.Scale, sSize, true, aspectAdjusted);
        }

        private void DrawPanel(int texture, Vector3 position, Vector3 rotation, Vector3 scale, Vector2 size, bool reflected, bool aspectAdjusted)
        {
            // Panel rect
            Vector3 pPanelRect = new Vector3(0, 0, 0);
            Vector3 sPanelRect = new Vector3(size.X, size.Y, 0);
            if (sPanelRect.X < m_PanelMinSize.Width) sPanelRect.X = m_PanelMaxSize.Width;
            if (sPanelRect.Y < m_PanelMinSize.Height) sPanelRect.Y = m_PanelMaxSize.Height;

            // Panel border position & scale
            Vector3 pBorder = pPanelRect;
            Vector3 sBorder = sPanelRect;

            // Adjust the panel rect for the border size
            pPanelRect.Y += m_PanelBorderSize;
            sPanelRect.X -= m_PanelBorderSize * 2;
            sPanelRect.Y -= m_PanelBorderSize * 2;

            // Panel background (clip a bit off for the top)
            Vector3 pPanelBack = pPanelRect;
            Vector3 sPanelBack = sPanelRect - new Vector3(0, m_PanelTopHeight, 0);

            // Panel top
            Vector3 pTop = new Vector3(pPanelRect.X, pPanelRect.Y + sPanelRect.Y - m_PanelTopHeight, 0);
            Vector3 sTop = new Vector3(sPanelRect.X, m_PanelTopHeight, 0);

            // Adjust the panel rect for the image margin
            pPanelRect.Y += m_ImageMargin;
            sPanelRect.X -= m_ImageMargin * 2;
            sPanelRect.Y -= m_ImageMargin * 2;

            // Image border
            Vector3 pImageBorder = pPanelRect;
            Vector3 sImageBorder = sPanelRect;

            // Adjust the panel rect for the image border
            pPanelRect.Y += m_ImageBorderThickness;
            sPanelRect.X -= m_ImageBorderThickness * 2;
            sPanelRect.Y -= m_ImageBorderThickness * 2;

            // Image size (optionally aspect-adjusted)
            Vector3 sImage;
            if (aspectAdjusted)
            {
                float panelBorder = (m_PanelBorderSize + m_ImageMargin + m_ImageBorderThickness) * 2;
                Vector2 imageSize = new Vector2(size.X - panelBorder, size.Y - panelBorder);
                Vector2 sImageSize = m_Renderer.FitWithAspect(imageSize, new Vector2(sPanelRect.X, sPanelRect.Y));
                sImage = new Vector3(sImageSize.X, sImageSize.Y, 0);
            }
            else
            {
                sImage = new Vector3(sPanelRect.X, sPanelRect.Y, 0);
            }

            // Image position (centered)
            Vector3 pImage = pPanelRect;
            pImage.Y += (sPanelRect.Y - sImage.Y) / 2;

            Microsoft.DirectX.Matrix xm = Microsoft.DirectX.Matrix.Identity;
            Microsoft.DirectX.Matrix rm = Microsoft.DirectX.Matrix.Identity;
            float zBias = 0;
            float zBiasDelta = 0.1f;

            // Draw the entire panel from Front-to-Back
            rm = Microsoft.DirectX.Matrix.Identity;

            // Deg to Rad
            rotation *= (float)Math.PI / 180;

            rm.Multiply(Microsoft.DirectX.Matrix.RotationX(rotation.X));
            rm.Multiply(Microsoft.DirectX.Matrix.RotationY(rotation.Y));
            rm.Multiply(Microsoft.DirectX.Matrix.RotationZ(rotation.Z));

            if (texture != -1)
            {
                xm = Microsoft.DirectX.Matrix.Identity;
                xm.Multiply(Microsoft.DirectX.Matrix.Scaling(sImage));
                xm.Multiply(Microsoft.DirectX.Matrix.Translation(pImage + new Vector3(0, 0, zBias)));
                xm.Multiply(Microsoft.DirectX.Matrix.Scaling(scale));
                xm.Multiply(rm);
                xm.Multiply(Microsoft.DirectX.Matrix.Translation(position));
                m_Renderer.DrawRect(m_textureRectVB, texture, m_PanelReflectionTextureID, xm, rm, reflected, true);

                zBias += zBiasDelta;

                xm = Microsoft.DirectX.Matrix.Identity;
                xm.Multiply(Microsoft.DirectX.Matrix.Scaling(sImageBorder));
                xm.Multiply(Microsoft.DirectX.Matrix.Translation(pImageBorder + new Vector3(0, 0, zBias)));
                xm.Multiply(Microsoft.DirectX.Matrix.Scaling(scale));
                xm.Multiply(rm);
                xm.Multiply(Microsoft.DirectX.Matrix.Translation(position));
                m_Renderer.DrawRect(m_imageEdgeRectVB, -1, -1, xm, rm, reflected, false);

                zBias += zBiasDelta;
            }

            xm = Microsoft.DirectX.Matrix.Identity;
            xm.Multiply(Microsoft.DirectX.Matrix.Scaling(sTop));
            xm.Multiply(Microsoft.DirectX.Matrix.Translation(pTop + new Vector3(0, 0, zBias)));
            xm.Multiply(Microsoft.DirectX.Matrix.Scaling(scale));
            xm.Multiply(rm);
            xm.Multiply(Microsoft.DirectX.Matrix.Translation(position));
            m_Renderer.DrawRect(m_textureRectVB, m_PanelTopTextureID, -1, xm, rm, reflected, true);

            xm = Microsoft.DirectX.Matrix.Identity;
            xm.Multiply(Microsoft.DirectX.Matrix.Scaling(sPanelBack));
            xm.Multiply(Microsoft.DirectX.Matrix.Translation(pPanelBack + new Vector3(0, 0, zBias)));
            xm.Multiply(Microsoft.DirectX.Matrix.Scaling(scale));
            xm.Multiply(rm);
            xm.Multiply(Microsoft.DirectX.Matrix.Translation(position));
            m_Renderer.DrawRect(m_textureRectVB, m_PanelBackTextureID, -1, xm, rm, reflected, true);
            zBias += zBiasDelta;

            xm = Microsoft.DirectX.Matrix.Identity;
            xm.Multiply(Microsoft.DirectX.Matrix.Scaling(sBorder));
            xm.Multiply(Microsoft.DirectX.Matrix.Translation(pBorder + new Vector3(0, 0, zBias)));
            xm.Multiply(Microsoft.DirectX.Matrix.Scaling(scale));
            xm.Multiply(rm);
            xm.Multiply(Microsoft.DirectX.Matrix.Translation(position));
            m_Renderer.DrawRect(m_pBorderVB, -1, -1, xm, rm, reflected, false);
        }

        void DrawExitDialog()
        {
            if (m_ExitDialogTextureID != -1)
            {
                STexture stex = m_Renderer.TextureManager.GetTexture(m_ExitDialogTextureID);
                m_Renderer.DrawOverlay(m_ExitDialogTextureID, new Size((int) stex.Size.X, (int) stex.Size.Y));
            }
        }

        #endregion

        #region Utilitarian
        void StopVideo()
        {
            if (m_Renderer.VideoPlaying)
            {
                // Stop any videos playing
                m_Renderer.StopVideo();
            }

            // Find the current panel
            if (m_Games.Count <= m_CurrentGame) return;
            Game currentGame = m_Games[m_CurrentGame];
            if (currentGame.Panels.Count <= m_CurrentPanel) return;
            Panel currentPanel = currentGame.Panels[m_CurrentPanel];

            // Reset the selection time for the panel
            currentPanel.SelectionTime = null;
        }

        void StartGame(bool enableVideoWrite)
        {
            Game currentGame = m_Games[m_CurrentGame];

            // Make sure we don't see the cursor
            if (Program.Settings.Windowed == false)
            {
                Cursor.Position = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height - 1);
            }

            // Stop the video
            StopVideo();

            // Launch the process to start a ROM
            Process p = new Process();
            p.StartInfo.Arguments = currentGame.BaseName;
            switch (Program.Settings.ScreenRotationDegrees)
            {
                case 90:
                    p.StartInfo.Arguments += " -rotate -ror";
                    break;
                case -90:
                    p.StartInfo.Arguments += " -rotate -rol";
                    break;
            }

            if (enableVideoWrite)
            {
                p.StartInfo.Arguments += " -aviwrite " + currentGame.BaseName + ".avi";
            }

            p.StartInfo.Arguments += " " + Program.Settings.MAMEParms;
            p.StartInfo.FileName = "mame.exe";
            p.Start();
            p.WaitForExit();

            // Make sure we activate, which resets the D3D device
            Application.DoEvents();
            Show();

            Application.DoEvents();
            WindowState = FormWindowState.Normal;

            Application.DoEvents();
            Activate();
        }
        #endregion
    }
}
