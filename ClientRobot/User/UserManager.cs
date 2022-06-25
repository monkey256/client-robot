using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using NetBase;

namespace Game
{
    static class UserManager
    {
        static Timer keepAliveTimer_ = new Timer();
        static Dictionary<string, IUser> guidUsers_ = new Dictionary<string, IUser>();

        public static void Init()
        {
            RegisterNotifyHandler<CLGT.DisconnectNtf>((user, ntf) => user.OnDisconnectNtf(ntf));
            keepAliveTimer_.Start(10000, dt => onKeepAliveTimer());
        }

        public static void UnInit()
        {
            keepAliveTimer_.Stop();
        }

        public static void AddUser(IUser user)
        {
            guidUsers_[user.Guid] = user;
        }

        public static void RemoveUser(IUser user)
        {
            if (guidUsers_.TryGetValue(user.Guid, out IUser tuser) && ReferenceEquals(user, tuser))
                guidUsers_.Remove(user.Guid);
        }

        public static void RegisterNotifyHandler<T>(Action<IUser, T> handler)
            where T : class, IMessage<T>, new()
        {
            IChain.RegisterNotifyHandler<T>((_c, _m) => handler?.Invoke(_c.Tag as IUser, _m));
        }

        static void onKeepAliveTimer()
        {
            foreach (var user in guidUsers_.Values)
            {
                _ = user.Send(new CLGT.KeepAliveReq());
            }
        }
    }
}
