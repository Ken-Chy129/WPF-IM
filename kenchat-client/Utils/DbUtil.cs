using System;
using System.Windows;
using KenChat.Views;
using MySql.Data.MySqlClient;

namespace KenChat.Utils;

public abstract class DbUtil
{

    private static MySqlConnection? _connection;

    public static MySqlConnection GetConnection()
    {
        if (_connection == null)
        {
            try
            {
                const string str = "server=x.xx.xxx.xx;User Id=root;password=xxx;Database=bravery_group";
                _connection = new MySqlConnection(str);
                _connection.Open();
            }
            catch (Exception)
            {
                var alertWindow = new AlertWindow();
                alertWindow.ConfirmationButtonClicked += (_, _) => Application.Current.Shutdown();
                alertWindow.ShowDialog("数据库连接失败，应用即将关闭");
            }
        }

        return _connection;
    }

}