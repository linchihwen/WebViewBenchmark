﻿using System;
using System.IO;
using Awesomium.Core;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using Awesomium.Windows.Forms;
using System.Runtime.InteropServices;

namespace KioskBenchmark.FormsAwesomium
{
    public partial class MainForm : Form
    {
        #region Fields
        private WebView webView;
        private ImageSurface surface;
        private WebSession session;
        private BindingSource bindingSource;
        #endregion


        #region Ctors
        public MainForm()
        {
            // Initialize the core and get a WebSession.
            WebSession session = InitializeCoreAndSession();

            // Notice that 'Control.DoubleBuffered' has been set to true
            // in the designer, to prevent flickering.

            InitializeComponent();

            // Initialize a new view.
            InitializeView( WebCore.CreateWebView( this.ClientSize.Width, this.ClientSize.Height, session ) );
        }

        public MainForm( Uri targetURL )
        {
            // Initialize the core and get a WebSession.
            WebSession session = InitializeCoreAndSession();

            // Notice that 'Control.DoubleBuffered' has been set to true
            // in the designer, to prevent flickering.

            InitializeComponent();

            // Initialize a new view.
            InitializeView( WebCore.CreateWebView( this.ClientSize.Width, this.ClientSize.Height, session ), false, targetURL );
        }

        // Used to create child (popup) windows.
        internal MainForm(WebView view, int width, int height)
        {
            this.Width = width;
            this.Height = height;

            InitializeComponent();

            // Initialize the view.
            InitializeView( view, true );

            // We should immediately call a resize,
            // after wrapping child views.
            if ( view != null )
                view.Resize( width, height );
        }
        #endregion


        #region Methods
        private WebSession InitializeCoreAndSession()
        {
            if ( !WebCore.IsRunning )
                WebCore.Initialize( new WebConfig() { LogLevel = LogLevel.Normal } );

            // Build a data path string. In this case, a Cache folder under our executing directory.
            // - If the folder does not exist, it will be created.
            // - The path should always point to a writeable location.
            string dataPath = String.Format( "{0}{1}Cache", Path.GetDirectoryName( Application.ExecutablePath ), Path.DirectorySeparatorChar );

            // Check if a session synchronizing to this data path, is already created;
            // if not, create a new one.
            session = WebCore.Sessions[ dataPath ] ??
                WebCore.CreateWebSession( dataPath, WebPreferences.Default );

            // The core must be initialized by now. Print the core version.
            Debug.Print( WebCore.Version.ToString() );

            // Return the session.
            return session;
        }

        private void InitializeView( WebView view, bool isChild = false, Uri targetURL = null )
        {
            if ( view == null )
                return;

            // Create an image surface to render the
            // WebView's pixel buffer.
            surface = new ImageSurface();
            surface.Updated += OnSurfaceUpdated;

            webView = view;

            // Assign our surface.
            webView.Surface = surface;

            if ( !isChild )
                webView.Source = targetURL ?? new Uri( "http://www.google.com/ncr" );

            // Give focus to the view.
            webView.FocusView();
        }

        protected override void OnPaint( PaintEventArgs e )
        {
            if ( ( surface != null ) && ( surface.Image != null ) )
                e.Graphics.DrawImageUnscaled( surface.Image, 0, 0 );
            else
                base.OnPaint( e );
        }

        protected override void OnActivated( EventArgs e )
        {
            base.OnActivated( e );
            this.Opacity = 1.0D;

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.FocusView();
        }

        protected override void OnDeactivate( EventArgs e )
        {
            base.OnDeactivate( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            // Let popup windows be semi-transparent,
            // when they are not active.
            if ( webView.ParentView != null )
                this.Opacity = 0.8D;

            webView.UnfocusView();
        }

        protected override void OnFormClosed( FormClosedEventArgs e )
        {
            // Get if this is form hosting a child view.
            bool isChild = webView.ParentView != null;

            // Destroy the WebView.
            if ( webView != null )
            {
                webView.Dispose();
                webView = null;
            }

            // The surface that is currently assigned to the view,
            // does not need to be disposed. It will be disposed 
            // internally.

            base.OnFormClosed( e );

            // Shut down the WebCore last.
            if ( Application.OpenForms.Count == 0 )
                WebCore.Shutdown();
        }

        protected override void OnResize( EventArgs e )
        {
            base.OnResize( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            // Never resize the view to a width or height equal to 0;
            // instead, you can pause internal rendering.
            webView.IsRendering = ( this.ClientSize.Width > 0 ) && ( this.ClientSize.Height > 0 );

            if ( webView.IsRendering )
                // Request a resize.
                webView.Resize( this.ClientSize.Width, this.ClientSize.Height );
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            base.OnKeyPress( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectKeyboardEvent( e.GetKeyboardEvent() );
        }

        protected override void OnKeyDown( KeyEventArgs e )
        {
            base.OnKeyDown( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectKeyboardEvent( e.GetKeyboardEvent( WebKeyboardEventType.KeyDown ) );
        }

        protected override void OnKeyUp( KeyEventArgs e )
        {
            base.OnKeyUp( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectKeyboardEvent( e.GetKeyboardEvent( WebKeyboardEventType.KeyUp ) );
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            base.OnMouseDown( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectMouseDown( e.Button.GetMouseButton() );
        }

        protected override void OnMouseUp( MouseEventArgs e )
        {
            base.OnMouseUp( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectMouseUp( e.Button.GetMouseButton() );
        }

        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectMouseMove( e.X, e.Y );
        }

        protected override void OnMouseWheel( MouseEventArgs e )
        {
            base.OnMouseWheel( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            webView.InjectMouseWheel( e.Delta, 0 );
        }

        private void OnSurfaceUpdated(object sender, SurfaceUpdatedEventArgs e)
        {
            Invalidate(e.DirtyRegion.ToRectangle(), false);
        }
        #endregion
        
        #region Event Handlers
        /*private void OnAddressChanged( object sender, UrlEventArgs e )
        {
            // Reflect the current URL to the window text.
            // Normally, after the page loads, we will get a title.
            // But a page may as well not specify a title.
            this.Text = e.Url.AbsoluteUri;
        }

        private void OnCursorChanged( object sender, CursorChangedEventArgs e )
        {
            // Update the cursor.
            this.Cursor = Awesomium.Windows.Forms.Utilities.GetCursor( e.CursorType );
        }

        private void OnSurfaceUpdated( object sender, SurfaceUpdatedEventArgs e )
        {
            // When the surface is updated, invalidate the 'dirty' region.
            // This will force the form to repaint that region.
            Invalidate( e.DirtyRegion.ToRectangle(), false );
        }

        private void OnShowContextMenu( object sender, ContextMenuEventArgs e )
        {
            // A context menu is requested, typically as a result of the user
            // right-clicking in the view. Open our extended WebControlContextMenu.
            webControlContextMenu.Show( this );
        }

        private void OnShowNewView( object sender, ShowCreatedWebViewEventArgs e )
        {
            if ( ( webView == null ) || !webView.IsLive )
                return;

            if ( e.IsPopup )
            {
                // Create a WebView wrapping the view created by Awesomium.
                WebView view = new WebView( e.NewViewInstance );
                // ShowCreatedWebViewEventArgs.InitialPos indicates screen coordinates.
                Rectangle screenRect = e.Specs.InitialPosition.ToRectangle();
                // Create a new WebForm to render the new view and size it.
                WebForm childForm = new WebForm( view, screenRect.Width, screenRect.Height )
                {
                    ShowInTaskbar = false,
                    FormBorderStyle = FormBorderStyle.FixedToolWindow,
                    ClientSize = screenRect.Size != Size.Empty ? screenRect.Size : new Size( 640, 480 )
                };

                // Show the form.
                childForm.Show( this );

                if ( screenRect.Location != Point.Empty )
                    // Move it to the specified coordinates.
                    childForm.DesktopLocation = screenRect.Location;
            }
            else if ( e.IsWindowOpen || e.IsPost )
            {
                // Create a WebView wrapping the view created by Awesomium.
                WebView view = new WebView( e.NewViewInstance );
                // Create a new WebForm to render the new view and size it.
                WebForm childForm = new WebForm( view, 640, 480 );
                // Show the form.
                childForm.Show( this );
            }
            else
            {
                // Let the new view be destroyed. It is important to set Cancel to true 
                // if you are not wrapping the new view, to avoid keeping it alive along
                // with a reference to its parent.
                e.Cancel = true;

                // Load the url to the existing view.
                webView.Source = e.TargetURL;
            }
        }

        private void OnCrashed( object sender, CrashedEventArgs e )
        {
            Debug.Print( "Crashed! Status: " + e.Status );
        }

        // Called in response to JavaScript: 'window.close'.
        private void OnWindowClose( object sender, WindowCloseEventArgs e )
        {
            // If this is a child form, respect the request and close it.
            if ( ( webView != null ) && ( webView.ParentView != null ) )
                this.Close();
        }

        // This is called when the page asks to be printed, usually as result of
        // a window.print().
        private void OnPrintRequest( object sender, PrintRequestEventArgs e )
        {
            if ( !webView.IsLive )
                return;

            // You can actually call PrintToFile anytime after the ProcessCreated
            // event is fired (or the DocumentReady or LoadingFrameComplete in 
            // subsequent navigations), but you usually call it in response to
            // a print request. You should possibly display a dialog to the user
            // such as a FolderBrowserDialog, to allow them select the output directory
            // and verify printing.
            int requestId = webView.PrintToFile( @".\Prints", PrintConfig.Default );

            Debug.Print( String.Format( "Print request {0} is being printed to {1}.", requestId, @".\Prints" ) );
        }

        private void OnPrintComplete( object sender, PrintCompleteEventArgs e )
        {
            Debug.Print( String.Format( "Print request {0} completed. The following files were created:", e.RequestId ) );

            foreach ( string file in e.Files )
                Debug.Print( String.Format( "\t {0}", file ) );
        }

        private void OnPrintFailed( object sender, PrintOperationEventArgs e )
        {
            Debug.Print( String.Format( "Printing request {0} failed! Make sure the provided outputDirectory is writable.", e.RequestId ) );
        }

        private void OnJavascriptDialog( object sender, JavascriptDialogEventArgs e )
        {
            if ( !e.DialogFlags.HasFlag( JSDialogFlags.HasPromptField ) &&
                !e.DialogFlags.HasFlag( JSDialogFlags.HasCancelButton ) )
            {
                // It's a 'window.alert'
                MessageBox.Show( this, e.Message );
                e.Handled = true;
            }
        }

        private void webControlContextMenu_Opening( object sender, ContextMenuOpeningEventArgs e )
        {
            // Update the visibility of our menu items based on the
            // latest context data.
            openSeparator.Visible =
                openMenuItem.Visible = !e.Info.IsEditable && ( webView.Source != null );
        }

        private void webControlContextMenu_ItemClicked( object sender, ToolStripItemClickedEventArgs e )
        {
            if ( ( webView == null ) || !webView.IsLive )
                return;

            // We only process the menu item added by us. The WebControlContextMenu
            // will handle the predefined items.
            if ( (string)e.ClickedItem.Tag != "open" )
                return;

            WebForm webForm = new WebForm( webView.Source );
            webForm.Show( this );
        }*/
        #endregion
    }
}
