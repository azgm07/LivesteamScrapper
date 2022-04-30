using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LivesteamScrapper.Controllers
{
    public class ConsoleController : Controller
    {
        private readonly ILogger<Controller> _logger;
        private bool isRunning;

        public static ChatConsole? Chat { get; set; }
        public static ViewersConsole? Viewers { get; set; }
        public static GameConsole? Game { get; set; }

        public ConsoleController(ILogger<Controller> logger)
        {
            _logger = logger;
        }

        public Task StartConsole(int delaySeconds = 5)
        {
            isRunning = true;
            while (isRunning)
            {
                Console.WriteLine();
                ShowViewersLog();
                Console.WriteLine();
                ShowChatLog();
                Console.WriteLine();
                ShowGameLog();
                Console.WriteLine();
                Thread.Sleep(delaySeconds * 1000);
            }
            return Task.CompletedTask;
        }

        public void StopConsole()
        {
            isRunning = false;
        }

        private void ShowChatLog()
        {
            if(Chat != null)
            {
                ChatConsole chatConsole = Chat;
                if (chatConsole.MessagesFound.HasValue)
                {
                    Console.WriteLine($"Messages found in page: {chatConsole.MessagesFound}");
                }
                if (string.IsNullOrEmpty(chatConsole.LastMessage))
                {
                    Console.WriteLine($"Last message found: {chatConsole.LastMessage}");
                }
                if (chatConsole.LastMessageIndex.HasValue)
                {
                    Console.WriteLine($"Last message index: {chatConsole.LastMessageIndex}");
                }
                if (chatConsole.NewMessages.HasValue)
                {
                    Console.WriteLine($"New messages found: {chatConsole.NewMessages}");
                }
            }
        }

        private void ShowViewersLog()
        {
            if (Viewers != null)
            {
                ViewersConsole viewersConsole = Viewers;
                if (viewersConsole.Count.HasValue)
                {
                    Console.WriteLine($"Viewers Count: {viewersConsole.Count}");
                }
            }
        }

        private void ShowGameLog()
        {
            if (Game != null)
            {
                GameConsole gameConsole = Game;
                if (string.IsNullOrEmpty(gameConsole.Name))
                {
                    Console.WriteLine($"Playing: {gameConsole.Name}");
                }
            }
        }
    }
}