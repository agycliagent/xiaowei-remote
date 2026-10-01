using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace XiaoWeiSimpleSetup
{
    public class SetupWizardForm : Form
    {
        private Panel headerPanel;
        private Label lblBannerTitle;
        private Label lblBannerSubtitle;
        private Panel contentPanel;
        private Panel bottomPanel;

        // Page 1 Controls
        private Label lblIntro;
        private CheckBox chkDesktopShortcut;
        private Button btnInstall;
        private Button btnCancel;

        // Page 2 Controls (Installing)
        private Label lblStatus;
        private ProgressBar progressBar;
        private Label lblDetail;

        // Page 3 Controls (Finish)
        private Label lblFinishTitle;
        private Label lblFinishDesc;
        private CheckBox chkLaunchNow;
        private Button btnFinish;

        private string installDir;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public SetupWizardForm()
        {
            try { SetProcessDPIAware(); } catch {}
            this.installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XiaoWeiRemote");

            InitializeComponent();
            ShowStep1();
        }

        private void InitializeComponent()
        {
            this.Text = "小薇远程 安装程序";
            this.ClientSize = new Size(500, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

            // 1. Header (Top)
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 75;
            headerPanel.BackColor = Color.FromArgb(20, 24, 34);

            lblBannerTitle = new Label();
            lblBannerTitle.Text = "小薇远程 (XiaoWei Remote)";
            lblBannerTitle.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            lblBannerTitle.ForeColor = Color.White;
            lblBannerTitle.Location = new Point(24, 15);
            lblBannerTitle.AutoSize = true;

            lblBannerSubtitle = new Label();
            lblBannerSubtitle.Text = "Windows 跨局域网高可用 SSH 守护管理器 · 全自动一键安装";
            lblBannerSubtitle.Font = new Font("Microsoft YaHei UI", 8.5F);
            lblBannerSubtitle.ForeColor = Color.FromArgb(160, 174, 192);
            lblBannerSubtitle.Location = new Point(25, 42);
            lblBannerSubtitle.AutoSize = true;

            headerPanel.Controls.Add(lblBannerTitle);
            headerPanel.Controls.Add(lblBannerSubtitle);
            this.Controls.Add(headerPanel);

            // 2. Bottom Action Bar (Bottom)
            bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 55;
            bottomPanel.BackColor = Color.FromArgb(235, 238, 242);
            this.Controls.Add(bottomPanel);

            // 3. Content Panel (Middle)
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(248, 249, 250);
            contentPanel.Padding = new Padding(30, 25, 30, 20);
            this.Controls.Add(contentPanel);

            // Set Z-order
            headerPanel.SendToBack();
            bottomPanel.SendToBack();
            contentPanel.BringToFront();
        }

        private void ShowStep1()
        {
            contentPanel.Controls.Clear();
            bottomPanel.Controls.Clear();

            lblIntro = new Label();
            lblIntro.Text = "欢迎使用小薇远程一键安装向导。将自动部署运行环境并配置反向连接。";
            lblIntro.Location = new Point(28, 30);
            lblIntro.Size = new Size(440, 24);
            lblIntro.ForeColor = Color.FromArgb(55, 65, 81);
            lblIntro.Font = new Font("Microsoft YaHei UI", 9F);
            contentPanel.Controls.Add(lblIntro);

            chkDesktopShortcut = new CheckBox();
            chkDesktopShortcut.Text = "创建桌面快捷方式（带有经典终端黑色图标）";
            chkDesktopShortcut.Checked = true;
            chkDesktopShortcut.Font = new Font("Microsoft YaHei UI", 9.5F);
            chkDesktopShortcut.ForeColor = Color.FromArgb(17, 24, 39);
            chkDesktopShortcut.Location = new Point(28, 75);
            chkDesktopShortcut.AutoSize = true;
            chkDesktopShortcut.Cursor = Cursors.Hand;
            contentPanel.Controls.Add(chkDesktopShortcut);

            btnInstall = new Button();
            btnInstall.Text = "立即安装(I)";
            btnInstall.Location = new Point(280, 12);
            btnInstall.Size = new Size(100, 32);
            btnInstall.BackColor = Color.FromArgb(16, 185, 129);
            btnInstall.ForeColor = Color.Black;
            btnInstall.FlatStyle = FlatStyle.System;
            btnInstall.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            btnInstall.Cursor = Cursors.Hand;
            btnInstall.Click += (s, e) => StartInstall();

            btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(390, 12);
            btnCancel.Size = new Size(85, 32);
            btnCancel.BackColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.System;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnInstall);
            bottomPanel.Controls.Add(btnCancel);
        }

        private void StartInstall()
        {
            contentPanel.Controls.Clear();
            bottomPanel.Controls.Clear();

            lblStatus = new Label();
            lblStatus.Text = "正在安装小薇远程...";
            lblStatus.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(17, 24, 39);
            lblStatus.Location = new Point(28, 25);
            lblStatus.AutoSize = true;
            contentPanel.Controls.Add(lblStatus);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(28, 60);
            progressBar.Size = new Size(440, 22);
            progressBar.Style = ProgressBarStyle.Continuous;
            contentPanel.Controls.Add(progressBar);

            lblDetail = new Label();
            lblDetail.Text = "正在解压文件...";
            lblDetail.Font = new Font("Microsoft YaHei UI", 8.5F);
            lblDetail.ForeColor = Color.FromArgb(107, 114, 128);
            lblDetail.Location = new Point(28, 92);
            lblDetail.Size = new Size(440, 20);
            contentPanel.Controls.Add(lblDetail);

            bool makeShortcut = chkDesktopShortcut.Checked;

            Thread t = new Thread(() => {
                try
                {
                    UpdateProgress(20, "正在创建安装文件夹...", "路径: " + installDir);
                    if (!Directory.Exists(installDir))
                    {
                        Directory.CreateDirectory(installDir);
                    }

                    UpdateProgress(40, "正在释放应用程序文件...", "释放核心程序与配置文件");
                    string targetExe = Path.Combine(installDir, "小薇远程.exe");
                    string targetScript = Path.Combine(installDir, "vps_tunnel.ps1");
                    string targetIco = Path.Combine(installDir, "app_classic_dark.ico");
                    string startBat = Path.Combine(installDir, "start.bat");
                    string stopBat = Path.Combine(installDir, "stop.bat");
                    string statusBat = Path.Combine(installDir, "status.bat");
                    string sshZip = Path.Combine(installDir, "OpenSSH-Win64.zip");

                    ExtractResource("小薇远程.exe", targetExe);
                    ExtractResource("vps_tunnel.ps1", targetScript);
                    ExtractResource("app_classic_dark.ico", targetIco);
                    ExtractResource("start.bat", startBat);
                    ExtractResource("stop.bat", stopBat);
                    ExtractResource("status.bat", statusBat);
                    ExtractResource("OpenSSH-Win64.zip", sshZip);

                    UpdateProgress(65, "正在配置 Windows OpenSSH 远程服务...", "检测并确保 OpenSSH Server 服务就绪");
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = "powershell.exe";
                        psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"try { if (-not (Get-Service sshd -ErrorAction SilentlyContinue)) { Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0 -ErrorAction SilentlyContinue | Out-Null }; Start-Service sshd -ErrorAction SilentlyContinue; Set-Service sshd -StartupType Automatic -ErrorAction SilentlyContinue; New-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -ErrorAction SilentlyContinue | Out-Null } catch {}\"";
                        psi.WindowStyle = ProcessWindowStyle.Hidden;
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process p = Process.Start(psi);
                        if (p != null) p.WaitForExit(15000);
                    }
                    catch {}

                    if (makeShortcut)
                    {
                        UpdateProgress(90, "正在生成桌面快捷方式...", "生成「小薇远程」桌面图标");
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string lnk = Path.Combine(desktop, "小薇远程.lnk");
                        CreateShortcut(lnk, targetExe, installDir, targetIco, "小薇远程 (XiaoWei Remote)");
                    }

                    UpdateProgress(100, "安装完成！", "服务环境与图标已成功部署");
                    Thread.Sleep(300);

                    this.Invoke((Action)(() => ShowStepFinish()));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() => {
                        MessageBox.Show("安装失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ShowStep1();
                    }));
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void UpdateProgress(int val, string text, string detail)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => UpdateProgress(val, text, detail)));
                return;
            }
            progressBar.Value = val;
            lblStatus.Text = text;
            lblDetail.Text = detail;
        }

        private void ShowStepFinish()
        {
            contentPanel.Controls.Clear();
            bottomPanel.Controls.Clear();

            lblFinishTitle = new Label();
            lblFinishTitle.Text = "🎉 安装完成！";
            lblFinishTitle.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            lblFinishTitle.ForeColor = Color.FromArgb(16, 140, 90);
            lblFinishTitle.Location = new Point(28, 18);
            lblFinishTitle.AutoSize = true;
            contentPanel.Controls.Add(lblFinishTitle);

            lblFinishDesc = new Label();
            lblFinishDesc.Text = "小薇远程 已成功安装且环境已自动就绪。\n桌面已生成黑色「小薇远程」图标。";
            lblFinishDesc.Font = new Font("Microsoft YaHei UI", 9.5F);
            lblFinishDesc.ForeColor = Color.FromArgb(75, 85, 99);
            lblFinishDesc.Location = new Point(30, 48);
            lblFinishDesc.Size = new Size(440, 40);
            contentPanel.Controls.Add(lblFinishDesc);

            chkLaunchNow = new CheckBox();
            chkLaunchNow.Text = "立即启动小薇远程服务（推荐）";
            chkLaunchNow.Checked = true;
            chkLaunchNow.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            chkLaunchNow.ForeColor = Color.FromArgb(17, 24, 39);
            chkLaunchNow.Location = new Point(30, 95);
            chkLaunchNow.AutoSize = true;
            chkLaunchNow.Cursor = Cursors.Hand;
            contentPanel.Controls.Add(chkLaunchNow);

            btnFinish = new Button();
            btnFinish.Text = "完成";
            btnFinish.Location = new Point(390, 12);
            btnFinish.Size = new Size(85, 32);
            btnFinish.BackColor = Color.White;
            btnFinish.FlatStyle = FlatStyle.System;
            btnFinish.Cursor = Cursors.Hand;
            btnFinish.Click += (s, e) => {
                if (chkLaunchNow.Checked)
                {
                    try
                    {
                        string targetExe = Path.Combine(installDir, "小薇远程.exe");
                        if (File.Exists(targetExe))
                        {
                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = targetExe,
                                WorkingDirectory = installDir,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch {}
                }
                this.Close();
            };
            bottomPanel.Controls.Add(btnFinish);
        }

        private static void ExtractResource(string resName, string outputPath)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string fullResName = null;
            foreach (string name in asm.GetManifestResourceNames())
            {
                if (name.EndsWith(resName, StringComparison.OrdinalIgnoreCase))
                {
                    fullResName = name;
                    break;
                }
            }

            if (fullResName != null)
            {
                using (Stream stream = asm.GetManifestResourceStream(fullResName))
                {
                    if (stream != null)
                    {
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        File.WriteAllBytes(outputPath, buffer);
                    }
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workDir, string iconPath, string desc)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                    Type scType = shortcut.GetType();
                    scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                    scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workDir });
                    scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { desc });
                    if (File.Exists(iconPath))
                    {
                        scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath + ",0" });
                    }
                    scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
                }
            }
            catch {}
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWizardForm());
        }
    }
}
