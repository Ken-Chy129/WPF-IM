package cn.ken.thirdauth.springbootstarterdemo.socket;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.SelectionKey;
import java.nio.channels.Selector;
import java.nio.channels.ServerSocketChannel;
import java.nio.channels.SocketChannel;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

/**
 * <pre>
 *
 * </pre>
 *
 * @author <a href="https://github.com/Ken-Chy129">Ken-Chy129</a>
 * @since 2023/6/2 18:29
 */
public class Server {

    private final int port;
    private Selector selector;
    private final ByteBuffer buffer = ByteBuffer.allocate(4096);
    private final Map<Integer, SocketChannel> users = new HashMap<>();
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Server(int port) {
        this.port = port;
    }

    public void start() {
        try (ServerSocketChannel serverSocketChannel = ServerSocketChannel.open()) {
            serverSocketChannel.configureBlocking(false);
            serverSocketChannel.bind(new InetSocketAddress(port));

            selector = Selector.open();
            serverSocketChannel.register(selector, SelectionKey.OP_ACCEPT);

            System.out.println("Chat server started on port " + port);

            while (true) {
                int readyChannels = selector.select();
                if (readyChannels == 0) {
                    continue;
                }

                Iterator<SelectionKey> keyIterator = selector.selectedKeys().iterator();
                while (keyIterator.hasNext()) {
                    SelectionKey key = keyIterator.next();

                    if (key.isAcceptable()) {
                        accept(key);
                    } else if (key.isReadable()) {
                        read(key);
                    }

                    keyIterator.remove();
                }
            }
        } catch (IOException e) {
            System.out.println("Error starting chat server: " + e.getMessage());
        }
    }

    private void accept(SelectionKey key) throws IOException {
        ServerSocketChannel serverSocketChannel = (ServerSocketChannel) key.channel();
        SocketChannel clientChannel = serverSocketChannel.accept();
        clientChannel.configureBlocking(false);
        clientChannel.register(selector, SelectionKey.OP_READ);
        System.out.println("New client connected");
    }

    private void read(SelectionKey key) throws IOException {
        SocketChannel clientChannel = (SocketChannel) key.channel();
        buffer.clear();
        try {
            int bytesRead = clientChannel.read(buffer);
            if (bytesRead == -1) {
                int clientId = offline(clientChannel);
                key.cancel();
                System.out.println("用户" + clientId + "主动断开");
                return;
            }

            Message message = readMessage();

            if (message.getType() == MessageType.Register.ordinal()) {
                // 保存客户端
                users.put(message.getUserId(), clientChannel);
                // 广播用户上线消息
                message.setType(MessageType.Login.ordinal());
                System.out.println("用户" + message.getUserId() + "上线了");
                broadcast(message, clientChannel);
            } else if (message.getType() == MessageType.Private.ordinal() || message.getType() == MessageType.Request.ordinal() || message.getType() == MessageType.Receive.ordinal() || message.getType() == MessageType.Delete.ordinal()) {
                // 点对点通知好友
                p2pSend(message);
            } else if (message.getType() == MessageType.Group.ordinal()) {
                // 群发消息
                broadcast(message, clientChannel);
            }
        } catch (IOException e) {
            key.cancel();
            int clientId = offline(clientChannel);
            if (clientId != -1) {
                System.out.println("用户" + clientId + "异常中断:" + e.getMessage());
            }
        }

    }

    // 读取客户端发来的消息
    private Message readMessage() throws JsonProcessingException {
        buffer.flip();
        JsonNode node = objectMapper.readTree(StandardCharsets.UTF_8.decode(buffer).toString());
        Message message = new Message();
        message.setUserId(node.get("UserId").asInt());
        message.setFriendId(node.get("FriendId").asInt());
        message.setContent(node.get("Content").asText());
        message.setType(node.get("Type").asInt());
        message.setSendTime(node.get("SendTime").asText());
        return message;
    }

    // 获取客户端的用户id
    private int getClientId(SocketChannel clientChannel) {
        // 如果客户端连接之后还没发送注册消息就中断连接了，那么此时map里并没有保存这个客户端，则返回-1
        for (Map.Entry<Integer, SocketChannel> entry : users.entrySet()) {
            if (entry.getValue() == clientChannel) {
                return entry.getKey();
            }
        }
        return -1;
    }

    // 客户端下线消息
    private int offline(SocketChannel clientChannel) throws IOException {
        int clientId = getClientId(clientChannel);
        if (clientId == -1) {
            return -1;
        }
        users.remove(clientId);
        clientChannel.close();
        Message message = new Message();
        message.setUserId(clientId);
        message.setType(MessageType.Offline.ordinal());
        broadcast(message, clientChannel);
        return clientId;
    }

    private void p2pSend(Message message) {
        try {
            SocketChannel client = users.get(message.getFriendId());
            if (client == null) {
                // 好友已经下线则不需要通知
                return;
            }
            buffer.clear();
            buffer.put(objectMapper.writeValueAsString(message).getBytes());
            buffer.flip();
            client.write(buffer);
        } catch (IOException e) {
            System.out.println("send message error:" + e.getMessage());
        }
    }

    private void broadcast(Message message, SocketChannel sender) {
        try {
            for (SocketChannel client : users.values()) {
                if (client != sender) {
                    buffer.clear();
                    buffer.put(objectMapper.writeValueAsString(message).getBytes());
                    buffer.flip();
                    client.write(buffer);
                }
            }
        } catch (IOException e) {
            System.out.println("send message error:" + e.getMessage());
        }
    }

}

