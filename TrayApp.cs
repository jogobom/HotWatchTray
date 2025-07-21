using LibreHardwareMonitor.Hardware;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;

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
            var tooltip = GetTemperatures(out var cpu, out var gpu);
            _trayIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            _trayIcon.Icon = CreateTempIcon(cpu, gpu);
        }

        private string GetTemperatures(out string cpu, out string gpu)
        {
            float? cpuValue = null;
            float? gpuValue = null;
            cpu = "--";
            gpu = "--";
            var cpuTemp = "CPU: N/A";
            var gpuTemp = "GPU: N/A";
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                switch (hardware)
                {
                    case { HardwareType: HardwareType.Cpu }:
                    {
                        var sensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        if (sensor is { Value: not null })
                        {
                            cpuValue = sensor.Value.Value;
                            cpu = ((int)cpuValue.Value).ToString();
                            cpuTemp = $"CPU: {sensor.Value.Value:F1}°C";
                        }
                        break;
                    }
                    case { HardwareType: HardwareType.GpuNvidia }:
                    case { HardwareType: HardwareType.GpuAmd }:
                    {
                        var sensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        if (sensor is { Value: not null })
                        {
                            gpuValue = sensor.Value.Value;
                            gpu = ((int)gpuValue.Value).ToString();
                            gpuTemp = $"GPU: {sensor.Value.Value:F1}°C";
                        }
                        break;
                    }
                }
            }
            return $"{cpuTemp}, {gpuTemp}";
        }

        private Icon CreateTempIcon(string cpu, string gpu)
        {
            // 16x16 icon size for tray
            using var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            // Set a dark grey background
            g.Clear(Color.FromArgb(255, 40, 40, 40));
            using var font = new Font("Tahoma", 8, FontStyle.Bold, GraphicsUnit.Pixel);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            // Use bright colors for visibility on dark background
            g.DrawString(cpu, font, Brushes.Orange, new RectangleF(0, 0, 16, 8), sf);
            g.DrawString(gpu, font, Brushes.Cyan, new RectangleF(0, 8, 16, 8), sf);
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
