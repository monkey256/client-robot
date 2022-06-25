using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf;
using NetBase;

namespace Game
{
    abstract class IChain
    {
        static ProtobufTranslater translater_ = new ProtobufTranslater();
        static Dictionary<string, Action<IChain, IMessage>> notifyHandlers_ =
            new Dictionary<string, Action<IChain, IMessage>>();

        List<KeyValuePair<string, Action<IMessage>>> responseHandlers_;

        protected string ip_;
        protected int port_;
        protected ConnectionState state_ = ConnectionState.None;

        public object Tag { get; set; }
        public byte[] RandomKey { get; set; }

        /// <summary>
        /// 网络错误事件通知，除了主动断开都会回调该事件
        /// </summary>
        public event Action<string> NetErrorEvent;

        /// <summary>
        /// 所有的网络断开都会回到该事件，该事件回调在NetErrorEvent之后
        /// </summary>
        public event Action<NetDisconnectType, string> NetTerminateEvent;

        public IChain()
        {
            responseHandlers_ = new List<KeyValuePair<string, Action<IMessage>>>();
        }

        public static void RegisterNotifyHandler<T>(Action<IChain, T> handler)
            where T : class, IMessage<T>, new()
        {
            ProtobufTranslater.RegisterProtobufType<T>();

            var protoName = typeof(T).FullName;
            notifyHandlers_[protoName] = (c, m) => handler?.Invoke(c, m as T);
        }

        public abstract GameAwaiter<bool> ConnectAsync(string ip, int port, int timeoutInMillionSeconds);
        public abstract bool Send(byte[] buffer, int offset, int count);
        public abstract void Disconnect();

        public virtual void OnReceiveLogicData(byte[] buffer, int offset, int length)
        {
            byte cryptFlag = buffer[offset];
            offset++;
            length--;

            if (cryptFlag == 1)
                Utility.Rc4Algorithm(RandomKey, buffer, offset, length);

            if (!ProtobufTranslater.ParsePBName(buffer, ref offset, ref length, out string protoName))
            {
                Log.Error($"无法识别的名称！");
                return;
            }

            var message = ProtobufTranslater.ParseMessage(protoName, buffer, offset, length);
            if (message == null)
                return;

            for (int i = 0; i < responseHandlers_.Count; ++i)
            {
                var p = responseHandlers_[i];
                if (p.Key == protoName)
                {
                    responseHandlers_.RemoveAt(i);
                    try
                    {
                        p.Value?.Invoke(message);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"处理协议:{protoName} 异常:\n{ex}");
                    }
                    return;
                }
            }

            if (notifyHandlers_.TryGetValue(protoName, out Action<IChain, IMessage> handler))
            {
                try
                {
                    handler?.Invoke(this, message);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"处理协议:{protoName} 异常:\n{ex}");
                }
            }
        }

        public virtual void OnDisconnected(NetDisconnectType disconnectType, string extraInfo)
        {
            if (state_ == ConnectionState.Established)
            {
                while (responseHandlers_.Count > 0)
                {
                    var handler = responseHandlers_.First().Value;
                    responseHandlers_.RemoveAt(0);

                    try
                    {
                        handler.Invoke(null);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"DispatchAllResponseWithNull时遇到异常：\n{ex}");
                    }
                }

                if (disconnectType != NetDisconnectType.ActiveDisconnect)
                {
                    try
                    {
                        NetErrorEvent?.Invoke(extraInfo);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"回调NetErrorEvent遇到错误：{ex.Message}");
                    }
                }

                try
                {
                    NetTerminateEvent?.Invoke(disconnectType, extraInfo);
                }
                catch (Exception ex)
                {
                    Log.Error($"回调NetTerminateEvent遇到错误：{ex.Message}");
                }
            }

            RandomKey = null;
            ip_ = string.Empty;
            port_ = 0;
            state_ = ConnectionState.Disconnected;
        }

        static MemoryStream sharedMemoryStream_ = new MemoryStream();
        static BinaryWriter sharedBinaryWriter_ = new BinaryWriter(sharedMemoryStream_);
        public bool SendMessage(IMessage message)
        {
            bool useEcrypt = RandomKey != null;
            sharedMemoryStream_.SetLength(0);
            sharedBinaryWriter_.Write((int)0);
            sharedBinaryWriter_.Write((byte)(useEcrypt ? 1 : 0));
            sharedBinaryWriter_.Write(Encoding.UTF8.GetBytes(message.GetType().FullName));
            sharedBinaryWriter_.Write((byte)0);
            sharedBinaryWriter_.Write(message.ToByteArray());
            if (useEcrypt)
                Utility.Rc4Algorithm(RandomKey, sharedMemoryStream_.GetBuffer(), 5, (int)sharedMemoryStream_.Length - 5);
            sharedMemoryStream_.Position = 0;
            sharedBinaryWriter_.Write((int)sharedMemoryStream_.Length);

            return Send(sharedMemoryStream_.GetBuffer(), 0, (int)sharedMemoryStream_.Length);
        }

        public GameAwaiter<T> SendRequestAsync<T>(IMessage req) where T : class, IMessage<T>, new()
        {
            var r = new GameAwaiter<T>();
            if (!SendMessage(req))
            {
                r.SetResult(null);
                return r;
            }

            ProtobufTranslater.RegisterProtobufType<T>();
            responseHandlers_.Add(
                new KeyValuePair<string, Action<IMessage>>(
                    typeof(T).FullName, 
                    m => r.SetResult(m as T)
                )
            );
            return r;
        }
    }
}
