namespace Scrapper.Models;

public class ChatInteractionModel
{
    public string Author { get; set; }
    public int Counter { get; set; }

    public ChatInteractionModel()
    {
        Author = "";
        Counter = 0;
    }
}
