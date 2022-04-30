namespace LivesteamScrapper.Models
{
    public class ChatConsole
    {
        public int? MessagesFound { get; set; }
        public string? LastMessage { get; set; }
        public int? LastMessageIndex { get; set; }
        public int? NewMessages { get; set; }
    }

    public class ViewersConsole
    {
        public int? Count { get; set; }
    }

    public class GameConsole
    {
        public string? Name { get; set; }
    }
}
