using System.Runtime.InteropServices;
using System.Text;

namespace IWorc;

// iWorc
// I Worked On Recent Chores
internal class Program
{
    const int delay = 500;
    const int nChars = 256;
    static List<TimeRegistration> registrations = new();
    static TimeRegistration? currentRegistration;

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    static async Task Main()
    {
        var stringBuffer = new StringBuilder(nChars);

        int counter = 0;

        while (true)
        {
            var handle = GetForegroundWindow();

            if (GetWindowText(handle, stringBuffer, nChars) > 0)
                HandleWindowText(stringBuffer.ToString());
            
            await Task.Delay(delay);

            if (counter > 500)
                break;

            counter++;
        }

        Console.WriteLine();

        ParseResults(registrations);
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


    private static void ParseResults(List<TimeRegistration> registrations)
    {
        var totalTimesPerWindow = registrations
            .GroupBy(x => x.Window, x => x.ActiveTime)
            .Select(x => new TotalTimeActive(x.Key, TimeSpan.FromMilliseconds(x.Select(y => y.TotalMilliseconds).Sum())));

        foreach (var item in totalTimesPerWindow.OrderByDescending(x => x.TotalTime))
        {
            Console.WriteLine($"TotalTime: {item.TotalTime} - Window: {item.Window}");
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