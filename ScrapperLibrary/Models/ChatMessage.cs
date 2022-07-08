namespace ScrapperLibrary.Models;

public class ChatMessage
{
    public string Author { get; set; }
    public string Content { get; set; }

    public ChatMessage()
    {
        Author = "";
        Content = "";
    }
}
