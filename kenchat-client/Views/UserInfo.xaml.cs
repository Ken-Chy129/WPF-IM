using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FontAwesome.WPF;
using KenChat.Models;
using KenChat.Utils;
using Microsoft.Win32;
using MySql.Data.MySqlClient;

namespace KenChat.Views;

public partial class UserInfo : BaseWindow
{
    private readonly int _userId;

    private readonly int _searchId;

    private readonly MySqlConnection _connection = DbUtil.GetConnection();
    
    private int _gender;

    private string _selectedImagePath;
        
    private User _user;

    private bool _isEdit;

    public event EventHandler UserInfoChanged;
    
    public event EventHandler DelFriendEvent;

    public UserInfo(int userId, int searchId, int type)
    {
        _userId = userId;
        _searchId = searchId;
        InitializeComponent();
        ChooseStyle(type);
        InitializeUserInfo();
    }

    private void ChooseStyle(int type)
    {
        switch (type)
        {
            // 查看自己的信息
            case 0:
                Button.Click += ChangeUserInfo;
                Button.ToolTip = "修改信息";
                Icon.Icon = FontAwesomeIcon.Pencil;
                Cancel.Visibility = Visibility.Visible;
                Save.Visibility = Visibility.Visible;
                break;
            // 添加时查看信息
            case 1:
                Button.Click += AddFriend;
                Button.ToolTip = "添加好友";
                Icon.Icon = FontAwesomeIcon.Plus;
                Cancel.Visibility = Visibility.Hidden;
                Save.Visibility = Visibility.Hidden;
                break;
            // 成为好友后查看信息
            default:
                Button.Click += DelFriend;
                Button.ToolTip = "删除好友";
                Icon.Icon = FontAwesomeIcon.Remove;
                Cancel.Visibility = Visibility.Hidden;
                Save.Visibility = Visibility.Hidden;
                break;
        }
    }

    private void InitializeUserInfo()
    {
        var sql = $"select * from bg_user where id = {_userId}";
        using var cmd = new MySqlCommand(sql, _connection);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            _user = new User
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["username"].ToString(),
                Sign = reader["sign"].ToString(),
                Avatar = new BitmapImage(new Uri(reader["avatar"].ToString() ?? "../Images/nohead.jpg")),
                Gender = Convert.ToInt32(reader["gender"]),
                Email = string.IsNullOrEmpty(reader["email"].ToString()) ? "暂未设置" : reader["email"].ToString(),
                Phone = reader["phone"].ToString()!.Length > 15 ? "QQ注册" : "+86" + reader["phone"],
                Region = reader["region"].ToString(),
                CreateTime = reader["create_time"].ToString()
            };

            FillUserInfo();
        }
    }

    private void FillUserInfo()
    {
        SelfAvatar.Source = _user.Avatar;
        SelfName.Text = _user.Name;
        SelfSign.Text = _user.Sign;
        SelfId.Text = _user.Id.ToString();
        SelfPhone.Text = _user.Phone;
        if (_user.Gender == 1)
        {
            GenderMale.Background = (Brush)new BrushConverter().ConvertFromString("#161616")!;
        }
        else
        {
            GenderFemale.Background = (Brush)new BrushConverter().ConvertFromString("#161616")!;
        }

        _gender = _user.Gender;
        SelfEmail.Text = _user.Email;
        SelfRegion.Text = _user.Region;
        SelfCreateTime.Text = _user.CreateTime;
    }

    private void ChangeAvatar(object sender, MouseButtonEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif"
        };
        if (openFileDialog.ShowDialog() != true) return;
        _selectedImagePath = openFileDialog.FileName;
        // 使用流的方式加载显示头像，防止上传图片时显示图片被其他进程占用
        var image = new BitmapImage();
        using var stream = new FileStream(_selectedImagePath, FileMode.Open, FileAccess.Read);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        SelfAvatar.Source = image;
    }

    private void AddFriend(object sender, RoutedEventArgs e)
    {
        var addFriend = new AddFriend(_searchId, _userId, SelfAvatar.Source, SelfName.Text, SelfSign.Text)
        {
            Owner = this
        };
        addFriend.Show();
    }

    private void DelFriend(object sender, RoutedEventArgs e)
    {
        if (_userId == 1)
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
                    $"delete from bg_friend where user_id = {_searchId} and friend_id = {_userId} or user_id = {_userId} and friend_id = {_searchId}";
                using var cmd = new MySqlCommand(sql, _connection);
                cmd.ExecuteNonQuery();
                DelFriendEvent.Invoke(this, EventArgs.Empty);
                Close();
            };
            alertWindow.ShowDialog("您将删除好友" + SelfName.Text + "，是否确认？");
        }
    }

    private void ChangeUserInfo(object sender, RoutedEventArgs e)
    {
        if (_isEdit) return;
        Button.Background = (Brush)new BrushConverter().ConvertFromString("#161616")!;
        SelfAvatar.Cursor = Cursors.Hand;
        SelfAvatar.MouseLeftButtonDown += ChangeAvatar;
        SelfName.IsReadOnly = false;
        SelfName.FontStyle = FontStyles.Italic;
        SelfSign.IsReadOnly = false;
        SelfSign.FontStyle = FontStyles.Italic;
        SelfEmail.IsReadOnly = false;
        SelfEmail.FontStyle = FontStyles.Italic;
        SelfRegion.IsReadOnly = false;
        SelfRegion.FontStyle = FontStyles.Italic;
        GenderMale.Click += ToMale;
        GenderFemale.Click += ToFemale;
        _isEdit = true;
    }

    private void ToMale(object? sender, RoutedEventArgs? e)
    {
        _gender = 1;
        GenderMale.Background = (Brush)new BrushConverter().ConvertFromString("#161616")!;
        GenderFemale.Background = (Brush)new BrushConverter().ConvertFromString("#c6c6c6")!;
    }
    
    private void ToFemale(object? sender, RoutedEventArgs? e)
    {
        _gender = 0;
        GenderMale.Background = (Brush)new BrushConverter().ConvertFromString("#c6c6c6")!;
        GenderFemale.Background = (Brush)new BrushConverter().ConvertFromString("#161616")!;
    }

    private void CancelChange(object sender, RoutedEventArgs e)
    {
        if (!_isEdit) return;
        Button.Background = (Brush)new BrushConverter().ConvertFromString("#c6c6c6")!;
        RemoveStyle();
        FillUserInfo();
        _isEdit = false;
    }

    private void RemoveStyle()
    {
        SelfAvatar.Cursor = Cursors.Arrow;
        SelfAvatar.MouseLeftButtonDown -= ChangeAvatar;
        SelfName.IsReadOnly = true;
        SelfName.FontStyle = FontStyles.Normal;
        SelfSign.IsReadOnly = true;
        SelfSign.FontStyle = FontStyles.Normal;
        SelfEmail.IsReadOnly = true;
        SelfEmail.FontStyle = FontStyles.Normal;
        SelfRegion.IsReadOnly = true;
        SelfRegion.FontStyle = FontStyles.Normal;
        GenderMale.Click -= ToMale;
        GenderFemale.Click -= ToFemale;
    }

    private void SaveChange(object sender, RoutedEventArgs e)
    {
        RemoveStyle();

        _user.Name = SelfName.Text;
        _user.Sign = SelfSign.Text;
        _user.Email = SelfEmail.Text;
        _user.Region = SelfRegion.Text;
        _user.Gender = _gender;
        
        // 修改了头像
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            var result = QiniuUtil.Upload(_user.Name + Path.GetExtension(_selectedImagePath), _selectedImagePath);
            if (result != 200)
            {
                var alertWindow = new AlertWindow();
                alertWindow.ShowDialog("头像上传失败");
                return;
            }
            // 获取上传后的图片链接
            var avatar = QiniuUtil.Host + _user.Name + Path.GetExtension(_selectedImagePath);
            SelfAvatar.Source = new BitmapImage(new Uri(avatar));
            _user.Avatar = SelfAvatar.Source;
            
            var sql =
                $"update bg_user set username = '{_user.Name}', sign = '{_user.Sign}', email = '{_user.Email}', " +
                $"region = '{_user.Region}', avatar = '{SelfAvatar.Source}', gender = {_gender} where id = {_userId}";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        }
        else
        {
            var sql =
                $"update bg_user set username = '{_user.Name}', sign = '{_user.Sign}', email = '{_user.Email}', " +
                $"region = '{_user.Region}', gender = {_gender} where id = {_userId}";
            using var cmd = new MySqlCommand(sql, _connection);
            cmd.ExecuteNonQuery();
        }

        UserInfoChanged.Invoke(this, EventArgs.Empty);
        _isEdit = false;
        Button.Background = (Brush)new BrushConverter().ConvertFromString("#c6c6c6")!;
        
    }
}