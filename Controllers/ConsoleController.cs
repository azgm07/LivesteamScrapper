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

        public static void ShowTimeLog(EnumsModel.TimerLog timerLog, TimeController timeController, string moreInfo)
        {
            switch (timerLog)
            {
                case EnumsModel.TimerLog.Start:
                    if(string.IsNullOrEmpty(moreInfo))
                    {
                        Console.WriteLine($"|{timeController.From}| Start time: {timeController.StartTime}");
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Start time: {timeController.StartTime} | {moreInfo}");
                    }
                    Console.WriteLine(lineBreak);
                    break;
                case EnumsModel.TimerLog.Stop:
                    if (string.IsNullOrEmpty(moreInfo))
                    {
                        Console.WriteLine($"|{timeController.From}| Stop time: {timeController.StopTime}");
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Stop time: {timeController.StopTime} | {moreInfo}");
                    }
                    Console.WriteLine(lineBreak);
                    break;
                case EnumsModel.TimerLog.Lap:
                    if (string.IsNullOrEmpty(moreInfo))
                    {
                        Console.WriteLine($"|{timeController.From}| Lap count: {timeController.LapTime.Count} - " +
                            $"Lap timer: {timeController.LapTime.Last() - timeController.StartTime}");
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Lap count: {timeController.LapTime.Count} - " +
                            $"Lap timer: {timeController.LapTime.Last() - timeController.StartTime} | {moreInfo}");
                    }
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
                    Console.WriteLine($"|{DateTime.Now}| Browser : Page is ready");
                    break;
                case EnumsModel.BrowserLog.NotReady:
                    Console.WriteLine($"|{DateTime.Now}| Browser : Page is not ready");
                    break;
                case EnumsModel.BrowserLog.Reloading:
                    Console.WriteLine($"|{DateTime.Now}| Browser : Page is reloading");
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