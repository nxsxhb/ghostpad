using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GhostPad
{
    public class Program : Form
    {
        private WebView2 webView = null!;
        private bool isLocked = true;
        private int cornerRadius = 12;
        private string configPath = string.Empty;
        private string notesPath = string.Empty;
        private string appFolder = string.Empty;

        // P/Invoke for Win32 Desktop Window Ownership
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public const int GWLP_HWNDPARENT = -8;

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        [DllImport("user32.dll", EntryPoint = "SetWindowRgn")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Constants
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;

        // Removes window from Alt+Tab switcher & clips children to window region
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: ignores Alt+Tab task switcher
                cp.Style |= 0x02000000;   // WS_CLIPCHILDREN: clips drawing of child windows like WebView2
                return cp;
            }
        }

        // Prevent minimize commands
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
            {
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private static System.Threading.Mutex? appMutex;

        [STAThread]
        public static void Main()
        {
            // Scope mutex per directory so the same executable cannot be launched twice accidentally,
            // while allowing users to test standalone executables in different directories.
            string dirHash = (AppDomain.CurrentDomain.BaseDirectory ?? "").ToLowerInvariant().GetHashCode().ToString("X");
            string mutexName = "GhostPad_Mutex_" + dirHash;
            appMutex = new System.Threading.Mutex(true, mutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                // Already running from this folder
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Program());
            GC.KeepAlive(appMutex);
        }

        public Program()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.MinimumSize = new Size(220, 180);
            this.BackColor = Color.FromArgb(18, 20, 24);

            try
            {
                string iconOnDisk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconOnDisk))
                {
                    this.Icon = new Icon(iconOnDisk);
                }
                else
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    using var stream = asm.GetManifestResourceStream("GhostPad.app.ico");
                    if (stream != null) this.Icon = new Icon(stream);
                }
            }
            catch { }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? "";
            bool isPortable = File.Exists(Path.Combine(baseDir, "portable.dat")) ||
                              File.Exists(Path.Combine(baseDir, "portable.txt")) ||
                              Directory.Exists(Path.Combine(baseDir, "data"));

            if (isPortable)
            {
                appFolder = Path.Combine(baseDir, "data");
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                appFolder = Path.Combine(appData, "GhostPad");
            }
            Directory.CreateDirectory(appFolder);
            configPath = Path.Combine(appFolder, "config.json");
            notesPath = Path.Combine(appFolder, "notes.json");

            LoadSavedSettings();

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.Transparent
            };
            this.Controls.Add(webView);

            InitializeWebView();

            this.LocationChanged += (s, e) => SaveSettings();
            this.SizeChanged += (s, e) =>
            {
                ApplyWindowRegion(cornerRadius);
                SaveSettings();
            };
        }

        // Anchor GhostPad's native owner to the desktop layer (SHELLDLL_DefView).
        // This permanently glues GhostPad to the desktop layer:
        // - Foreground of the wallpaper
        // - Background of any other application window
        // - Survives Win+D (Show Desktop) naturally without timers or topmost hacks
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AnchorToDesktopLayer();
            ApplyWindowRegion(cornerRadius);
        }

        private void ApplyWindowRegion(int radius)
        {
            try
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                if (radius <= 0)
                {
                    SetWindowRgn(this.Handle, IntPtr.Zero, true);
                    if (webView != null && webView.IsHandleCreated)
                    {
                        SetWindowRgn(webView.Handle, IntPtr.Zero, true);
                    }
                    EnumChildWindows(this.Handle, (hWnd, lParam) =>
                    {
                        SetWindowRgn(hWnd, IntPtr.Zero, true);
                        return true;
                    }, IntPtr.Zero);
                }
                else
                {
                    IntPtr hRgnForm = CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, radius * 2, radius * 2);
                    SetWindowRgn(this.Handle, hRgnForm, true);

                    if (webView != null && webView.IsHandleCreated)
                    {
                        IntPtr hRgnWeb = CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, radius * 2, radius * 2);
                        SetWindowRgn(webView.Handle, hRgnWeb, true);
                    }

                    EnumChildWindows(this.Handle, (hWnd, lParam) =>
                    {
                        IntPtr hRgnChild = CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, radius * 2, radius * 2);
                        SetWindowRgn(hWnd, hRgnChild, true);
                        return true;
                    }, IntPtr.Zero);
                }
            }
            catch { }
        }

        private void AnchorToDesktopLayer()
        {
            try
            {
                IntPtr desktopOwner = GetDesktopDefView();
                if (desktopOwner != IntPtr.Zero)
                {
                    SetWindowLongPtr(this.Handle, GWLP_HWNDPARENT, desktopOwner);
                }
            }
            catch { }
        }

        private static IntPtr GetDesktopDefView()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero) return defView;

            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                IntPtr child = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (child != IntPtr.Zero)
                {
                    found = child;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found != IntPtr.Zero ? found : progman;
        }

        private async void InitializeWebView()
        {
            try
            {
                CoreWebView2Environment? env = null;
                string baseUdf = Path.Combine(appFolder, "WebViewData");

                try
                {
                    env = await CoreWebView2Environment.CreateAsync(null, baseUdf);
                }
                catch
                {
                    // If the primary data folder is locked or in use, fallback to an isolated temp profile
                    string fallbackUdf = Path.Combine(Path.GetTempPath(), $"GhostPad_WV2_{Guid.NewGuid():N}");
                    env = await CoreWebView2Environment.CreateAsync(null, fallbackUdf);
                }

                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui.html");
                if (File.Exists(filePath))
                {
                    webView.CoreWebView2.Navigate(new Uri(filePath).AbsoluteUri);
                }
                else
                {
                    string html = LoadEmbeddedHtml();
                    if (!string.IsNullOrEmpty(html))
                    {
                        webView.CoreWebView2.NavigateToString(html);
                    }
                }

                webView.NavigationCompleted += (s, e) =>
                {
                    SendInitialDataToWebView();
                    // Ensure desktop anchor is active once browser is fully ready
                    AnchorToDesktopLayer();
                    ApplyWindowRegion(cornerRadius);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2 engine: {ex.Message}\n\nPlease ensure Microsoft Edge WebView2 runtime is installed.", "GhostPad Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string LoadEmbeddedHtml()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "GhostPad.ui.html";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            catch { }
            return string.Empty;
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;
                string action = root.TryGetProperty("action", out var act) ? act.GetString() ?? "" : "";

                switch (action)
                {
                    case "moveDelta":
                        if (!isLocked)
                        {
                            int dx = root.GetProperty("dx").GetInt32();
                            int dy = root.GetProperty("dy").GetInt32();
                            this.BeginInvoke((Action)(() =>
                            {
                                this.Location = new Point(this.Left + dx, this.Top + dy);
                            }));
                        }
                        break;

                    case "resizeDelta":
                        int rdx = root.GetProperty("dx").GetInt32();
                        int rdy = root.GetProperty("dy").GetInt32();
                        this.BeginInvoke((Action)(() =>
                        {
                            int newW = Math.Max(this.MinimumSize.Width, this.Width + rdx);
                            int newH = Math.Max(this.MinimumSize.Height, this.Height + rdy);
                            this.Size = new Size(newW, newH);
                            ApplyWindowRegion(cornerRadius);
                        }));
                        break;

                    case "setCornerRadius":
                        if (root.TryGetProperty("radius", out var crElem))
                        {
                            cornerRadius = Math.Clamp(crElem.GetInt32(), 0, 32);
                            this.BeginInvoke((Action)(() =>
                            {
                                ApplyWindowRegion(cornerRadius);
                                SaveSettings();
                            }));
                        }
                        break;

                    case "savePosition":
                        this.BeginInvoke((Action)(() =>
                        {
                            SaveSettings();
                        }));
                        break;

                    case "setLocked":
                        if (root.TryGetProperty("locked", out var lk))
                        {
                            isLocked = lk.GetBoolean();
                            this.BeginInvoke((Action)(() =>
                            {
                                SaveSettings();
                            }));
                        }
                        break;

                    case "setOpacity":
                        if (root.TryGetProperty("opacity", out var op))
                        {
                            double opacityValue = Math.Clamp(op.GetDouble(), 0.15, 1.0);
                            this.BeginInvoke((Action)(() =>
                            {
                                this.Opacity = opacityValue;
                                SaveSettings();
                            }));
                        }
                        break;

                    case "saveNotesData":
                        if (root.TryGetProperty("notesJson", out var notesElem))
                        {
                            string notesRaw = notesElem.GetString() ?? "[]";
                            File.WriteAllText(notesPath, notesRaw);
                        }
                        break;

                    case "bringToFront":
                        this.BeginInvoke((Action)(() =>
                        {
                            try
                            {
                                this.BringToFront();
                            }
                            catch { }
                        }));
                        break;

                    case "openDataFolder":
                        this.BeginInvoke((Action)(() =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = appFolder,
                                    UseShellExecute = true
                                });
                            }
                            catch { }
                        }));
                        break;

                    case "close":
                        this.BeginInvoke((Action)(() =>
                        {
                            SaveSettings();
                            Application.Exit();
                        }));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
            }
        }

        private void SendInitialDataToWebView()
        {
            try
            {
                string existingNotes = "[]";
                if (File.Exists(notesPath))
                {
                    existingNotes = File.ReadAllText(notesPath);
                }

                var payload = new
                {
                    action = "initApp",
                    isLocked = isLocked,
                    opacity = this.Opacity,
                    cornerRadius = this.cornerRadius,
                    storagePath = appFolder,
                    notes = existingNotes
                };
                string json = JsonSerializer.Serialize(payload);
                webView.CoreWebView2.PostWebMessageAsString(json);
            }
            catch { }
        }

        private void LoadSavedSettings()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    int x = root.TryGetProperty("x", out var jx) ? jx.GetInt32() : 120;
                    int y = root.TryGetProperty("y", out var jy) ? jy.GetInt32() : 120;
                    int w = root.TryGetProperty("w", out var jw) ? jw.GetInt32() : 380;
                    int h = root.TryGetProperty("h", out var jh) ? jh.GetInt32() : 520;
                    double op = root.TryGetProperty("opacity", out var jop) ? jop.GetDouble() : 0.92;
                    bool lk = root.TryGetProperty("isLocked", out var jlk) ? jlk.GetBoolean() : true;
                    int cr = root.TryGetProperty("cornerRadius", out var jcr) ? jcr.GetInt32() : 12;

                    Rectangle screenBounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
                    if (x < -200 || x > screenBounds.Width - 50) x = 120;
                    if (y < -200 || y > screenBounds.Height - 50) y = 120;
                    if (w < 220) w = 380;
                    if (h < 180) h = 520;

                    this.Bounds = new Rectangle(x, y, w, h);
                    this.Opacity = Math.Clamp(op, 0.15, 1.0);
                    this.isLocked = lk;
                    this.cornerRadius = Math.Clamp(cr, 0, 32);
                    return;
                }
            }
            catch { }

            this.Bounds = new Rectangle(120, 120, 380, 520);
            this.Opacity = 0.92;
            this.isLocked = true;
            this.cornerRadius = 12;
        }

        private void SaveSettings()
        {
            try
            {
                if (this.WindowState == FormWindowState.Minimized || this.Left <= -30000 || this.Top <= -30000)
                {
                    return;
                }

                var settings = new
                {
                    x = this.Left,
                    y = this.Top,
                    w = this.Width,
                    h = this.Height,
                    opacity = this.Opacity,
                    isLocked = this.isLocked,
                    cornerRadius = this.cornerRadius
                };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch { }
        }
    }
}
