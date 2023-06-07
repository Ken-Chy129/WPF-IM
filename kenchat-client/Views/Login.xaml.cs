using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KenChat.Models;
using KenChat.Utils;
using MySql.Data.MySqlClient;

namespace KenChat.Views;

public partial class Login
{
    private readonly MySqlConnection _connection = DbUtil.GetConnection();

    public Login()
    {
        InitializeComponent();
    }

    private void UserLogin(object? sender, RoutedEventArgs? e)
    {
        var phone = UserAccount.Text;
        var password = UserPassword.Password;
        // MD5加密
        var computeHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
        password = Convert.ToBase64String(computeHash);
        try
        {
            // 查询用户
            User user;
            var sql = $"select * from bg_user where phone = '{phone}' and password = '{password}'";
            using var cmd = new MySqlCommand(sql, _connection);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    // 保存用户信息
                    user = new User
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Avatar = new BitmapImage(new Uri(reader["avatar"].ToString() ?? "../Images/nohead.jpg")),
                        Sign = reader["sign"].ToString(),
                        Name = reader["username"].ToString(),
                        Gender = Convert.ToInt32(reader["gender"])
                    };
                }
                else
                {
                    var alertWindow = new AlertWindow();
                    alertWindow.ShowDialog("账号不存在或密码错误");
                    UserAccount.Text = null;
                    UserPassword.Password = null;
                    UserAccount.Focus();
                    return;
                }
            }
        
            // 更新用户状态，完成登录操作
            sql = $"update bg_user set status = 1 where id = {user.Id}";
            new MySqlCommand(sql, _connection).ExecuteNonQuery();
            new MainWindow(user).Show();
            Close();
        }
        catch (Exception)
        {
            var alertWindow = new AlertWindow();
            alertWindow.ShowDialog("登录失败");
            UserAccount.Text = null;
            UserPassword.Password = null;
            UserAccount.Focus();
        }
        
    }

    private void ChangeToRegister(object sender, RoutedEventArgs e)
    {
        var register = new Register();
        register.Show();
        Close();
    }

    private void LoginByKeyboard(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
                
        e.Handled = true; // 阻止其他控件处理该事件
        UserLogin(null, null);
    }

    private void QQ_Login(object sender, MouseButtonEventArgs e)
    {
        var url = HttpUtil.GetAuthorizationUrl();
        var browser = new Browser(url, this);
        browser.Show();
    }
    
}