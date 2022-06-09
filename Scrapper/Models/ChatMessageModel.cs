namespace Scrapper.Models;

public class ChatMessageModel
{
    public string Author { get; set; }
    public string Content { get; set; }

    public ChatMessageModel()
    {
        Author = "";
        Content = "";
    }
}
