using Microsoft.Win32;
using mshtml;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WebViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string ConfigFileName = "main.conf";
        private CConfiguration _conf;

        public MainWindow()
        {
            InitializeComponent();

            SetBrowserCompatibilityMode();

            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            this.Topmost = true;
            this.Top = 0;
            this.Left = 0;

            _conf = CConfiguration.Read(ConfigFileName);
            _lastUse = DateTime.Now;

            InitButtons();
            InitHandlers();
        }

        private void SetBrowserCompatibilityMode()
        {
            //http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx
            // FeatureControl settings are per-process
            var fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            // make sure we're not running inside Visual Studio
            if (String.Compare(fileName, "devenv.exe", true) == 0) {
                return;
            }

            using (var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                RegistryKeyPermissionCheck.ReadWriteSubTree
                )) {
                // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
                UInt32 mode = 10000; // 10000; or 11000 if IE11 is explicitly supported as well
                key.SetValue(fileName, mode, RegistryValueKind.DWord);
            }

            using (var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_NINPUT_LEGACYMODE",
                RegistryKeyPermissionCheck.ReadWriteSubTree
                )) {
                // disable Legacy Input Model
                UInt32 mode = 0;
                key.SetValue(fileName, mode, RegistryValueKind.DWord);
            }

        }

        // чтобы тачскрин не мог сдвигать окно
        private void ManipulationBoundaryFeedbackHandler(object sender, ManipulationBoundaryFeedbackEventArgs e)
        { e.Handled = true; }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift) && e.Key == Key.C) {
                EnterPasswordWindow chPassWin = new EnterPasswordWindow(this);
                if (chPassWin.ShowDialog() == true) {
                    this.Close();
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void BlockKeys(KeyEventArgs e)
        { e.Handled = true; }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitBrowser();

            _clock = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate {
                lock (_lastUseLocker) {
                    if ((DateTime.Now - _lastUse).Seconds > _conf.ResetDelay) {
                        ResetBrowser();
                        _lastUse = DateTime.Now;
                    }
                }
            }, this.Dispatcher);

            ProtectKeyBoard();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
            this.Activate();
            this.Focus();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UnProtectKeyBoard();
        }

        internal bool CheckPassword(string password)
        {
            return _conf.ExitPassword.Equals(password);
        }

        #region B R O W S E R

        private WebBrowser _browser = null;
        private WebBrowserHostUIHandler _wbHandler;
        private DispatcherTimer _clock;
        private DateTime _lastUse;
        private object _lastUseLocker = new object();


        private void InitBrowser()
        {
            _browser = new WebBrowser();
            _browser.Margin = new Thickness(0);
            _browser.ClipToBounds = true;
            _browser.Focusable = true;
            _browser.Cursor = Cursors.None;
            _browser.IsHitTestVisible = false;

            _wbHandler = new WebBrowserHostUIHandler(_browser);
            _wbHandler.IsWebBrowserContextMenuEnabled = false;
            _wbHandler.ScriptErrorsSuppressed = true;
            _wbHandler.OpenNewWindowSuppressed = true;

            _browser.LoadCompleted += _browser_LoadCompleted;
            _browser.StylusDown += _browser_UserAction;
            _browser.StylusMove += _browser_UserAction;
            _browser.StylusUp += _browser_UserAction;

            _browser.TouchDown += _browser_UserAction;
            _browser.TouchMove += _browser_UserAction;
            _browser.TouchUp += _browser_UserAction;

            _browser.KeyDown += _browser_UserAction;
            _browser.KeyUp += _browser_UserAction;

            _browser.AddHandler(UIElement.MouseWheelEvent, _mouseWeelHandler, true);
            _browser.AddHandler(UIElement.MouseDownEvent, _mouseDownHandler, true);
            _browser.AddHandler(UIElement.MouseMoveEvent, _mouseMoveHandler, true);
            _browser.AddHandler(UIElement.MouseUpEvent, _mouseUpHandler, true);

            _browser.AddHandler(UIElement.TouchMoveEvent, _touchMoveHandler, true);
            _browser.AddHandler(UIElement.TouchUpEvent, _touchUpHandler, true);
            _browser.AddHandler(UIElement.TouchDownEvent, _touchDownHandler, true);

            mainContent.Children.Add(_browser);

            GoHome();
        }

        private void ReleaseBrowser()
        {
            if (_browser == null) {
                return;
            }

            _browser.LoadCompleted -= _browser_LoadCompleted;
            _browser.StylusDown -= _browser_UserAction;
            _browser.StylusMove -= _browser_UserAction;
            _browser.StylusUp -= _browser_UserAction;

            _browser.TouchDown -= _browser_UserAction;
            _browser.TouchMove -= _browser_UserAction;
            _browser.TouchUp -= _browser_UserAction;

            _browser.KeyDown -= _browser_UserAction;
            _browser.KeyUp -= _browser_UserAction;

            _browser.RemoveHandler(UIElement.MouseWheelEvent, _mouseWeelHandler);
            _browser.RemoveHandler(UIElement.MouseDownEvent, _mouseDownHandler);
            _browser.RemoveHandler(UIElement.MouseMoveEvent, _mouseMoveHandler);
            _browser.RemoveHandler(UIElement.MouseUpEvent, _mouseUpHandler);

            _browser.RemoveHandler(UIElement.TouchMoveEvent, _touchMoveHandler);
            _browser.RemoveHandler(UIElement.TouchUpEvent, _touchUpHandler);
            _browser.RemoveHandler(UIElement.TouchDownEvent, _touchDownHandler);

            if (_wbHandler != null) {
                _wbHandler = null;
            }

            mainContent.Children.Clear();
            _browser = null;
        }

        private void UpdateTimer()
        {
            lock (_lastUseLocker) {
                _lastUse = DateTime.Now;
            }
        }

        private void ResetBrowser()
        {
            try {
                ReleaseBrowser();
                InitBrowser();
            }
            catch { }
        }

        private void GoBack()
        {
            if (_browser == null) {
                return;
            }

            if (_browser.CanGoBack) {
                _browser.GoBack();
            }
        }

        private void GoForward()
        {
            if (_browser == null) {
                return;
            }

            if (_browser.CanGoForward) {
                _browser.GoForward();
            }
        }

        private void GoHome()
        {
            if (_browser == null) {
                return;
            }

            _browser.Navigate(_conf.Url);
        }

        #region EVENTHandlers

        private Point pt1, pt2 = new Point();
        private int firstTouchId = -1;

        private MouseWheelEventHandler _mouseWeelHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;

        private EventHandler<TouchEventArgs> _touchMoveHandler;
        private EventHandler<TouchEventArgs> _touchDownHandler;
        private EventHandler<TouchEventArgs> _touchUpHandler;

        private void InitHandlers()
        {
            _mouseWeelHandler = new MouseWheelEventHandler(BrowserOnMouseWheel);
            _mouseUpHandler = new MouseButtonEventHandler(BrowserMouseUp);
            _mouseDownHandler = new MouseButtonEventHandler(BrowserMouseDown);
            _mouseMoveHandler = new MouseEventHandler(BrowserMouseMove);

            _touchMoveHandler = new EventHandler<TouchEventArgs>(BrowserTouchMove);
            _touchUpHandler = new EventHandler<TouchEventArgs>(BrowserTouchUp);
            _touchDownHandler = new EventHandler<TouchEventArgs>(BrowserTouchDown);
        }


        #region MSHTML events

        private void _browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            mshtml.HTMLDocumentEvents2_Event doc = ((mshtml.HTMLDocumentEvents2_Event)_browser.Document);
            doc.onmousedown += new HTMLDocumentEvents2_onmousedownEventHandler(doc_UserAction);
            doc.onmousemove += new HTMLDocumentEvents2_onmousemoveEventHandler(doc_UserAction);
            doc.onclick += new HTMLDocumentEvents2_onclickEventHandler(doc_UserClickAction);
            doc.onmousewheel += new HTMLDocumentEvents2_onmousewheelEventHandler(doc_UserClickAction);
            UpdateButtons();
        }

        private void doc_UserAction(IHTMLEventObj pEvtObj)
        { UpdateTimer(); }

        private bool doc_UserClickAction(IHTMLEventObj pEvtObj)
        { UpdateTimer(); return true; }

        private void _browser_UserAction(object sender, EventArgs e)
        { UpdateTimer(); }

        #endregion

        #region MouseEventHandlers

        private void BrowserOnMouseWheel(object sender, MouseWheelEventArgs e)
        { UpdateTimer(); }

        private void BrowserMouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateTimer();

            WebBrowser _canvas = (WebBrowser)sender as WebBrowser;
            if (_canvas != null) {
                e.MouseDevice.Capture(_canvas);
                pt1 = e.GetPosition(_canvas);
            }
        }

        private void BrowserMouseMove(object sender, MouseEventArgs e)
        {
            UpdateTimer();
            WebBrowser _canvas = (WebBrowser)sender as WebBrowser;
            if (_canvas != null) {
                pt2 = e.GetPosition(_canvas);
            }
        }

        private void BrowserMouseUp(object sender, MouseButtonEventArgs e)
        {
            UpdateTimer();
            WebBrowser _canvas = (WebBrowser)sender as WebBrowser;
            if (_canvas != null && e.MouseDevice.Captured == _canvas) {
                _canvas.ReleaseMouseCapture();
                if (pt1.X - pt2.X > 0.7 * this.Width) {
                    GoForward();
                } else if (pt2.X - pt1.X > 0.7 * this.Width) {
                    GoBack();
                }
            }
        }

        #endregion

        #region TouchEventHandlers

        private void BrowserTouchDown(object sender, TouchEventArgs e)
        {
            UpdateTimer();

            WebBrowser _canvas = (WebBrowser)sender as WebBrowser;
            if (_canvas != null) {
                e.TouchDevice.Capture(_canvas);
                // Record the ID of the first touch point if it hasn't been recorded.
                if (firstTouchId == -1) {
                    firstTouchId = e.TouchDevice.Id;
                }
            }
        }

        private void BrowserTouchMove(object sender, TouchEventArgs e)
        {
            UpdateTimer();
            WebBrowser _canvas = (WebBrowser)sender as WebBrowser;
            if (_canvas != null) {
                TouchPoint tp = e.GetTouchPoint(_canvas);
                // This is the first touch point; just record its position.
                if (e.TouchDevice.Id == firstTouchId) {
                    pt1.X = tp.Position.X;
                    pt1.Y = tp.Position.Y;
                }
                // This is not the first touch point; draw a line from the first point to this one.
                else if (e.TouchDevice.Id != firstTouchId) {
                    pt2.X = tp.Position.X;
                    pt2.Y = tp.Position.Y;
                }
            }
        }

        private void BrowserTouchUp(object sender, TouchEventArgs e)
        {
            UpdateTimer();
            WebBrowser _canvas = (WebBrowser)sender as WebBrowser;
            if (_canvas != null && e.TouchDevice.Captured == _canvas) {
                _canvas.ReleaseTouchCapture(e.TouchDevice);
                if (pt1.X - pt2.X > 0.7 * this.Width) {
                    GoForward();
                } else if (pt2.X - pt1.X > 0.7 * this.Width) {
                    GoBack();
                }
            }
        }

        #endregion

        #endregion

        #endregion


        #region USER INTERFACE

        private bool _showButtons = true;

        private void InitButtons()
        {
            _showButtons = false;
            if (_conf.Theme.Length > 0 && !_conf.Theme.Equals("none", StringComparison.CurrentCultureIgnoreCase)) {
                string themeDir = string.Format("media\\themes\\{0}", _conf.Theme);

                if (Directory.Exists(themeDir)) {
                    string mainFile = string.Format("{0}\\main.png", themeDir);
                    string backFile = string.Format("{0}\\back.png", themeDir);
                    string forwardFile = string.Format("{0}\\forward.png", themeDir);

                    _showButtons = File.Exists(mainFile) && File.Exists(mainFile) && File.Exists(mainFile);

                    if (_showButtons) {
                        btBack.Background = new ImageBrush(new BitmapImage(new Uri(backFile, UriKind.Relative)));
                        btForward.Background = new ImageBrush(new BitmapImage(new Uri(forwardFile, UriKind.Relative)));
                        btMain.Background = new ImageBrush(new BitmapImage(new Uri(mainFile, UriKind.Relative)));
                    }
                }
            }

            if (_showButtons) {
                btBack.Visibility = Visibility.Visible;
                btForward.Visibility = Visibility.Visible;
                btMain.Visibility = Visibility.Visible;
                bottomMenu.Visibility = Visibility.Visible;
                mainContent.Margin = new Thickness(0, 0, 0, btBack.Height + 20);
            } else {
                btBack.Visibility = Visibility.Hidden;
                btForward.Visibility = Visibility.Hidden;
                btMain.Visibility = Visibility.Hidden;
                bottomMenu.Visibility = Visibility.Hidden;
                mainContent.Margin = new Thickness(0);
            }
        }

        private void UpdateButtons()
        {
            if (!_showButtons) {
                return;
            }

            btBack.Visibility = _browser.CanGoBack ? Visibility.Visible : Visibility.Hidden;
            btForward.Visibility = _browser.CanGoForward ? Visibility.Visible : Visibility.Hidden;
        }

        private void btBack_Click(object sender, RoutedEventArgs e)
        { GoBack(); }

        private void btMain_Click(object sender, RoutedEventArgs e)
        { GoHome(); }

        private void btForward_Click(object sender, RoutedEventArgs e)
        { GoForward(); }

        #endregion


        #region Protect KeyBoard

        private static void ProtectKeyBoard()
        {
            //File.Create("info.dat");
            KeyBlocker.ProtectKeyBoard();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        { UpdateTimer(); }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        { UpdateTimer(); }

        private void Window_TouchDown(object sender, TouchEventArgs e)
        { UpdateTimer(); }

        public static void UnProtectKeyBoard()
        {
            //File.Delete("info.dat");
            KeyBlocker.UnProtectKeyBoard();
        }

        #endregion

    }
}
