package cn.ken.thirdauth.springbootstarterdemo.controller;

import cn.ken.thirdauth.ThirdAuthRequestFactory;
import cn.ken.thirdauth.model.AuthCallback;
import cn.ken.thirdauth.model.AuthResponse;
import cn.ken.thirdauth.model.AuthToken;
import cn.ken.thirdauth.model.AuthUserInfo;
import cn.ken.thirdauth.request.AuthRequest;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * <pre>
 *
 * </pre>
 *
 * @author <a href="https://github.com/Ken-Chy129">Ken-Chy129</a>
 * @since 2023/3/17 21:51
 */
@RestController
public class TestController {
    
    @Autowired()
    private ThirdAuthRequestFactory thirdAuthRequestFactory;
    
    @GetMapping("qq")
    public String getQqAuthUrl() {
        AuthRequest authRequest = thirdAuthRequestFactory.get("qq");
        return authRequest.authorizeUrl("2020101602");
    }

    @GetMapping("info")
    public AuthUserInfo getQqUserInfo(String code) {
        AuthRequest authRequest = thirdAuthRequestFactory.get("qq");
        AuthResponse<AuthToken> accessToken = authRequest.getAccessToken(new AuthCallback(code, "2020101602"));
        AuthResponse<AuthUserInfo> userInfo = authRequest.getUserInfo(accessToken.getData());
        return userInfo.getData();
    }
    
}
