namespace KenChat.Models;

public class Message
{
    public string? Username { set; get; }
    public string? UserAvatar { set; get; }
    public string? Content { set; get; }
    public int Type { set; get; }
    public string? SendTime { set; get; }
}

