using System.Windows.Media;

namespace KenChat.Models;

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Sign { get; set; }
    public ImageSource? Avatar { get; set; }
    public int Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Region { get; set; }
    public string? CreateTime { get; set; }
}