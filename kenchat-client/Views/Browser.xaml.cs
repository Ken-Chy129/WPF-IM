using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using KenChat.Models;
using KenChat.Utils;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace KenChat.Views;

public partial class Browser : BaseWindow
{
    private readonly MySqlConnection _connection = DbUtil.GetConnection();

    private readonly Login _loginWindow;

    public Browser(string url, Login loginWindow)
    {
        InitializeComponent();
        WebBrowser.Navigate(url);
        _loginWindow = loginWindow;
    }

    private void Callback(object sender, NavigationEventArgs e)
    {
        var currentUrl = e.Uri.ToString();

        // 在这里判断是否跳转到回调页面
        if (currentUrl.StartsWith("http://group.ken-chy129.cn/"))
        {
            // 解析URL获取Token信息
            var token = GetTokenFromUrl(currentUrl, "code");

            if (token != null)
            {
                var response = HttpUtil.GetUserInfo(token);
                var map = JsonConvert.DeserializeObject<Dictionary<string, object?>>(response);
                if (map != null)
                {
                    var uuid = map["uuid"]!.ToString();
                    User user;
                    var sql1 = $"select * from bg_user where phone = '{uuid}'";
                    using var cmd1 = new MySqlCommand(sql1, _connection);
                    using (var reader1 = cmd1.ExecuteReader())
                    {
                        if (reader1.Read())
                        {
                            // 保存用户信息
                            user = new User
                            {
                                Id = Convert.ToInt32(reader1["id"]),
                                Avatar = new BitmapImage(new Uri(reader1["avatar"].ToString() ?? "../Images/nohead.jpg")),
                                Sign = reader1["sign"].ToString(),
                                Name = reader1["username"].ToString(),
                                Gender = Convert.ToInt32(reader1["gender"])
                            };
                            // 更新用户状态
                            reader1.Close();
                            var sql = $"update bg_user set status = 1 where id = {user.Id}";
                            new MySqlCommand(sql, _connection).ExecuteNonQuery();
                        }
                        else
                        {
                            reader1.Close();
                            var username = map["username"]!.ToString();
                            var avatar = map["avatar"] == null ? "../Images/nohead.jpg" : map["avatar"]!.ToString();
                            var gender = Convert.ToInt32(map["gender"]);
                            if (!map.TryGetValue("location", out var region))
                            {
                                region = "";
                            }
                            const string sign = "这个人很高冷，没有个性签名";

                            const string sql2 =
                                "insert into bg_user(username, password, sign, phone, avatar, gender, region, status) value " +
                                "(@username, @password, @sign, @phone, @avatar, @gender, @region, 1)";
                            using var cmd2 = new MySqlCommand(sql2, _connection);
                            cmd2.Parameters.AddWithValue("@username", username);
                            cmd2.Parameters.AddWithValue("@password",
                                Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("123456"))));
                            cmd2.Parameters.AddWithValue("@sign", sign);
                            cmd2.Parameters.AddWithValue("@phone", uuid);
                            cmd2.Parameters.AddWithValue("@avatar", avatar);
                            cmd2.Parameters.AddWithValue("@gender", gender);
                            cmd2.Parameters.AddWithValue("@region", region!.ToString());
                            cmd2.ExecuteNonQuery();

                            var sql3 = $"select id from bg_user where phone = '{uuid}'";
                            using var cmd3 = new MySqlCommand(sql3, _connection);
                            var id = Convert.ToInt32(cmd3.ExecuteScalar());

                            user = new User
                            {
                                Id = id,
                                Avatar = new BitmapImage(new Uri(avatar!)),
                                Sign = sign,
                                Name = username,
                                Gender = gender
                            };
                        }
                    }

                    var mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    _loginWindow.Close();
                    Close();
                }
            }
            else
            {
                var alertWindow = new AlertWindow();
                alertWindow.ShowDialog("授权登录出现异常");
                Close();
            }
        }
    }

    private static string? GetTokenFromUrl(string url, string parameterName)
    {
        // 解析URL参数
        var queryParameters = HttpUtility.ParseQueryString(new Uri(url).Query);

        // 获取Token参数的值
        return queryParameters[parameterName];
    }
}