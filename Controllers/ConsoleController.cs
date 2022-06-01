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
            Console.WriteLine(string.Concat($"|{DateTime.Now}|", "Console Started ", "\n", lineBreak));
            Thread.Sleep(delaySeconds * 1000);
            while (!token.IsCancellationRequested)
            {
                List<string> strings = new();
                strings.Add(ShowChannelLog());
                strings.Add(ShowGameLog());
                strings.Add(ShowViewersLog());
                strings.Add(ShowChatLog());

                string joined = string.Join(" | ", strings);
                Console.WriteLine(string.Concat($"|{ DateTime.Now}| ", joined, "\n", lineBreak));

                await Task.Delay(delaySeconds * 1000);
            }

            Console.WriteLine(string.Concat($"|{DateTime.Now}| ", "Console Stopped ", "\n", lineBreak));
        }

        private string ShowChatLog()
        {
            ChatConsole chatConsole = Chat;
            if (chatConsole.MessagesFound != -1)
            {
                return $"Messages found in page: {chatConsole.MessagesFound}";
            }
            if (!string.IsNullOrEmpty(chatConsole.LastMessage))
            {
                return $"Last message found: {chatConsole.LastMessage}";
            }
            return "";
        }

        private string ShowViewersLog()
        {
            ViewersConsole viewersConsole = Viewers;
            if (viewersConsole.Count != -1)
            {
                return $"Viewers Count: {viewersConsole.Count}";
            }
            return "";
        }

        private string ShowChannelLog()
        {

            ChannelConsole channelConsole = Channel;
            if (!string.IsNullOrEmpty(channelConsole.Website) && !string.IsNullOrEmpty(channelConsole.Name))
            {
                return $"Channel: {channelConsole.Website}/{channelConsole.Name}";
            }
            return "";
        }

        private string ShowGameLog()
        {

            GameConsole gameConsole = CurrentGame;
            if (!string.IsNullOrEmpty(gameConsole.Name))
            {
                return $"Playing: {gameConsole.Name}";
            }
            return "";
        }

        public static void ShowTimeLog(EnumsModel.TimerLog timerLog, TimeController timeController, string moreInfo)
        {
            switch (timerLog)
            {
                case EnumsModel.TimerLog.Start:
                    if(string.IsNullOrEmpty(moreInfo))
                    {
                        Console.WriteLine($"|{timeController.From}| Start time: {timeController.StartTime}" + "\n" + lineBreak);
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Start time: {timeController.StartTime} | {moreInfo}" + "\n" + lineBreak);
                    }
                    break;
                case EnumsModel.TimerLog.Stop:
                    if (string.IsNullOrEmpty(moreInfo))
                    {
                        Console.WriteLine($"|{timeController.From}| Stop time: {timeController.StopTime}" + "\n" + lineBreak);
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Stop time: {timeController.StopTime} | {moreInfo}" + "\n" + lineBreak);
                    }
                    break;
                case EnumsModel.TimerLog.Lap:
                    if (string.IsNullOrEmpty(moreInfo))
                    {
                        Console.WriteLine($"|{timeController.From}| Lap count: {timeController.LapTime.Count}" + "\n" + lineBreak);
                    }
                    else
                    {
                        Console.WriteLine($"|{timeController.From}| Lap count: {timeController.LapTime.Count} | {moreInfo}" + "\n" + lineBreak);
                    }
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
                    Console.WriteLine($"|{DateTime.Now}| Browser : Page is ready" + "\n" + lineBreak);
                    break;
                case EnumsModel.BrowserLog.NotReady:
                    Console.WriteLine($"|{DateTime.Now}| Browser : Page is not ready" + "\n" + lineBreak);
                    break;
                case EnumsModel.BrowserLog.Reloading:
                    Console.WriteLine($"|{DateTime.Now}| Browser : Page is reloading" + "\n" + lineBreak);
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
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Has Started" + "\n" + lineBreak);
                    break;
                case EnumsModel.ScrapperLog.Running:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Is already running" + "\n" + lineBreak);
                    break;
                case EnumsModel.ScrapperLog.FailedToStart:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Failed to Start" + "\n" + lineBreak);
                    break;
                case EnumsModel.ScrapperLog.Failed:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Failed and has to Stop" + "\n" + lineBreak);
                    break;
                case EnumsModel.ScrapperLog.Stopped:
                    Console.WriteLine($"|{DateTime.Now}| Scrapper : Has Stopped" + "\n" + lineBreak);
                    break;
                default:
                    break;
            }
        }

        public static void ShowExceptionLog(string message)
        {
            Console.WriteLine($"|{DateTime.Now}| Exception : {message}" + "\n" + lineBreak);
        }
    }
}