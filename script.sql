create table bg_friend
(
    id          int auto_increment
        primary key,
    user_id     int                                 not null,
    friend_id   int                                 not null,
    remark      varchar(255)                        not null,
    status      tinyint                             not null,
    create_time timestamp default CURRENT_TIMESTAMP not null
)
    charset = utf8;

create table bg_group
(
    id         int auto_increment
        primary key,
    name       varchar(255) not null,
    `describe` varchar(255) not null,
    avatar     text         not null
);

create table bg_record
(
    id          bigint auto_increment
        primary key,
    sender_id   int          not null,
    receiver_id int          not null,
    content     text         not null,
    type        tinyint      not null,
    status      tinyint      not null,
    send_time   varchar(255) not null
)
    charset = utf8;

create table bg_request
(
    id          int auto_increment
        primary key,
    user_id     int                                 not null,
    friend_id   int                                 not null,
    msg         text                                null,
    status      int                                 not null,
    create_time timestamp default CURRENT_TIMESTAMP not null
)
    charset = utf8;

create table bg_user
(
    id          int auto_increment
        primary key,
    username    varchar(255)                           not null,
    password    char(24)                               not null,
    phone       varchar(255)                           not null,
    email       varchar(255)                           null,
    region      varchar(255)                           null,
    gender      tinyint      default 1                 not null,
    avatar      mediumtext                             not null,
    sign        varchar(255) default '个性签名'        null,
    status      int                                    not null,
    create_time timestamp    default CURRENT_TIMESTAMP not null
)
    collate = utf8mb4_general_ci;

create definer = root@`%` trigger addRobot
    after insert
    on bg_user
    for each row
begin
    insert into bg_friend(user_id, friend_id, remark, status)
    values (new.id, 1, 'Robot', 1),
           (1, new.id, new.username, 1);
    insert into bg_record(sender_id, receiver_id, content, type, status, send_time) value (1, new.id,
                                                                                           '我是机器人小陈，很高兴见到你，欢迎向我提问！如果在软件使用过程中遇到问题，可以通过访问网站https://ken-chy129.cn与作者进行反馈',
                                                                                           0, 0,
                                                                                           (select DATE_FORMAT(current_timestamp, '%m-%d %H:%i')));
end;


insert into bg_user(id, username, password, phone, gender, avatar, sign, status, create_time) value(1, 'Robot', '0', '11111111111', 1, '', '机器人小陈', 1, '2023-06-01 00:00:00')