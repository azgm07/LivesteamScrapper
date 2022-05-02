namespace LivesteamScrapper.Models
{
    public class ChatConsole
    {
        public int MessagesFound { get; set; }
        public string LastMessage { get; set; }

        public ChatConsole()
        {
            MessagesFound = -1;
            LastMessage = string.Empty;
        }
    }

    public class ViewersConsole
    {
        public int Count { get; set; }
        public ViewersConsole()
        {
            Count = -1;
        }

    }

    public class GameConsole
    {
        public string Name { get; set; }

        public GameConsole()
        {
            Name = string.Empty;
        }
    }
}
