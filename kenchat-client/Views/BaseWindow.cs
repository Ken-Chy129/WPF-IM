using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HandyControl.Controls;
using Window = System.Windows.Window;

namespace KenChat.Views;

public class BaseWindow : Window
{
    
    protected void MoveWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    protected async void Close(object? sender, MouseButtonEventArgs? e)
    {
        // 创建一个双精度动画实例
        var anim = new DoubleAnimation
        {
            // 窗口从当前位置向上移动自身的高度，即完全隐藏窗口
            To = -ActualHeight,
            // 表示动画持续的时间为 0.5 秒
            Duration = TimeSpan.FromSeconds(0.5),
            // 用缓入缓出的效果
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        anim.Completed += (_, _) => Close();

        var storyboard = new Storyboard();
        storyboard.Children.Add(anim);
        // 将动画 anim 的目标对象设置为当前窗口
        Storyboard.SetTarget(anim, this);
        // 就将动画与窗口的渲染变换属性关联起来
        Storyboard.SetTargetProperty(anim, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

        RenderTransform = new TranslateTransform();
        // 开始播放动画
        BeginStoryboard(storyboard);
        // 延迟执行 0.5 秒，以等待动画完成
        await Task.Delay(TimeSpan.FromSeconds(0.5));

    }
    
    protected void NotifyIcon_Loaded(object sender, RoutedEventArgs e)
    {
        ((NotifyIcon)sender).Visibility = Visibility.Visible;
    }

    protected static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
    
        while (parent != null && parent is not T)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
    
        return parent as T;
    }
    
}