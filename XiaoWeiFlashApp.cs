using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XiaoWeiRemote
{
    public class ToastForm : Form
    {
        private Timer closeTimer;
        private string titleText;
        private string statusText;

        public ToastForm(string title, string status)
        {
            this.titleText = title;
            this.statusText = status;

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

            // Close after 1.2 seconds
            closeTimer = new Timer();
            closeTimer.Interval = 1200;
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

            // Green status dot
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                g.FillEllipse(dotBrush, 24, 24, 10, 10);
            }

            // Title
            using (Font font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(243, 244, 246)))
            {
                g.DrawString(this.titleText, font, brush, 42, 18);
            }

            // Status message in green
            using (Font font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(16, 185, 129)))
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

            // Launch or verify background service
            try
            {
                if (File.Exists(scriptPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "powershell.exe";
                    psi.Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{0}\" Start", scriptPath);
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                }
            }
            catch {}

            // Show brief 1.2s flash notification
            ToastForm toast = new ToastForm("小薇远程", "● 服务已启动，后台守护中");
            Application.Run(toast);
        }
    }
}
