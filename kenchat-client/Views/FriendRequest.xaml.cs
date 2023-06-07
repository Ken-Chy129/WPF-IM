using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KenChat.Models;
using KenChat.Utils;
using MySql.Data.MySqlClient;

namespace KenChat.Views;

public partial class FriendRequest
{
    private readonly MySqlConnection _connection = DbUtil.GetConnection();

    private readonly int _userId;
    
    // 好友请求处理操作事件
    public event EventHandler RequestActionCompleted;
    
    public FriendRequest(int userId)
    {
        _userId = userId;
        InitializeComponent();
        InitializeFriendRequests();
    }

    private void InitializeFriendRequests()
    {
        var friendAdds = new List<FriendAdd>();
        var sql = $"select br.*, bu.username, bu.avatar from bg_request br left join bg_user bu on br.user_id = bu.id where friend_id = {_userId} order by create_time DESC";
        using (var cmd = new MySqlCommand(sql, _connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                friendAdds.Add(new FriendAdd
                {
                    Id = Convert.ToInt32(reader["user_id"]),
                    Name = reader["username"].ToString(),
                    Avatar = reader["avatar"].ToString(),
                    Msg = reader["msg"].ToString(),
                    IsReceived = Convert.ToInt32(reader["status"]) == 1 ? "已接受" : "已拒绝",
                    Deal = Convert.ToInt32(reader["status"]) == 0 ? Visibility.Hidden : Visibility.Visible,
                    UnDeal = Convert.ToInt32(reader["status"]) == 0 ? Visibility.Visible : Visibility.Hidden,
                    FontStyle = FontStyles.Normal,
                    IsReadOnly = true,
                    CreateTime = reader["create_time"].ToString()
                });
            }
        }

        FriendList.ItemsSource = friendAdds;
        Tip.Visibility = friendAdds.Count == 0 ? Visibility.Visible : Visibility.Hidden;
    }
    
    private void GetUserInfo(object sender, SelectionChangedEventArgs e)
    {
        var listBox = (ListBox)sender;
        var selectedIndex = listBox.SelectedIndex;
        var friends = (List<FriendAdd>)FriendList.ItemsSource;
        
        var friendId = friends[selectedIndex].Id;
        var userInfo = new UserInfo(friendId, _userId, 1);
        userInfo.Show();
    }

    private void ReceiveRequest(object sender, MouseButtonEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        var listBoxItem = FindParent<ListBoxItem>(textBlock);

        if (listBoxItem != null)
        {
            var index = FriendList.ItemContainerGenerator.IndexFromContainer(listBoxItem);
            var friendListItemsSource = (List<FriendAdd>)FriendList.ItemsSource;
            var friendId = friendListItemsSource[index].Id;
            var remark = friendListItemsSource[index].Name;
            // 新增好友记录
            var sql1 =
                $"insert into bg_friend(user_id, friend_id, remark, status) value({_userId}, {friendId}, '{remark}', 1)";
            using var cmd1 = new MySqlCommand(sql1, _connection);
            var affectedRows = cmd1.ExecuteNonQuery();
            if (affectedRows > 0)
            {
                // 修改对方的好友状态为成功添加
                var sql2 = $"update bg_friend set status = 1 where user_id = {friendId} and friend_id = {_userId}";
                new MySqlCommand(sql2, _connection).ExecuteNonQuery();
                // 修改添加好友请求为已添加
                var sql3 = $"update bg_request set status = 1 where user_id = {friendId} and friend_id = {_userId}";
                new MySqlCommand(sql3, _connection).ExecuteNonQuery();

                // 修改显示，将接受和拒绝按钮换为文字显示已接受
                friendListItemsSource[index].Deal = Visibility.Visible;
                friendListItemsSource[index].UnDeal = Visibility.Hidden;
                friendListItemsSource[index].IsReceived = "已接受";
                // 将名称还原为不可以修改和normal字体
                friendListItemsSource[index].IsReadOnly = true;
                friendListItemsSource[index].FontStyle = FontStyles.Normal;

                FriendList.ItemsSource = null;
                FriendList.ItemsSource = friendListItemsSource;

                RequestActionCompleted.Invoke(this, EventArgs.Empty);
                // 通知对方自己已接受请求
                SocketUtil.SendMessage(_userId, friendId, null, MessageType.Receive, null);
            }
        }
    }

    private void RefuseRequest(object sender, MouseButtonEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        var listBoxItem = FindParent<ListBoxItem>(textBlock);
        if (listBoxItem != null)
        {
            var index = FriendList.ItemContainerGenerator.IndexFromContainer(listBoxItem);
            var friendListItemsSource = (List<FriendAdd>)FriendList.ItemsSource;
            var friendId = friendListItemsSource[index].Id;
            
            // 删除对方的好友记录
            var sql1 = $"delete from bg_friend where user_id = {friendId} and friend_id = {_userId}";
            new MySqlCommand(sql1, _connection).ExecuteNonQuery();
            
            // 更改好友添加请求为已拒绝
            var sql2 = $"update bg_friend set status = 2 where user_id = {friendId} and friend_id = {_userId}";
            new MySqlCommand(sql2, _connection).ExecuteNonQuery();
            
            // 修改显示，将接受和拒绝按钮换为文字显示已拒绝
            friendListItemsSource[index].Deal = Visibility.Visible;
            friendListItemsSource[index].UnDeal = Visibility.Hidden;
            friendListItemsSource[index].IsReceived = "已拒绝";
            // 将名称还原为不可以修改和normal字体
            friendListItemsSource[index].IsReadOnly = true;
            friendListItemsSource[index].FontStyle = FontStyles.Normal;
            
            FriendList.ItemsSource = null;
            FriendList.ItemsSource = friendListItemsSource;
            
            RequestActionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetRemark(object sender, RoutedEventArgs e)
    {
        var textBlock = (Button)sender;
        var listBoxItem = FindParent<ListBoxItem>(textBlock);
        if (listBoxItem != null)
        {
            var index = FriendList.ItemContainerGenerator.IndexFromContainer(listBoxItem);
            var friendListItemsSource = (List<FriendAdd>)FriendList.ItemsSource;
            friendListItemsSource[index].FontStyle = FontStyles.Italic;
            friendListItemsSource[index].IsReadOnly = false;
            FriendList.ItemsSource = null;
            FriendList.ItemsSource = friendListItemsSource;
        }
    }

    private void UpdateRemark(object sender, TextChangedEventArgs e)
    {
        var textBlock = (TextBox)sender;
        var listBoxItem = FindParent<ListBoxItem>(textBlock);
        if (listBoxItem != null)
        {
            var index = FriendList.ItemContainerGenerator.IndexFromContainer(listBoxItem);
            var friendListItemsSource = (List<FriendAdd>)FriendList.ItemsSource;
            friendListItemsSource[index].Name = textBlock.Text;
        }
    }
}