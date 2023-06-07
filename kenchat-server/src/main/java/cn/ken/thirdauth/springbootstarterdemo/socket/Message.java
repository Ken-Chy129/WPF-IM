package cn.ken.thirdauth.springbootstarterdemo.socket;

import java.io.Serializable;

/**
 * <pre>
 *
 * </pre>
 *
 * @author <a href="https://github.com/Ken-Chy129">Ken-Chy129</a>
 * @since 2023/6/1 10:40
 */
public class Message implements Serializable {
    
    
    private int UserId;
    
    private int FriendId;
    
    private String Content;
    
    private int Type;
    
    private String SendTime;

    public int getUserId() {
        return UserId;
    }

    public void setUserId(int userId) {
        this.UserId = userId;
    }

    public int getFriendId() {
        return FriendId;
    }

    public void setFriendId(int friendId) {
        this.FriendId = friendId;
    }

    public String getContent() {
        return Content;
    }

    public void setContent(String content) {
        this.Content = content;
    }

    public int getType() {
        return Type;
    }

    public void setType(int type) {
        this.Type = type;
    }

    public String getSendTime() {
        return SendTime;
    }

    public void setSendTime(String sendTime) {
        this.SendTime = sendTime;
    }
}
