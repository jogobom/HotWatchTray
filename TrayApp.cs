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
            var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => Exit());
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
            var (cpuTemp, gpuTemp) = GetTemperatures();
            var tooltip = $"CPU: {cpuTemp}, GPU: {gpuTemp}";
            _trayIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            _trayIcon.Icon = CreateTempIcon(cpuTemp, gpuTemp);
        }

        private (string cpuTemp, string GpuTemp) GetTemperatures()
        {
            var firstCpu = _computer.Hardware.First(h => h.HardwareType == HardwareType.Cpu);
            var firstGpu = _computer.Hardware.First(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd);

            return (
                GetHardwareTemperatureAsString(firstCpu, "CPU"),
                GetHardwareTemperatureAsString(firstGpu, "GPU")
                );
        }

        private static string GetHardwareTemperatureAsString(IHardware hardware, string name)
        {
            var sensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
            return sensor is { Value: not null } ? $"{name}: {sensor.Value.Value:F1}°C" : "--";
        }

        private static Icon CreateTempIcon(string cpuTemperature, string gpuTemperature)
        {
            using var bmp = new Bitmap(24, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(255, 40, 40, 40));
            using var font = new Font("Tahoma", 8, FontStyle.Bold, GraphicsUnit.Pixel);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.DrawString(cpuTemperature, font, Brushes.Orange, new RectangleF(0, 0, 24, 8), sf);
            g.DrawString(gpuTemperature, font, Brushes.Cyan, new RectangleF(0, 8, 24, 8), sf);
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
