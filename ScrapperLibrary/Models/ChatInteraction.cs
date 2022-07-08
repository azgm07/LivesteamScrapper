namespace ScrapperLibrary.Models;

public class ChatInteraction
{
    public string Author { get; set; }
    public int Counter { get; set; }

    public ChatInteraction()
    {
        Author = "";
        Counter = 0;
    }
}
