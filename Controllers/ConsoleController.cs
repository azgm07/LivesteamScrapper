using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LivesteamScrapper.Controllers
{
    public static class ConsoleController
    {
        //private static readonly ILogger<Controller> _logger;

        private static bool IsRunning;

        private static string lineBreak = "------------------------------";

        public static ChatConsole Chat { get; set; } = new ChatConsole();
        public static ViewersConsole Viewers { get; set; } = new ViewersConsole();
        public static GameConsole CurrentGame { get; set; } = new GameConsole();

        public static Task StartConsole(int delaySeconds = 30)
        {
            Console.WriteLine(string.Concat("\n", "Console Started ", lineBreak, "\n"));
            IsRunning = true;
            Thread.Sleep(delaySeconds * 1000);
            while (IsRunning)
            {
                ShowGameLog();
                ShowViewersLog();
                ShowChatLog();
                Console.WriteLine(lineBreak);
                Thread.Sleep(delaySeconds * 1000);
            }
            return Task.CompletedTask;
        }

        public static void StopConsole()
        {
            Console.WriteLine(string.Concat("\n", "Console Stopped ", lineBreak, "\n"));
            IsRunning = false;
        }

        private static void ShowChatLog()
        {
            ChatConsole chatConsole = Chat;
            if (chatConsole.MessagesFound != -1)
            {
                Console.WriteLine($"Messages found in page: {chatConsole.MessagesFound}");
            }
            if (!string.IsNullOrEmpty(chatConsole.LastMessage))
            {
                Console.WriteLine($"Last message found: {chatConsole.LastMessage}");
            }
        }

        private static void ShowViewersLog()
        {
            ViewersConsole viewersConsole = Viewers;
            if (viewersConsole.Count != -1)
            {
                Console.WriteLine($"Viewers Count: {viewersConsole.Count}");
            }
        }

        private static void ShowGameLog()
        {

            GameConsole gameConsole = CurrentGame;
            if (!string.IsNullOrEmpty(gameConsole.Name))
            {
                Console.WriteLine($"Playing: {gameConsole.Name}");
            }

        }

        public static void ShowTimerLog(EnumsModel.TimerLog timerLog, TimerController timerController)
        {
            switch (timerLog)
            {
                case EnumsModel.TimerLog.Start:
                    Console.WriteLine($"|{timerController.From}| Start time: {timerController.StartTime}");
                    Console.WriteLine(lineBreak);
                    break;
                case EnumsModel.TimerLog.Stop:
                    Console.WriteLine($"|{timerController.From}| Stop time: {timerController.StopTime}");
                    Console.WriteLine(lineBreak);
                    break;
                case EnumsModel.TimerLog.Lap:
                    Console.WriteLine($"|{timerController.From}| Lap count: {timerController.LapCount} - Lap timer: {timerController.LapTimer}");
                    Console.WriteLine(lineBreak);
                    break;
                default:
                    break;
            }
        }

        public static void ShowBrowserLog(EnumsModel.BrowserLog browserLog)
        {
            switch (browserLog)
            {
                case EnumsModel.BrowserLog.Ready:
                    Console.WriteLine($"\n|Browser| : Page is ready\n");
                    break;
                case EnumsModel.BrowserLog.NotReady:
                    Console.WriteLine($"\n|Browser| : Page is not ready\n");
                    Console.WriteLine(lineBreak);
                    break;
                default:
                    break;
            }
        }

        public static void ShowExceptionLog(string message)
        {
            Console.WriteLine($"\n|Exception| : {message}\n");
        }
    }
}