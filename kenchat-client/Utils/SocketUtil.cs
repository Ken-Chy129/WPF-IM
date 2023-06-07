using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using KenChat.Models;
using KenChat.Views;
using Newtonsoft.Json;

namespace KenChat.Utils;

public abstract class SocketUtil
{
    private static TcpClient? _client;

    private static NetworkStream? _stream;

    public static TcpClient GetClient()
    {
        if (_client != null) return _client;

        try
        {
            _client = new TcpClient();
            _client.Connect(App.GetHost(), 8888);
        }
        catch (Exception)
        {
            var alertWindow = new AlertWindow();
            alertWindow.ConfirmationButtonClicked += (_, _) => Application.Current.Shutdown();
            alertWindow.ShowDialog("聊天服务器连接失败，请检查您的网络");
        }

        return _client;
    }

    public static NetworkStream GetStream()
    {
        _stream = GetClient().GetStream();
        return _stream;
    }

    public static void Register(int userId)
    {
        var message = new Chat
        {
            UserId = userId,
            Type = (int)MessageType.Register
        };
        Send(message);
    }

    public static void SendMessage(int userId, int friendId, string? content, MessageType type, string? sendTime)
    {
        var message = new Chat
        {
            UserId = userId,
            FriendId = friendId,
            Content = content,
            Type = (int)type,
            SendTime = sendTime
        };
        Send(message);
    }

    private static void Send(Chat message)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            _stream!.Write(data, 0, data.Length);
        }
        catch (Exception)
        {
            _stream!.Close();
            _client!.Close();
            var alertWindow = new AlertWindow();
            alertWindow.ConfirmationButtonClicked += (_, _) => Application.Current.Shutdown();
            alertWindow.ShowDialog("聊天服务器出现异常，应用即将关闭");
        }
    }
}