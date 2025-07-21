using LibreHardwareMonitor.Hardware;
using System.Drawing.Text;

namespace HotWatchTray
{
    public class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly Computer _computer;
        private readonly System.Threading.Timer _timer;

        public TrayApp()
        {
            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "Loading temperatures..."
            };
            var menu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Exit());
            menu.Items.Add(exitItem);
            _trayIcon.ContextMenuStrip = menu;

            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true
            };
            _computer.Open();

            _timer = new System.Threading.Timer(UpdateTemps, null, 0, 5000);
        }

        private void UpdateTemps(object? state)
        {
            var (cpu, gpu) = GetTemperatures();
            var tooltip = $"CPU: {cpu}°C, GPU: {gpu}°C";
            _trayIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            _trayIcon.Icon = CreateTempIcon(cpu, gpu);
        }

        private (string cpu, string gpu) GetTemperatures()
        {
            var cpu = "--";
            var gpu = "--";
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                switch (hardware)
                {
                    case { HardwareType: HardwareType.Cpu }:
                    {
                        cpu = GetStringFromTemperatureSensor(hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature));
                        break;
                    }
                    case { HardwareType: HardwareType.GpuNvidia }:
                    case { HardwareType: HardwareType.GpuAmd }:
                    {
                        gpu = GetStringFromTemperatureSensor(hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature));
                        break;
                    }
                }
            }

            return (cpu, gpu);
        }

        private static string GetStringFromTemperatureSensor(ISensor? sensor)
        {
            return sensor is { Value: not null } ? ((int)((float?)sensor.Value.Value).Value).ToString() : "--";
        }

        private static Icon CreateTempIcon(string cpu, string gpu)
        {
            using var bmp = new Bitmap(24, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(255, 40, 40, 40));
            using var font = new Font("Tahoma", 8, FontStyle.Bold, GraphicsUnit.Pixel);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.DrawString(cpu, font, Brushes.Orange, new RectangleF(0, 0, 24, 8), sf);
            g.DrawString(gpu, font, Brushes.Cyan, new RectangleF(0, 8, 24, 8), sf);
            var hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private void Exit()
        {
            _trayIcon.Visible = false;
            _timer.Dispose();
            _computer.Close();
            Application.Exit();
        }
    }
}
