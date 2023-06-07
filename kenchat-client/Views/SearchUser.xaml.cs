using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KenChat.Models;
using KenChat.Utils;
using MySql.Data.MySqlClient;

namespace KenChat.Views;

public partial class SearchUser
{
    private readonly MySqlConnection _connection = DbUtil.GetConnection();

    private readonly int _userId;

    private int _friendId;

    public SearchUser(int userId)
    {
        _userId = userId;
        InitializeComponent();
    }

    private void Search(object sender, KeyEventArgs? e)
    {
        if (e != null && e.Key != Key.Enter) return;

        var friends = new List<Friend>();
        var searchContent = SearchContent.Text;
        // 没有输入则不用查询直接返回
        if (string.IsNullOrEmpty(searchContent))
        {
            Tip.Visibility = Visibility.Visible;
            FriendList.ItemsSource = null;
            return;
        }

        // 查询没有成为好友以及没有发送好友请求或者被发送好友请求，且不是自己的用户,已被拒绝的可以查询得到
        var sql =
            $"select * from bg_user where (username like '%{searchContent}%' or phone like '{searchContent}%' or email like '%{searchContent}%') " +
            $"and (id not in (select friend_id from bg_friend where user_id = {_userId} or friend_id = {_userId})) and id != {_userId}";
        using (var cmd = new MySqlCommand(sql, _connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                friends.Add(new Friend
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["username"].ToString(),
                    Sign = reader["sign"].ToString(),
                    Avatar = reader["avatar"].ToString()
                });
            }
        }

        FriendList.ItemsSource = friends;
        Tip.Visibility = friends.Count == 0 ? Visibility.Visible : Visibility.Hidden;
    }

    private void GetUserInfo(object sender, SelectionChangedEventArgs e)
    {
        var listBox = (ListBox)sender;
        var selectedIndex = listBox.SelectedIndex;
        var friends = (List<Friend>)FriendList.ItemsSource;
        _friendId = friends[selectedIndex].Id;
        var userInfo = new UserInfo(_friendId, _userId, 1);
        userInfo.Show();
    }

    private void AddFriend(object sender, RoutedEventArgs routedEventArgs)
    {
        var textBlock = (Button)sender;
        var listBoxItem = FindParent<ListBoxItem>(textBlock);
        if (listBoxItem != null)
        {
            var index = FriendList.ItemContainerGenerator.IndexFromContainer(listBoxItem);
            var friendListItemsSource = (List<Friend>)FriendList.ItemsSource;
            var avatar = friendListItemsSource[index].Avatar;
            var friendId = friendListItemsSource[index].Id;
            var name = friendListItemsSource[index].Name;
            var sign = friendListItemsSource[index].Sign;
            var addFriend = new AddFriend(_userId, friendId, new BitmapImage(new Uri(avatar!)), name!, sign!)
            {
                Owner = this
            };
            addFriend.AddActionCompleted += (o, _) =>
            {
                // 刷新好友搜索表
                Search(o, null);
            };
            addFriend.Show();   
        }
    }
}