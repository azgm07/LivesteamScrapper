using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LivesteamScrapper.Controllers
{
    public class ConsoleController
    {
        private const string lineBreak = "------------------------------";

        public ChannelConsole Channel { get; set; }
        public ChatConsole Chat { get; set; }
        public ViewersConsole Viewers { get; set; }
        public GameConsole CurrentGame { get; set; }

        public ConsoleController()
        {
            Channel = new ChannelConsole();
            Chat = new ChatConsole();
            Viewers = new ViewersConsole();
            CurrentGame = new GameConsole();
        }

        public async Task RunConsoleAsync(CancellationToken token, int delaySeconds = 30)
        {
            Console.WriteLine(string.Concat("\n", "Console Started ", lineBreak, "\n"));
            Thread.Sleep(delaySeconds * 1000);
            while (!token.IsCancellationRequested)
            {
                ShowGameLog();
                ShowViewersLog();
                ShowChatLog();
                Console.WriteLine(lineBreak);
                await Task.Delay(delaySeconds * 1000);
            }

            Console.WriteLine(string.Concat("\n", "Console Stopped ", lineBreak, "\n"));
        }

        private void ShowChatLog()
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

        private void ShowViewersLog()
        {
            ViewersConsole viewersConsole = Viewers;
            if (viewersConsole.Count != -1)
            {
                Console.WriteLine($"Viewers Count: {viewersConsole.Count}");
            }
        }

        private void ShowGameLog()
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
                        Console.WriteLine($"|{timeController.From}| Lap count: {timeController.LapTime.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Lap count: {timeController.LapTime.Count} | {moreInfo}");
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

        public static void ShowScrapperLog(EnumsModel.ScrapperLog scrapperLog)
        {
            switch (scrapperLog)
            {
                case EnumsModel.ScrapperLog.Started:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Has Started");
                    break;
                case EnumsModel.ScrapperLog.Running:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Is already running");
                    break;
                case EnumsModel.ScrapperLog.FailedToStart:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Failed to Start");
                    break;
                case EnumsModel.ScrapperLog.Failed:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Failed and has to Stop");
                    break;
                case EnumsModel.ScrapperLog.Stopped:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Has Stopped");
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