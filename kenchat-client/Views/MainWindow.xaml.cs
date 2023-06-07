using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Tools;
using KenChat.Models;
using KenChat.Utils;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace KenChat.Views;

public partial class MainWindow : BaseWindow
{
    private readonly MySqlConnection _connection = DbUtil.GetConnection();
    
    private readonly NetworkStream _stream = SocketUtil.GetStream();

    private readonly byte[] _buffer = new byte[4096];

    public static User User;

    private readonly Dictionary<int, Friend> _friendMap = new();

    private readonly Dictionary<int, List<Message>?> _messagesMap = new();
    
    private bool _withFriend = true;

    private readonly List<Group> _groups = new();

    private readonly List<Friend> _friends = new();

    public MainWindow(User user)
    {
        InitializeComponent();
        User = user;
        InitInfo();
        FillFriends(); // 填充好友
        FillRequestNum(); // 填充未处理的好友请求信息
        FriendList.SelectedIndex = 0;
        SocketUtil.Register(User.Id); // 在服务端注册自己的信息
        Task.Run(ReceiveMessages); // 开启一个线程异步处理好友发来的消息
    }

    // -----------------------信息填充函数-----------------------
    
    private void InitInfo()
    {
        // 初始化个人信息
        SelfAvatar.Source = User.Avatar;
        SelfName.Text = User.Name;

        // 初始化圈子信息
        const string sql = "select * from bg_group";
        using var cmd = new MySqlCommand(sql, _connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var group = new Group
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString(),
                Describe = reader["describe"].ToString(),
                Avatar = reader["avatar"].ToString(),
            };
            _groups.Add(group);
        }
        GroupList.ItemsSource = _groups;
    }
    
    private void FillFriends()
    {
        _friends.Clear();
        _friendMap.Clear();
        
        // 查询当前登录用户的所有好友
        var sql1 = "select bf.remark, bu.* from bg_friend bf left join bg_user bu on bf.friend_id = bu.id " +
                   $"where user_id = {User.Id} and bf.status = 1 order by bu.status desc, remark";
        using var cmd1 = new MySqlCommand(sql1, _connection);
        using (var reader = cmd1.ExecuteReader())
        {
            while (reader.Read())
            {
                var friend = new Friend
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["remark"].ToString(),
                };
                _friends.Add(friend);
                _friendMap.TryAdd(friend.Id, friend);
            }
        }

        // 设置好友信息
        foreach (var friend in _friends)
        {
            var sql2 = $"select * from bg_user where id = {friend.Id}";
            using var cmd2 = new MySqlCommand(sql2, _connection);
            using var reader = cmd2.ExecuteReader();
            if (!reader.Read()) continue;
            friend.Sign = reader["sign"].ToString();
            friend.StatusColor = Convert.ToInt32(reader["status"]) == 1 ? Brushes.Green : Brushes.Gray;
            friend.Avatar = reader["avatar"].ToString();
        }

        // 查找与每个好友有多少未读信息
        foreach (var friend in _friends)
        {
            var sql3 =
                $"select count(*) from bg_record where sender_id = {friend.Id} and receiver_id = {User.Id} and status = 0";
            using var cmd3 = new MySqlCommand(sql3, _connection);
            friend.MsgNum = Convert.ToInt32(cmd3.ExecuteScalar());
        }

        FriendList.SelectionChanged -= ChatWithFriend;
        FriendList.ItemsSource = null;
        FriendList.ItemsSource = _friends;
        FriendList.SelectionChanged += ChatWithFriend;
    }

    // 查询有多少未处理的好友请求
    private void FillRequestNum()
    {
        var sql = $"select count(*) from bg_request where friend_id = {User.Id} and status = 0";
        using var cmd = new MySqlCommand(sql, _connection);
        var num = Convert.ToInt32(cmd.ExecuteScalar());
        if (num == 0)
        {
            RequestNumBorder.Visibility = Visibility.Hidden;
            RequestNum.Visibility = Visibility.Hidden;
        }
        else
        {
            RequestNumBorder.Visibility = Visibility.Visible;
            RequestNum.Visibility = Visibility.Visible;
            RequestNum.Text = num.ToString();
        }
    }
    
    // -----------------------信息填充函数-----------------------
    
    
    // -----------------------按钮跳转窗口函数-----------------------
    
    // 跳转个人信息界面
    private void SelfInfo(object sender, RoutedEventArgs e)
    {
        var userInfo = new UserInfo(User.Id, User.Id, 0);
        userInfo.UserInfoChanged += (_, _) =>
        {
            var sql = $"select * from bg_user where id = {User.Id}";
            using var cmd = new MySqlCommand(sql, _connection);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                User.Avatar = new BitmapImage(new Uri(reader["avatar"].ToString() ?? "../Images/nohead.jpg"));
                User.Name = reader["username"].ToString();
                User.Sign = reader["sign"].ToString();
                User.Email = reader["email"].ToString();
                User.Region = reader["region"].ToString();
                User.Gender = Convert.ToInt32(reader["gender"]);
                SelfAvatar.Source = User.Avatar;
                SelfName.Text = User.Name;
            }
        };
        userInfo.Show();
    }
    
    // 跳转添加好友界面
    private void AddFriend(object sender, RoutedEventArgs e)
    {
        var searchUser = new SearchUser(User.Id);
        searchUser.Show();
    }
    
    // 跳转好友请求界面
    private void FriendRequests(object sender, RoutedEventArgs e)
    {
        var friendRequest = new FriendRequest(User.Id);
        // 设置事件处理函数
        friendRequest.RequestActionCompleted += (_, _) =>
        {
            FillFriends();
            FillRequestNum();
        };
        friendRequest.Show();
    }
    
    // -----------------------按钮跳转窗口函数-----------------------
    
    
    // -----------------------好友列表相关函数-----------------------
    
    // 切换到好友列表
    private void Friend(object sender, RoutedEventArgs e)
    {
        _withFriend = true;
        FriendList.Visibility = Visibility.Visible;
        GroupList.Visibility = Visibility.Hidden;
        FriendList.SelectedIndex = 0;
        ChatWithFriend(sender, null);
    }
    
    // 切换到群组列表
    private void Group(object sender, RoutedEventArgs e)
    {
        _withFriend = false;
        FriendList.Visibility = Visibility.Hidden;
        GroupList.Visibility = Visibility.Visible;
        GroupList.SelectedIndex = 0;
        ChatWithGroup(sender, null);
    }
    
    // 列表查询
    private void Search(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (_withFriend) SearchFriend();
        else SearchGroup();
    }
    
    private void SearchFriend()
    {
        _friends.Clear();
        var searchText = SearchBox.Text;
        // 根据搜索框内容模糊查询当前用户好友中匹配的用户名、手机号码以及邮箱，先按照是否登录排序，随后按照名称排序
        var sql1 = "select bu.*, bf.remark from bg_friend bf left join bg_user bu on bf.friend_id = bu.id where " +
                   $"(remark like '%{searchText}%' or phone like '{searchText}%' or email like '%{searchText}%') " +
                   $"and user_id = {User.Id} and bf.status = 1 order by bu.status desc, bf.remark";
        using var cmd1 = new MySqlCommand(sql1, _connection);
        using (var reader = cmd1.ExecuteReader())
        {
            while (reader.Read())
            {
                _friends.Add(new Friend
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["remark"].ToString(),
                    Sign = reader["sign"].ToString(),
                    StatusColor = Convert.ToInt32(reader["status"]) == 1 ? Brushes.Green : Brushes.Gray,
                    Avatar = reader["avatar"].ToString()
                });
            }
        }

        // 填充未读信息数量
        foreach (var friend in _friends)
        {
            var sql2 =
                $"select count(*) from bg_record where sender_id = {friend.Id} and receiver_id = {User.Id} and status = 0";
            using var cmd2 = new MySqlCommand(sql2, _connection);
            friend.MsgNum = Convert.ToInt32(cmd2.ExecuteScalar());
        }

        FriendList.SelectionChanged -= ChatWithFriend;
        FriendList.ItemsSource = null;
        FriendList.ItemsSource = _friends;
        FriendList.SelectionChanged += ChatWithFriend;
    }

    private void SearchGroup()
    {
        _groups.Clear();
        var searchText = SearchBox.Text;
        // 根据搜索框内容模糊查询当前用户好友中匹配的用户名、手机号码以及邮箱，先按照是否登录排序，随后按照名称排序
        var sql = $"select * from bg_group where `name` like '%{searchText}%' or `describe` like '%{searchText}%'";
        using var cmd = new MySqlCommand(sql, _connection);
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                _groups.Add(new Group
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["name"].ToString(),
                    Describe = reader["describe"].ToString(),
                    Avatar = reader["avatar"].ToString()
                });
            }
        }

        GroupList.SelectionChanged -= ChatWithGroup;
        GroupList.ItemsSource = null;
        GroupList.ItemsSource = _groups;
        GroupList.SelectionChanged += ChatWithGroup;
    }

    // 查询好友信息
    private void FriendInfo(object sender, RoutedEventArgs e)
    {
        var index = FriendList.SelectedIndex;
        var userInfo = new UserInfo(_friends[index].Id, User.Id, 2);
        userInfo.DelFriendEvent += (_, _) => FillFriends();
        userInfo.Show();
    }

    // 删除好友
    private void DeleteFriend(object sender, RoutedEventArgs e)
    {
        var index = FriendList.SelectedIndex;
        var friendId = _friends[index].Id;
        if (friendId == 1)
        {
            var alertWindow = new AlertWindow();
            alertWindow.ShowDialog("您不可以抛弃机器人小陈噢~");
        }
        else
        {
            var alertWindow = new AlertWindow(1);
            alertWindow.ConfirmationButtonClicked += (_, _) =>
            {
                var sql =
                    $"delete from bg_friend where user_id = {User.Id} and friend_id = {friendId} or user_id = {friendId} and friend_id = {User.Id}";
                using var cmd = new MySqlCommand(sql, _connection);
                cmd.ExecuteNonQuery();
                FillFriends();
                FriendList.SelectedIndex = 0;
                SocketUtil.SendMessage(User.Id, friendId, null, MessageType.Delete, null);
            };
            alertWindow.ShowDialog("您将删除好友" + _friends[index].Name + "，是否确认？");
        }
    }

    // -----------------------好友列表函数-----------------------
    
    
    // -----------------------聊天函数-----------------------

    // 点击左侧好友列表中的好友项，进入聊天框
    private void ChatWithFriend(object sender, SelectionChangedEventArgs? e)
    {
        var index = FriendList.SelectedIndex;
        if (index < 0) index = 0;
        if (index > FriendList.Items.Count) return;

        // 设置对话框对方的名字
        ReceiverName.Text = _friends[index].Name;

        var friendId = _friends[index].Id;
        // 如果消息记录尚未查询加载
        if (!_messagesMap.TryGetValue(friendId, out var conversations))
        {
            conversations = new List<Message>();
            // 设置聊天记录
            var sql =
                $"select * from bg_record where (sender_id = {User.Id} and receiver_id = {friendId}) " +
                $"or (sender_id = {friendId} and receiver_id = {User.Id})";
            using var cmd = new MySqlCommand(sql, _connection);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var isSend = Convert.ToInt32(reader["sender_id"]) == User.Id;
                    conversations.Add(new Message
                    {
                        Content = reader["content"].ToString(),
                        Username = isSend ? SelfName.Text : _friendMap[Convert.ToInt32(reader["sender_id"])].Name,
                        UserAvatar = isSend
                            ? SelfAvatar.Source.ToString()
                            : _friendMap[Convert.ToInt32(reader["sender_id"])].Avatar,
                        Type = isSend ? 0 : 1,
                        SendTime = DateTime.Now.ToString("MM-dd HH:mm")
                    });
                }
            }
            
            _messagesMap.TryAdd(friendId, conversations);
        }

        Conversation.ItemsSource = conversations;
        if (Conversation.Items.Count > 0)
        {
            Conversation.ScrollIntoView(Conversation.Items[^1]);
        }

        // 将未读消息数设置为0
        if (_friends[index].MsgNum != 0)
        {
            _friends[index].MsgNum = 0;
            FriendList.SelectionChanged -= ChatWithFriend;
            FriendList.ItemsSource = null;
            FriendList.ItemsSource = _friends;
            FriendList.SelectedIndex = index;
            FriendList.SelectionChanged += ChatWithFriend;

            // 将未读消息设置为已读
            Task.Run(() =>
            {
                var sql =
                    $"update bg_record set status = 1 where status = 0 and sender_id = {friendId} and receiver_id = {User.Id}";
                using var cmd = new MySqlCommand(sql, _connection);
                cmd.ExecuteNonQuery();
            });
        }
    }
    
    private void ChatWithGroup(object sender, SelectionChangedEventArgs? e)
    {
        var index = GroupList.SelectedIndex;
        if (index < 0) index = 0;
        if (index > GroupList.Items.Count) return;

        // 设置对话框对方的名字
        ReceiverName.Text = _groups[index].Name;

        var groupId = _groups[index].Id;
        // 如果消息记录尚未查询加载
        if (!_messagesMap.TryGetValue(groupId, out var conversations))
        {
            conversations = new List<Message>();
            // 设置聊天记录
            var sql = $"select br.*,bu.username,bu.avatar from bg_record br left join bg_user bu on br.sender_id = bu.id where receiver_id = {groupId}";
            using var cmd = new MySqlCommand(sql, _connection);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var isSend = Convert.ToInt32(reader["sender_id"]) == User.Id;
                    conversations.Add(new Message
                    {
                        Content = reader["content"].ToString(),
                        Username = isSend ? SelfName.Text : reader["username"].ToString(),
                        UserAvatar = isSend ? SelfAvatar.Source.ToString() : reader["avatar"].ToString(),
                        Type = isSend ? 0 : 1,
                        SendTime = DateTime.Now.ToString("MM-dd HH:mm")
                    });
                }
            }

            _messagesMap.TryAdd(groupId, conversations);
        }

        Conversation.ItemsSource = conversations;
        if (Conversation.Items.Count > 0)
        {
            Conversation.ScrollIntoView(Conversation.Items[^1]);
        }
    }
    
    // -----------------------聊天函数-----------------------
    
    
    // -----------------------发送消息函数-----------------------

    // 鼠标左键发送
    private void SendByClick(object sender, RoutedEventArgs e)
    {
        if (_withFriend) SendToFriend();
        else SendToGroup();
    }

    // 键盘回车键发送
    private void SendByKeyboard(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        e.Handled = true; // 阻止其他控件处理该事件
        if (_withFriend) SendToFriend();
        else SendToGroup();
    }

    // 发送给好友
    private void SendToFriend()
    {
        var index = FriendList.SelectedIndex;
        if (index == -1)
        {
            var alertWindow = new AlertWindow();
            alertWindow.ShowDialog("请先选定要发送消息的好友");
            Message.Text = null;
            return;
        }

        var friends = (List<Friend>)FriendList.ItemsSource;
        var message = Message.Text;
        var sendTime = DateTime.Now.ToString("MM-dd HH:mm");
        UpdateSession(message, sendTime);

        var friendId = friends[index].Id;
        // 异步将消息入库
        Task.Run(() =>
        {
            var sql = "insert into bg_record(sender_id, receiver_id, content, type, status, send_time) " +
                      $"value({User.Id}, {friendId}, '{message}', 0, 0, '{sendTime}')";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        });

        // 如果是发送给机器人
        if (friendId == 1)
        {
            // 异步处理返回结果
            Task.Run(() => { SendToRobot(message); });
        }
        else
        {
            SocketUtil.SendMessage(User.Id, friendId, message, MessageType.Private, sendTime);
        }
    }

    // 发送给群组
    private void SendToGroup()
    {
        var index = GroupList.SelectedIndex;
        if (index == -1)
        {
            var alertWindow = new AlertWindow();
            alertWindow.ShowDialog("请先选定要发送消息的群组");
            Message.Text = null;
            return;
        }

        var message = Message.Text;
        var sendTime = DateTime.Now.ToString("MM-dd HH:mm");
        UpdateSession(message, sendTime);

        var groupId = _groups[index].Id;
        // 异步将消息入库
        Task.Run(() =>
        {
            var sql = "insert into bg_record(sender_id, receiver_id, content, type, status, send_time) " +
                      $"value({User.Id}, {groupId}, '{message}', 1, 0, '{sendTime}')";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        });
        // 发送给聊天服务器
        SocketUtil.SendMessage(User.Id, groupId, message, MessageType.Group, sendTime);
    }

    // 发送给机器人
    private void SendToRobot(string message)
    {
        var conversations = (List<Message>)Conversation.ItemsSource;
        var content = HttpUtil.TalkWithGpt(User.Id.ToString(), message);
        var sendTime = DateTime.Now.ToString("MM-dd HH:mm");
        
        conversations.Add(new Message
        {
            Content = content,
            Username = "Robot",
            UserAvatar = "https://cdn.ken-chy129.cn//picgo/202306021905780.png",
            Type = 1,
            SendTime = sendTime
        });

        Task.Run(() =>
        {
            var sql = "insert into bg_record(sender_id, receiver_id, content, type, status, send_time) " +
                      $"value(1, {User.Id}, '{content}', 0, 0, '{sendTime}')";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        });

        // 更新UI需要使用 Dispatcher
        Dispatcher.Invoke(() =>
        {
            var index = FriendList.SelectedIndex;
            Conversation.ItemsSource = null;
            Conversation.ItemsSource = conversations;
            Conversation.ScrollIntoView(Conversation.Items[^1]);
            FillFriends();
            FriendList.SelectedIndex = index;
        });
    }
    
    // 更新聊天会话框
    private void UpdateSession(string message, string sendTime)
    {
        var conversations = (List<Message>)Conversation.ItemsSource;
        conversations.Add(new Message
        {
            Content = message,
            Username = SelfName.Text,
            UserAvatar = SelfAvatar.Source.ToString(),
            Type = 0,
            SendTime = sendTime
        });
        Message.Text = null;
        Conversation.ItemsSource = null;
        Conversation.ItemsSource = conversations;
        Conversation.ScrollIntoView(Conversation.Items[^1]);
    }
    
    // -----------------------发送消息函数-----------------------
    
    
    // -----------------------系统相关函数-----------------------
    
    // 异步接受服务器发来的消息
    private void ReceiveMessages()
    {
        while (true)
        {
            try
            {
                var bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
                var message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                var chat = JsonConvert.DeserializeObject<Chat>(message);
                if (chat == null) continue;

                switch (chat.Type)
                {
                    case (int)MessageType.Login:
                    {
                        // 上线通知
                        if (_friendMap.ContainsKey(chat.UserId))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _friendMap[chat.UserId].StatusColor = Brushes.Green;
                                FriendList.SelectionChanged -= ChatWithFriend;
                                FriendList.ItemsSource = null;
                                FriendList.ItemsSource = _friends;
                                FriendList.SelectionChanged += ChatWithFriend;
                            });
                        }
                        continue;
                    }
                    case (int)MessageType.Offline:
                    {
                        // 下线通知
                        if (_friendMap.ContainsKey(chat.UserId))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _friendMap[chat.UserId].StatusColor = Brushes.Gray;
                                FriendList.SelectionChanged -= ChatWithFriend;
                                FriendList.ItemsSource = null;
                                FriendList.ItemsSource = _friends;
                                FriendList.SelectionChanged += ChatWithFriend;
                            });
                        }
                        continue;
                    }
                    case (int)MessageType.Request:
                    {
                        // 好友请求
                        Dispatcher.Invoke(FillRequestNum);
                        continue;
                    }
                    case (int)MessageType.Receive:
                    {
                        // 请求被接受
                        Dispatcher.Invoke(FillFriends);
                        continue;
                    }
                    case (int)MessageType.Delete:
                    {
                        // 被好友删除
                        Dispatcher.Invoke(() =>
                        {
                            var alertWindow = new AlertWindow();
                            alertWindow.ShowDialog("很抱歉，您被好友" + _friendMap[chat.UserId].Name + "删除了");
                            FillFriends();
                        });
                        continue;
                    }
                }

                // 已经加载过该项，则直接添加，没有加载过则等初次加载时直接就可以从数据库获得全量数据，故无需在此处add
                // 如果是发送给好友，则寻找发送方的消息记录，如果发送给的是群组，则查找群组的消息记录
                if (_messagesMap.TryGetValue(chat.Type == (int)MessageType.Private ? chat.UserId : chat.FriendId, out var conversations))
                {
                    if (!_friendMap.ContainsKey(chat.UserId))
                    {
                        var sql = $"select * from bg_user where id = {chat.UserId}";
                        using var cmd = new MySqlCommand(sql, _connection);
                        using var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            var friend = new Friend
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["username"].ToString(),
                                Avatar = reader["avatar"].ToString()
                            };
                            _friendMap.TryAdd(friend.Id, friend);
                        }
                    }
                    
                    conversations!.Add(new Message
                    {
                        Username = _friendMap[chat.UserId].Name,
                        UserAvatar = _friendMap[chat.UserId].Avatar,
                        SendTime = chat.SendTime,
                        Content = chat.Content,
                        Type = 1
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    bool isNow;
                    int index;
                    if (_withFriend)
                    { 
                        index = FriendList.SelectedIndex; 
                        isNow = _friends[index].Id == chat.UserId;
                    }
                    else
                    {
                        index = GroupList.SelectedIndex;
                        isNow = _groups[index].Id == chat.FriendId;
                    }

                    if (isNow)
                    {
                        // 如果发消息的是当前对话用户，则直接更新对话框数据源
                        Conversation.ItemsSource = null;
                        Conversation.ItemsSource = conversations;
                        Conversation.ScrollIntoView(Conversation.Items[^1]);
                    }
                    else
                    {
                        // 如果不是则更新好友列表中未读消息数量
                        if (_withFriend)
                        {
                            _friendMap[chat.UserId].MsgNum += 1;
                            FriendList.SelectionChanged -= ChatWithFriend;
                            FriendList.ItemsSource = null;
                            FriendList.ItemsSource = _friends;
                            FriendList.SelectedIndex = index;
                            FriendList.SelectionChanged += ChatWithFriend;
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() =>
                {
                    var alertWindow = new AlertWindow();
                    alertWindow.ShowDialog(e.Message);
                });
            }
        }
    }
    
    private void Hide(object sender, MouseButtonEventArgs e)
    {
        Visibility = Visibility.Hidden;
    }
    
    private void Min(object sender, MouseButtonEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void Show(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Visible;
        WindowState = WindowState.Normal;
        WindowHelper.SetWindowToForeground(this);
    }
    
    private void Close(object sender, RoutedEventArgs e)
    {
        var sql = $"update bg_user set status = 0 where id = {User.Id}";
        new MySqlCommand(sql, _connection).ExecuteNonQuery();
        _connection.Close();
        _stream.Close();
        SocketUtil.GetClient().Close();
        Close();
        Application.Current.Shutdown();
    }
    
    // -----------------------系统相关函数-----------------------
    
}