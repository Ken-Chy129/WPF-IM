package cn.ken.thirdauth.springbootstarterdemo;

import cn.ken.thirdauth.springbootstarterdemo.socket.Server;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class ThirdauthSpringBootStarterDemoApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(ThirdauthSpringBootStarterDemoApplication.class, args);
        new Server(8888).start();
    }

}
