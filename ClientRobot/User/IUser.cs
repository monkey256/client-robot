using System;
using System.Collections.Generic;
using CLGT;
using CLPF;
using Google.Protobuf;
using NetBase;

namespace Game
{
    class IUser
    {
        IChain chain_ = null;
        LoginState state_ = LoginState.None;
        string device_ = Utility.GenerateCharString(10);

        int loginType_;
        string loginToken_;

        public string Guid { get; private set; }
        public int UserId { get; private set; }
        public string Nickname { get; protected set; }
        public int Gender { get; protected set; }
        public int Head { get; protected set; }
        public string Account { get; protected set; }
        public string Phone { get; protected set; }
        public long Currency { get; protected set; }
        public long BankCurrency { get; protected set; }

        public virtual IChain CreateChain()
        {
            return new ChainWebSocket();
        }

        public async GameAwaiter<string> LoginAsync(string ip, int port, int loginType, string token)
        {
            if (state_ == LoginState.Logining)
                return "正在登录中";

            if (state_ == LoginState.Logined)
                return "已登录";

            var chain = CreateChain();
            chain.NetTerminateEvent += (_t, _m) => onChainTerminate(chain, _t, _m);
            chain.Tag = this;
            chain_ = chain;
            loginType_ = loginType;
            loginToken_ = token;
            state_ = LoginState.Logining;
            bool success = await chain_.ConnectAsync(ip, port, 5000);
            if (chain != chain_)
                return "登录过程被打断";

            if (!success)
            {
                chain_ = null;
                state_ = LoginState.None;
                return "连接服务器失败";
            }

            //发送握手请求
            {
                var req = new HandReq();
                req.Platform = 3;
                req.Product = 1;
                req.Version = 1;
                req.Device = device_;
                req.Channel = 1;
                req.Country = "CN";
                req.Language = "zh-cn";
                var ack = await chain.SendRequestAsync<HandAck>(req);
                if (chain != chain_)
                    return "登录过程被打断";

                if (ack?.Errcode != 0)
                    return ack.ToErrorMessage("1无法识别的平台 2无法识别的产品 3版本太老需强更 4拒绝访问 5你的IP已被封禁 6你的设备已被封禁");

                chain.RandomKey = ack.RandomKey.ToByteArray();
            }

            //发送登录请求
            {
                var req = new LoginReq();
                req.LoginType = loginType;
                req.Token = token;
                var ack = await chain.SendRequestAsync<LoginAck>(req);
                if (chain != chain_)
                    return "登录过程被打断";

                if (ack?.Errcode != 0)
                    return ack.ToErrorMessage("1平台服务器不可用 2账号被封禁 3系统繁忙 4系统错误 5系统暂未开放 6认证失败 7暂不支持的登录方式 8设备注册过多 9IP注册过多 10设备登录账号过多 11IP登录账号过多");

                Guid = ack.UserGuid;
                UserId = ack.UserId;
                Nickname = ack.Nickname;
                Gender = ack.Gender;
                Head = ack.Head;
                Account = ack.Account;
                Phone = ack.Phone;
                Currency = ack.Currency;
                BankCurrency = ack.BankCurrency;
            }

            state_ = LoginState.Logined;
            UserManager.AddUser(this);
            return string.Empty;
        }

        public async GameAwaiter Logout()
        {
            if (state_ == LoginState.Logined)
            {
                UserManager.RemoveUser(this);
                await chain_.SendRequestAsync<LogoutAck>(new LogoutReq());
            }

            if (chain_ != null)
            {
                chain_.Disconnect();
                chain_ = null;
            }
            state_ = LoginState.None;
        }

        void onChainTerminate(IChain chain, NetDisconnectType disconnectType, string message)
        {
            if (chain != chain_)
                return;

            if (state_ == LoginState.Logined)
            {
                UserManager.RemoveUser(this);
            }

            chain_ = null;
            state_ = LoginState.None;
        }

        public void OnDisconnectNtf(DisconnectNtf ntf)
        {
            if (state_ != LoginState.Logined)
                return;

            var errmessage = ntf.Code.ToErrorMessage("1连接超时 2被踢下线 3被挤下线 4网关维护 5平台维护 6游戏维护 7与平台服务器断开连接 8与游戏服务器断开连接 9系统错误");
            Log.Error($"{Guid}_{UserId}_{Nickname}被踢下线：{errmessage}");
            UserManager.RemoveUser(this);

            chain_ = null;
            state_ = LoginState.None;
        }

        public bool Send(IMessage message)
        {
            if (chain_ != null)
                return chain_.SendMessage(message);
            return false;
        }

        public async GameAwaiter<T> SendRequestAsync<T>(IMessage req)
            where T : class, IMessage<T>, new()
        {
            T r = default;
            if (chain_ != null)
                r = await chain_.SendRequestAsync<T>(req);
            return r;
        }
    }
}
