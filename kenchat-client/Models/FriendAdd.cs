using System.Windows;

namespace KenChat.Models;

public class FriendAdd
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public string? Msg { get; set; }
    public string? IsReceived { get; set; }
    public Visibility Deal { get; set; }
    public FontStyle FontStyle { get; set; }
    public Visibility UnDeal { get; set; }
    public bool IsReadOnly { get; set; }
    public string? CreateTime { get; set; }
}