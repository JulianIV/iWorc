using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace IWorc.WPF
{
    /// <summary>
    /// iWorc
    /// I Worked On Recent Chores
    /// </summary>
    public partial class App : Application
    {
        const int delay = 500;
        const int nChars = 256;
        static List<TimeRegistration> registrations = new();
        static TimeRegistration currentRegistration;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);


        protected override void OnStartup(StartupEventArgs e)
        {
            var icon = new NotifyIcon()
            {
                Icon = ProjectResources.iWorc_icon,
                Visible = true
            };
            icon.MouseClick += new MouseEventHandler(TrayIconOnClick);
            icon.MouseDoubleClick += new MouseEventHandler(CloseClick);
            
            var stringBuffer = new StringBuilder(nChars);

            base.OnStartup(e);

            while (true)
            {
                var handle = GetForegroundWindow();

                if (GetWindowText(handle, stringBuffer, nChars) > 0)
                    HandleWindowText(stringBuffer.ToString());

                Task.Delay(delay).GetAwaiter().GetResult();
            }

            void TrayIconOnClick(object? sender, EventArgs e)
                => MessageBox.Show(ParseResults(registrations));

            void CloseClick(object? sender, EventArgs e)
                => Environment.Exit(0);
        }

        private static void HandleWindowText(string activeWindow)
        {
            var time = DateTimeOffset.Now;

            if (currentRegistration == null)
            {
                var newRegistration = new TimeRegistration(activeWindow, time);
                currentRegistration = newRegistration;
                Console.WriteLine($"New active window is: {activeWindow}, startTime: {time}");
                Console.WriteLine();

                return;
            }

            if (currentRegistration.Window != activeWindow)
            {
                currentRegistration.EndTime = time;
                Console.WriteLine($"New active window is: {activeWindow}, startTime: {time}");
                Console.WriteLine($"Previous window was active for: {currentRegistration.ActiveTime}");
                Console.WriteLine();

                var newRegistration = new TimeRegistration(activeWindow, time);
                currentRegistration = newRegistration;
                registrations.Add(newRegistration);
            }
        }

        private static string ParseResults(List<TimeRegistration> registrations)
        {
            var totalTimesPerWindow = registrations
                .GroupBy(x => x.Window, x => x.ActiveTime)
                .Select(x => new TotalTimeActive(x.Key, TimeSpan.FromMilliseconds(x.Select(y => y.TotalMilliseconds).Sum())));

            var sb = new StringBuilder();
            foreach (var item in totalTimesPerWindow.OrderByDescending(x => x.TotalTime))
            {
                var message = $"TotalTime: {item.TotalTime} - Window: {item.Window}";
                Console.WriteLine(message);
                sb.AppendLine(message);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    class TimeRegistration
    {
        public TimeRegistration(string window, DateTimeOffset startTime)
        {
            Window = window;
            StartTime = startTime;
        }

        public string Window { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }

        public TimeSpan ActiveTime
            => EndTime.HasValue ? EndTime.Value - StartTime : DateTimeOffset.Now - StartTime;
    }

    record TotalTimeActive(string Window, TimeSpan TotalTime);

}
