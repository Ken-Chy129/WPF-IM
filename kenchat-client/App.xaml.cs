using System.Windows;
using System.Windows.Threading;
using KenChat.Views;

namespace KenChat;

public partial class App
{

    private static readonly string Test = "localhost";
    private static readonly string Prod = "xxx.xx.xx.xxx";

    public static string GetHost()
    {
        return Test;
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // 订阅 DispatcherUnhandledException 事件
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // 处理异常逻辑，显示错误信息
        var alertWindow = new AlertWindow();
        // alertWindow.ConfirmationButtonClicked += (_, _) =>
        // {
        //     try
        //     {
        //         var sql = $"update bg_user set status = 0 where id = {KenChat.Views.MainWindow.User.Id}";
        //         new MySqlCommand(sql, DbUtil.GetConnection()).ExecuteNonQuery();
        //         DbUtil.GetConnection().Close();
        //         SocketUtil.GetStream().Close();
        //         SocketUtil.GetClient().Close();
        //         Current.Shutdown();
        //     }
        //     catch (Exception)
        //     {
        //         Current.Shutdown();
        //     }
        // };
        alertWindow.ShowDialog("出现未处理的异常:" + e.Exception.Message);

        // 标记异常已处理，防止应用程序崩溃
        e.Handled = true;
    }
}