package cn.ken.thirdauth.springbootstarterdemo.socket;

/**
 * <pre>
 *
 * </pre>
 *
 * @author <a href="https://github.com/Ken-Chy129">Ken-Chy129</a>
 * @since 2023/6/4 15:31
 */
public enum MessageType {

    // 服务器注册消息
    Register,

    // 好友私聊
    Private,

    // 群聊
    Group,

    // 登录消息
    Login,

    // 下线消息
    Offline,

    // 好友请求消息
    Request,

    // 好友接受消息
    Receive,

    // 好友删除消息
    Delete
}
