using System;
using KenChat.Views;
using Qiniu.Storage;
using Qiniu.Util;

namespace KenChat.Utils;

// 七牛云上传工具类
public abstract class QiniuUtil
{
    private static readonly Mac Mac = new("xxx",
        "xxxx");

    public const string Host = "xxx";

    public static int Upload(string key, string filePath)
    {
        try
        {
            // 设置上传策略
            var putPolicy = new PutPolicy
            {
                Scope = "ken-chat:" + key
            };
            var token = Auth.CreateUploadToken(Mac, putPolicy.ToJsonString());
            // 配置存储空间信息
            var config = new Config
            {
                Zone = Zone.ZONE_CN_South,
                UseHttps = true,
                UseCdnDomains = false
            };
            var target = new UploadManager(config);
            var result = target.UploadFile(filePath, key, token, new PutExtra());
            return result.Code;
        }
        catch (Exception)
        {
            return 500;
        }
    }
}