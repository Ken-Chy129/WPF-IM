using System;
using System.Windows;
using System.Windows.Media;
using KenChat.Models;
using KenChat.Utils;
using MySql.Data.MySqlClient;

namespace KenChat.Views;

public partial class AddFriend
{
    private readonly MySqlConnection _connection = DbUtil.GetConnection();

    private readonly int _userId;

    private readonly int _friendId;

    // 添加完好友后的事件处理
    public event EventHandler? AddActionCompleted;

    public AddFriend(int userId, int friendId, ImageSource avatarSource, string name, string sign)
    {
        InitializeComponent();
        _userId = userId;
        _friendId = friendId;
        Avatar.ImageSource = avatarSource;
        Name.Text = name;
        Sign.Text = sign;
    }

    private void SendRequest(object sender, RoutedEventArgs e)
    {
        var remark = Name.Text;
        var msg = Msg.Text;
        // 新增请求记录
        var sql1 = $"insert into bg_request(user_id, friend_id, msg, status) value({_userId}, {_friendId}, '{msg}', 0)";
        using var cmd1 = new MySqlCommand(sql1, _connection);
        var affectedRows = cmd1.ExecuteNonQuery();
        if (affectedRows > 0)
        {
            // 请求成功则直接增加单项好友记录，但是状态设置为未被确认
            var sql2 =
                $"insert into bg_friend(user_id, friend_id, remark, status) value({_userId}, {_friendId}, '{remark}', 0)";
            using var cmd2 = new MySqlCommand(sql2, _connection);
            cmd2.ExecuteNonQuery();
            SocketUtil.SendMessage(_userId, _friendId, null, MessageType.Request, null);
            var alertWindow = new AlertWindow();
            alertWindow.ConfirmationButtonClicked += (_, _) => { Close(); };
            alertWindow.ShowDialog("已发送添加请求");
            AddActionCompleted!(this, EventArgs.Empty);
        }
    }

    private void SetRemark(object sender, RoutedEventArgs e)
    {
        Name.IsReadOnly = false;
        Name.FontStyle = FontStyles.Italic;
    }
}