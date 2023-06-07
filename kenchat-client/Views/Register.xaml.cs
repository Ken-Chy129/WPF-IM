using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KenChat.Utils;
using Microsoft.Win32;
using MySql.Data.MySqlClient;

namespace KenChat.Views;

public partial class Register : BaseWindow
{

    private readonly MySqlConnection _connection = DbUtil.GetConnection();

    private string _selectedImagePath;

    private Timer _countdownTimer;

    private int _countdownSeconds = 30;

    private readonly Dictionary<string, object> _checkCode = new();
    
    public Register()
    {
        InitializeComponent();
    }
    
    // 用户注册
    private void UserRegister(object sender, RoutedEventArgs e)
    {
        var alertWindow = new AlertWindow();
        var avatar = _selectedImagePath;
        var phone = Phone.Text;
        var username = Username.Text;
        var password = Password.Password;
        var code = Code.Text;
        
        // 用户信息校验
        if (string.IsNullOrEmpty(avatar))
        {
            alertWindow.ShowDialog("请先上传头像~");
            return;
        }
        if (!HttpUtil.IsValidPhone(phone))
        {
            alertWindow.ShowDialog("手机号码不合法");
            return;
        }
        if (username.Length is < 6 or > 12)
        {
            alertWindow.ShowDialog("用户名要求长度为6-12字符！");
            return;
        }
        if (password.Length is < 6 or > 15)
        {
            alertWindow.ShowDialog("密码要求长度为6-15字符！");
            return;
        }
        
        // 校验验证码
        if (DateTime.Now.CompareTo(_checkCode["expireTime"]) > 0)
        {
            alertWindow.ShowDialog("验证码已经过期！");
            return;
        }
        if (!phone.Equals(_checkCode["phone"]) || !code.Equals(_checkCode["code"]))
        {
            alertWindow.ShowDialog("验证码错误！");
            return;
        }
        var query = $"select * from bg_user where phone = '{phone}'";
        using var cmd1 = new MySqlCommand(query, _connection);
        var num = Convert.ToInt32(cmd1.ExecuteScalar());
        if (num > 0)
        {
            alertWindow.ShowDialog("该手机号码已注册！");
            return;
        }
        
        // 上传头像
        var result = QiniuUtil.Upload(username + Path.GetExtension(_selectedImagePath), _selectedImagePath);
        if (result != 200)
        {
            alertWindow.ShowDialog("头像上传失败");
            return;
        }
        // 获取上传后的图片链接
        avatar = QiniuUtil.Host + Username.Text + Path.GetExtension(_selectedImagePath);
        
        // 注册用户
        const string insert = $"insert into bg_user(username, password, sign, phone, avatar, gender, status) value " +
                           $"(@username, @password, @sign ,@phone, @avatar, 1, 0)";
        using var cmd2 = new MySqlCommand(insert, _connection);
        cmd2.Parameters.AddWithValue("@username", username);
        cmd2.Parameters.AddWithValue("@password", Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password))));
        cmd2.Parameters.AddWithValue("@sign", "这个人很高冷，没有个性签名");
        cmd2.Parameters.AddWithValue("@phone", phone);
        cmd2.Parameters.AddWithValue("@avatar", avatar);
        var rowAffected = cmd2.ExecuteNonQuery();
        if (rowAffected > 0)
        {
            alertWindow.ConfirmationButtonClicked += (_, _) =>
            {
                var login = new Login();
                Close();
                login.Show();
            };
            alertWindow.ShowDialog("注册成功！");
        }
        else
        {
            alertWindow.ShowDialog("注册失败！");
        }
    }

    // 选择并展示头像
    private void UploadAvatar(object sender, MouseButtonEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif"
        };
        if (openFileDialog.ShowDialog() != true) return;
        _selectedImagePath = openFileDialog.FileName;
        // 使用流的方式加载显示头像，防止上传图片时显示图片被其他进程占用
        var image = new BitmapImage();
        using (var stream = new FileStream(_selectedImagePath, FileMode.Open, FileAccess.Read))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            Avatar.Source = image;
        }
        Avatar.Visibility = Visibility.Visible;
        Plus.Visibility = Visibility.Hidden;
    }
    
    // 获取验证码
    private void GetCode(object sender, RoutedEventArgs e)
    {
        var phoneNumber = Phone.Text;
        var alertWindow = new AlertWindow();
        // 校验手机号码是否合法
        if (!HttpUtil.IsValidPhone(phoneNumber))
        {
            alertWindow.ShowDialog("手机号不合法！");
            return;
        }
        // 将按钮禁用
        CodeButton.IsEnabled = false;
        // 将按钮显示内容切换为倒计时数字
        CodeButton.Content = _countdownSeconds.ToString();
        // 初始化倒计时器(每秒触发一次)，开始倒计时
        _countdownTimer = new Timer(1000);
        _countdownTimer.Elapsed += CountdownTimerElapsed;
        _countdownTimer.Start();
        // 生成6位随机数充当验证码
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        _checkCode["phone"] = phoneNumber;
        _checkCode["code"] = code;
        _checkCode["expireTime"] =  DateTime.Now.AddMinutes(5);
        // 发送验证码
        HttpUtil.SendSms("+86" + phoneNumber, code);
        // 提示消息
        alertWindow.ShowDialog("验证码发送成功，5分钟内有效~");
    }
    
    // 再次验证码倒计时
    private void CountdownTimerElapsed(object sender, ElapsedEventArgs e)
    {
        _countdownSeconds--;
        Dispatcher.Invoke(() =>
        {
            CodeButton.Content = _countdownSeconds.ToString(); // 更新倒计时内容

            if (_countdownSeconds > 0) return;
            
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            CodeButton.IsEnabled = true; // 启用按钮
            CodeButton.Content = "获取验证码"; // 恢复按钮文本
            _countdownSeconds = 30;
        });
    }
    
    // 切换登录界面
    private void ChangeToLogin(object sender, RoutedEventArgs e)
    {
        var login = new Login();
        Close();
        login.Show();
    }
}