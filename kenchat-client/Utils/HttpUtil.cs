using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace KenChat.Utils;

// 封装需要使用到的API
public abstract class HttpUtil
{
    private static string Post(string url, object data)
    {
        using var client = new HttpClient();
        try
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            // 发送 POST 请求
            var response = client.PostAsync(url, content).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }
        catch (HttpRequestException e)
        {
            return e.Message;
        }
    }

    private static string Get(string url)
    {
        using var client = new HttpClient();
        try
        {
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }
        catch (HttpRequestException e)
        {
            return e.Message;
        }
    }

    // 发送注册短信验证码
    public static void SendSms(string phone, string code)
    {
        // todo: 接入短信API
    }

    // GPT聊天
    public static string TalkWithGpt(string userId, string sendMsg)
    {
        // todo: 接入文本聊天API
        return "";
    }

    public static string GetAuthorizationUrl()
    {
        // todo: 接入Oauth2接口
        return Get("http://" + App.GetHost() + ":8889/qq");
    }

    public static string GetUserInfo(string code)
    {
        // todo: 接入Oauth2接口
        return Get("http://" + App.GetHost() + ":8889/info?code=" + code);
    }

    public static bool IsValidPhone(string phone)
    {
        var regex = new Regex(@"^1[3|4|5|7|8][0-9]{9}$");
        return regex.IsMatch(phone);
    }
}