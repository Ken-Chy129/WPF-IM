using System.Windows.Media;

namespace KenChat.Models;

public class Friend
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Sign { get; set; }
    public Brush? StatusColor { get; set; }
    public string? Avatar { get; set; }
    public int MsgNum { get; set; }
}