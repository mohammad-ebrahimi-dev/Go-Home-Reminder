using System;
using System.Diagnostics;
using System.IO;

namespace WorkTime
{
    class Program
    {
        static void Main()
        {
            CalculateLeaveTime();
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void CalculateLeaveTime()
        {
            Console.WriteLine("Enter your arrival time (e.g. 8:30 or 08:30): ");
            string input = Console.ReadLine()?.Trim();

            if (!TryParseTime(input, out int hour, out int minute))
            {
                Console.WriteLine("Invalid format! Use hh:mm (example: 09:15)");
                return;
            }

            DateTime arrival = DateTime.Today.AddHours(hour).AddMinutes(minute);
            DateTime leaveTime = arrival.AddHours(8).AddMinutes(50);

            int displayHour = leaveTime.Hour % 12;
            if (displayHour == 0) displayHour = 12;

            Console.WriteLine();
            Console.WriteLine($"You can leave at {displayHour:D2}:{leaveTime.Minute:D2} PM");
            Console.WriteLine();

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(desktop, "Go_Home.txt");

            if (!File.Exists(filePath))
            {
                File.WriteAllText($"{desktop}\\Go_Home.txt", "Work is over!\r\nTime to go home!\r\n\r\nIt's time to relax! 😊️");
                Console.WriteLine("Reminder file created on desktop: Go_Home.txt");
            }

            RunCmd("schtasks /delete /tn \"LeaveReminder\" /f >nul 2>&1");

            DateTime taskTime = leaveTime;
            if (taskTime <= DateTime.Now.AddMinutes(-2))
                taskTime = leaveTime.AddDays(1);

            string time24 = taskTime.ToString("HH:mm");
            string date = $"{taskTime:MM/dd/yyyy}"; 

            string command =
                $"schtasks /create /tn \"LeaveReminder\" " +
                $" /tr \"\\\"C:\\Windows\\notepad.exe\\\" \\\"{filePath}\\\"\" " +
                $" /sc once /sd {date} /st {time24} /f";

            string result = RunCmd(command);

            if (string.IsNullOrWhiteSpace(result) || result.Contains("SUCCESS") || result.Contains("موفقیت"))
            {
                Console.WriteLine($"Reminder successfully set for {displayHour:D2}:{taskTime.Minute:D2} {(taskTime.Hour >= 12 ? "PM" : "AM")}");
            }
            else
            {
                Console.WriteLine("Failed to create task:");
                Console.WriteLine(result);
            }
        }

        static bool TryParseTime(string s, out int h, out int m)
        {
            h = m = 0;
            if (string.IsNullOrEmpty(s)) return false;
            var parts = s.Split(':');
            if (parts.Length != 2) return false;
            return int.TryParse(parts[0], out h) &&
                   int.TryParse(parts[1], out m) &&
                   h >= 0 && h <= 23 && m >= 0 && m <= 59;
        }

        static string RunCmd(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return string.IsNullOrWhiteSpace(error) ? output.Trim() : "ERROR: " + error.Trim();
            }
            catch (Exception ex)
            {
                return "EXCEPTION: " + ex.Message;
            }
        }
    }
}