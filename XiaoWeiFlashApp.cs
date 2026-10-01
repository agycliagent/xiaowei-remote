using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace XiaoWeiRemote
{
    public class ToastForm : Form
    {
        private System.Windows.Forms.Timer closeTimer;
        private string titleText;
        private string statusText;
        private Color statusColor;

        public ToastForm(string title, string status, bool isSuccess = true)
        {
            this.titleText = title;
            this.statusText = status;
            this.statusColor = isSuccess ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(320, 85);
            this.BackColor = Color.FromArgb(17, 20, 28);
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;

            // Rounded corner region
            using (GraphicsPath path = new GraphicsPath())
            {
                int r = 16;
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(this.Width - r, 0, r, r, 270, 90);
                path.AddArc(this.Width - r, this.Height - r, r, r, 0, 90);
                path.AddArc(0, this.Height - r, r, r, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }

            // Auto close after duration (1.2s for success, 2.0s for failure)
            closeTimer = new System.Windows.Forms.Timer();
            closeTimer.Interval = isSuccess ? 1200 : 2000;
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                this.Close();
            };
            closeTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Border
            using (Pen pen = new Pen(Color.FromArgb(35, 42, 59), 2))
            {
                g.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
            }

            // Status dot
            using (SolidBrush dotBrush = new SolidBrush(this.statusColor))
            {
                g.FillEllipse(dotBrush, 24, 24, 10, 10);
            }

            // Title
            using (Font font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(243, 244, 246)))
            {
                g.DrawString(this.titleText, font, brush, 42, 18);
            }

            // Status message
            using (Font font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(this.statusColor))
            {
                g.DrawString(this.statusText, font, brush, 42, 44);
            }
        }
    }

    public static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch {}
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string scriptPath = Path.Combine(appDir, "vps_tunnel.ps1");
            if (!File.Exists(scriptPath))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                scriptPath = Path.Combine(localAppData, "XiaoWeiRemote", "vps_tunnel.ps1");
            }

            bool isSuccess = false;
            string statusMsg = "● 启动异常，未找到核心服务文件";

            if (File.Exists(scriptPath))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "powershell.exe";
                    psi.Arguments = string.Format("-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{0}\" Start", scriptPath);
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;

                    Process p = Process.Start(psi);
                    if (p != null)
                    {
                        // Give it a brief 100ms moment to confirm process initialization
                        Thread.Sleep(100);
                        if (!p.HasExited || p.ExitCode == 0)
                        {
                            isSuccess = true;
                            statusMsg = "● 服务已启动，后台守护中";
                        }
                        else
                        {
                            statusMsg = "● 守护进程启动退出，请重试";
                        }
                    }
                    else
                    {
                        statusMsg = "● 进程创建失败，可能被安全软件拦截";
                    }
                }
                catch (Exception ex)
                {
                    statusMsg = "● 启动拦截: " + ex.Message;
                }
            }

            // Show brief flash notification (Auto-fades in 1.2s, 0 extra clicks)
            ToastForm toast = new ToastForm("小薇远程", statusMsg, isSuccess);
            Application.Run(toast);
        }
    }
}
