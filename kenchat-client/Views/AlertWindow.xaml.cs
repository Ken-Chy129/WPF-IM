using System;
using System.Windows;

namespace KenChat.Views;

public partial class AlertWindow : BaseWindow
{
    // 消息确认后的触发事件
    public event EventHandler ConfirmationButtonClicked;

    public AlertWindow(int type = 0)
    {
        ConfirmationButtonClicked = (_, _) => { };
        InitializeComponent();
        if (type == 1)
        {
            CancelButton.Visibility = Visibility.Visible;
        }
    }

    private void Confirm(object sender, RoutedEventArgs e)
    {
        ConfirmationButtonClicked.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void Cancel(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void ShowDialog(string message, string title = "提示:")
    {
        Head.Text = title;
        Message.Text = message;
        Message.ToolTip = message;
        ShowDialog();
    }
}
