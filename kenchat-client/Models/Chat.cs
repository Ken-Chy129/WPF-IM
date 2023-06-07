namespace KenChat.Models;

public class Chat
{
    public int UserId { init; get; }
    public int FriendId { init; get; }
    public string? Content { init; get; }
    public int Type { init; get; }
    public string? SendTime { init; get; }
}