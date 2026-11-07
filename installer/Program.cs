using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GhostPadSetup
{
    public class SetupForm : Form
    {
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;
        private Button installButton = null!;
        private TextBox pathBox = null!;
        private Button browseButton = null!;
        private CheckBox cbDesktop = null!;
        private CheckBox cbStartMenu = null!;
        private CheckBox cbStartup = null!;
        private CheckBox cbLaunch = null!;

        [STAThread]
        public static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }

        public SetupForm()
        {
            this.Text = "GhostPad Setup";
            this.Size = new Size(500, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 22, 27);
            this.ForeColor = Color.FromArgb(243, 244, 246);

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("GhostPadSetup.app.ico");
                if (stream != null) this.Icon = new Icon(stream);
            }
            catch { }

            BuildUI();
        }

        private void BuildUI()
        {
            // App Mascot Icon
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("GhostPadSetup.app.ico");
                if (stream != null)
                {
                    using var ico = new Icon(stream, 48, 48);
                    var iconBox = new PictureBox
                    {
                        Image = ico.ToBitmap(),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Location = new Point(24, 18),
                        Size = new Size(48, 48)
                    };
                    this.Controls.Add(iconBox);
                }
            }
            catch { }

            // Title Header
            var titleLabel = new Label
            {
                Text = "GhostPad Setup Wizard",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 244, 246),
                Location = new Point(82, 16),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);

            var descLabel = new Label
            {
                Text = "Install GhostPad desktop notepad on your computer.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(84, 46),
                AutoSize = true
            };
            this.Controls.Add(descLabel);

            // Path box
            string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "GhostPad");
            var pathTitle = new Label
            {
                Text = "Installation directory:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(209, 213, 219),
                Location = new Point(26, 92),
                AutoSize = true
            };
            this.Controls.Add(pathTitle);

            pathBox = new TextBox
            {
                Text = installPath,
                ReadOnly = false,
                BackColor = Color.FromArgb(31, 35, 43),
                ForeColor = Color.FromArgb(243, 244, 246),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(26, 114),
                Size = new Size(335, 26)
            };
            this.Controls.Add(pathBox);

            browseButton = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.FromArgb(45, 51, 62),
                ForeColor = Color.FromArgb(243, 244, 246),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(368, 113),
                Size = new Size(88, 27),
                Cursor = Cursors.Hand
            };
            browseButton.FlatAppearance.BorderColor = Color.FromArgb(75, 85, 99);
            browseButton.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog
                {
                    Description = "Select GhostPad Installation Folder",
                    UseDescriptionForTitle = true,
                    SelectedPath = Directory.Exists(pathBox.Text) ? pathBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                };
                if (fbd.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string selected = fbd.SelectedPath.Trim();
                    if (!selected.TrimEnd('\\').EndsWith("GhostPad", StringComparison.OrdinalIgnoreCase))
                    {
                        selected = Path.Combine(selected, "GhostPad");
                    }
                    pathBox.Text = selected;
                }
            };
            this.Controls.Add(browseButton);

            // Options
            cbDesktop = new CheckBox
            {
                Text = "Create a Desktop shortcut",
                Checked = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(243, 244, 246),
                Location = new Point(28, 150),
                AutoSize = true
            };
            this.Controls.Add(cbDesktop);

            cbStartMenu = new CheckBox
            {
                Text = "Create a Start Menu shortcut",
                Checked = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(243, 244, 246),
                Location = new Point(28, 176),
                AutoSize = true
            };
            this.Controls.Add(cbStartMenu);

            cbStartup = new CheckBox
            {
                Text = "Launch automatically on Windows login (Wallpaper anchor)",
                Checked = false,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(243, 244, 246),
                Location = new Point(28, 202),
                AutoSize = true
            };
            this.Controls.Add(cbStartup);

            cbLaunch = new CheckBox
            {
                Text = "Launch GhostPad when installation finishes",
                Checked = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(99, 102, 241),
                Location = new Point(28, 228),
                AutoSize = true
            };
            this.Controls.Add(cbLaunch);

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(26, 260),
                Size = new Size(430, 14),
                Visible = false
            };
            this.Controls.Add(progressBar);

            statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(26, 280),
                Size = new Size(300, 20),
                Visible = false
            };
            this.Controls.Add(statusLabel);

            // Install Button
            installButton = new Button
            {
                Text = "Install GhostPad",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(316, 285),
                Size = new Size(140, 36),
                Cursor = Cursors.Hand
            };
            installButton.FlatAppearance.BorderSize = 0;
            installButton.Click += async (s, e) => await StartInstallation();
            this.Controls.Add(installButton);
        }

        private async Task StartInstallation()
        {
            string chosenPath = pathBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(chosenPath))
            {
                chosenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "GhostPad");
            }

            installButton.Enabled = false;
            pathBox.Enabled = false;
            browseButton.Enabled = false;
            cbDesktop.Enabled = false;
            cbStartMenu.Enabled = false;
            cbStartup.Enabled = false;
            cbLaunch.Enabled = false;

            progressBar.Visible = true;
            statusLabel.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            statusLabel.Text = "Preparing installation...";

            await Task.Run(() =>
            {
                try
                {
                    // 1. Terminate existing running instance if any
                    foreach (var p in Process.GetProcessesByName("GhostPad"))
                    {
                        try { p.Kill(); p.WaitForExit(3000); } catch { }
                    }

                    // 2. Setup destination directory
                    string installDir = chosenPath;
                    Directory.CreateDirectory(installDir);

                    // 3. Extract embedded app.zip
                    UpdateStatus("Extracting files...");
                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("GhostPadSetup.app.zip"))
                    {
                        if (stream == null) throw new Exception("Embedded application package not found.");
                        using var archive = new ZipArchive(stream);
                        foreach (var entry in archive.Entries)
                        {
                            string destPath = Path.Combine(installDir, entry.FullName);
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(destPath);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                                entry.ExtractToFile(destPath, overwrite: true);
                            }
                        }
                    }

                    string exePath = Path.Combine(installDir, "GhostPad.exe");

                    // 4. Desktop Shortcut
                    if (cbDesktop.Checked)
                    {
                        UpdateStatus("Creating Desktop shortcut...");
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        CreateShortcut(Path.Combine(desktopPath, "GhostPad.lnk"), exePath, installDir, "GhostPad Desktop Notepad");
                    }

                    // 5. Start Menu Shortcut
                    if (cbStartMenu.Checked)
                    {
                        UpdateStatus("Creating Start Menu shortcut...");
                        string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "GhostPad");
                        Directory.CreateDirectory(startMenuPath);
                        CreateShortcut(Path.Combine(startMenuPath, "GhostPad.lnk"), exePath, installDir, "GhostPad Desktop Notepad");
                    }

                    // 6. Windows Startup
                    if (cbStartup.Checked)
                    {
                        UpdateStatus("Configuring Windows startup...");
                        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                        CreateShortcut(Path.Combine(startupFolder, "GhostPad.lnk"), exePath, installDir, "GhostPad Desktop Notepad");
                    }

                    // 7. Register Windows Uninstaller
                    UpdateStatus("Registering uninstaller...");
                    RegisterUninstaller(installDir, exePath);

                    // 8. Launch GhostPad
                    if (cbLaunch.Checked)
                    {
                        UpdateStatus("Launching GhostPad...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            WorkingDirectory = installDir,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() =>
                    {
                        MessageBox.Show($"Installation encountered an error: {ex.Message}", "GhostPad Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });

            this.Close();
        }

        private void UpdateStatus(string message)
        {
            this.Invoke((Action)(() =>
            {
                statusLabel.Text = message;
            }));
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string description)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Description = description;
                shortcut.IconLocation = targetPath + ",0";
                shortcut.Save();
            }
            catch { }
        }

        private static void RegisterUninstaller(string installDir, string exePath)
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\GhostPad";
                using var key = Registry.CurrentUser.CreateSubKey(keyPath);
                if (key != null)
                {
                    key.SetValue("DisplayName", "GhostPad");
                    key.SetValue("DisplayVersion", "1.0.0");
                    key.SetValue("Publisher", "nxsxhb");
                    key.SetValue("DisplayIcon", exePath);
                    key.SetValue("InstallLocation", installDir);
                    key.SetValue("UninstallString", $"cmd.exe /c taskkill /f /im GhostPad.exe & rmdir /s /q \"{installDir}\" & reg delete \"HKCU\\{keyPath}\" /f");
                    key.SetValue("NoModify", 1);
                    key.SetValue("NoRepair", 1);
                }
            }
            catch { }
        }
    }
}
