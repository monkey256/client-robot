using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using NetBase;

namespace Game
{
    class ChainWebSocket : IChain
    {
        static NetBase.BlockBufferFactory recvLoopBufferFactory_ = new NetBase.BlockBufferFactory(16384, 1024);

        ClientWebSocket socket_;
        Queue<IBuffer> sendTaskQueue_;
        LoopBuffer recvLoopBuffer_;
        int recvLength_;
        bool isSending_;
        byte[] recvBuffer_ = new byte[4096];

        public override GameAwaiter<bool> ConnectAsync(string ip, int port, int timeoutInMillionSeconds)
        {
            var r = new GameAwaiter<bool>();
            if (state_ != ConnectionState.None && state_ != ConnectionState.Disconnected)
            {
                r.SetResult(false);
                return r;
            }

            ip_ = ip;
            port_ = port;
            state_ = ConnectionState.Connecting;
            socket_ = new ClientWebSocket();
            sendTaskQueue_ = new Queue<IBuffer>();
            recvLoopBuffer_ = new LoopBuffer(recvLoopBufferFactory_.AllocBuffer(), 0);
            recvLength_ = 0;
            new Action(async () =>
            {
                var socket = socket_;
                var token = new CancellationTokenSource(timeoutInMillionSeconds);
                var url = $"ws://{ip}:{port}";
                bool hasException = false;
                try
                {
                    await socket.ConnectAsync(new Uri(url), token.Token);
                }
                catch (Exception ex)
                {
                    Log.Error($"WebSocket建立连接失败，url:{url} ex:{ex.Message}");
                    hasException = true;
                }
                BaseApplication.RunInMainThread(() =>
                {
                    if (socket != socket_)
                    {
                        r.SetResult(false);
                        return;
                    }

                    if (token.IsCancellationRequested || hasException)
                    {
                        state_ = ConnectionState.None;
                        socket_ = null;
                        sendTaskQueue_ = null;
                        recvLoopBuffer_.Dispose();
                        recvLoopBuffer_ = null;
                        recvLength_ = 0;
                        r.SetResult(false);
                        return;
                    }

                    beginReceive(socket);
                    state_ = ConnectionState.Established;
                    r.SetResult(true);
                });
            }).Invoke();

            return r;
        }

        public override bool Send(byte[] buffer, int offset, int count)
        {
            if (state_ != ConnectionState.Established)
                return false;

            if (isSending_)
            {
                var ib = BufferAllocHelper.AllocBuffer(count);
                ib.CopyFrom(buffer, offset, count);
                sendTaskQueue_.Enqueue(ib);
                return true;
            }

            isSending_ = true;
            sendImpl(buffer, offset, count);
            return true;
        }

        async void sendImpl(byte[] buffer, int offset, int count)
        {
            var segment = new ArraySegment<byte>(buffer, offset, count);
            var socket = socket_;
            bool hasException = false;
            string errmessage = string.Empty;
            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                hasException = true;
                errmessage = ex.Message;
            }

            BaseApplication.RunInMainThread(() =>
            {
                if (socket != socket_)
                    return;

                if (hasException)
                {
                    Log.Error($"发送消息遇到异常：{errmessage}");
                    OnDisconnected(NetDisconnectType.NetError, errmessage);
                    return;
                }

                isSending_ = false;
                if (sendTaskQueue_.TryDequeue(out IBuffer ib))
                {
                    isSending_ = true;
                    sendImpl(ib.Content, ib.Index, ib.Length);
                    ib.Dispose();
                }
            });
        }

        async void beginReceive(ClientWebSocket socket)
        {
            if (socket_ != socket)
                return;

            var segment = new ArraySegment<byte>(recvBuffer_, 0, recvBuffer_.Length);
            WebSocketReceiveResult result = null;
            bool hasException = false;
            string errmessage = string.Empty;
            try
            {
                result = await socket.ReceiveAsync(segment, CancellationToken.None);
            }
            catch (Exception ex)
            {
                hasException = true;
                errmessage = ex.Message;
            }

            BaseApplication.RunInMainThread(() =>
            {
                if (socket_ != socket)
                    return;

                if (hasException)
                {
                    Log.Error($"接收消息遇到异常：{errmessage}");
                    OnDisconnected(NetDisconnectType.NetError, errmessage);
                    return;
                }

                if (result.CloseStatus.HasValue)
                {
                    var message = $"{result.CloseStatus.Value}:{result.CloseStatusDescription ?? string.Empty}";
                    Log.Debug($"连接断开：{message}");
                    OnDisconnected(NetDisconnectType.RemoteDisconnect, message);
                    return;
                }

                recvLoopBuffer_.Write(recvBuffer_, 0, result.Count);
                recvLength_ += result.Count;
                beginReceive(socket);
                tryTriggerRecvBuffer();
            });
        }

        public override void Disconnect()
        {
            if (state_ == ConnectionState.Connecting || state_ == ConnectionState.Established)
            {
                socket_.CloseAsync(WebSocketCloseStatus.NormalClosure, "主动关闭", CancellationToken.None);
            }
            OnDisconnected(NetDisconnectType.ActiveDisconnect, "主动断开");
        }

        public override void OnDisconnected(NetDisconnectType disconnectType, string extraInfo)
        {
            socket_ = null;
            sendTaskQueue_ = null;
            recvLoopBuffer_?.Dispose();
            recvLoopBuffer_ = null;
            recvLength_ = 0;
            isSending_ = false;
            base.OnDisconnected(disconnectType, extraInfo);
        }

        static byte[] sharedBuffer_ = new byte[16384];
        void tryTriggerRecvBuffer()
        {
            if (recvLength_ < 4 || !recvLoopBuffer_.Read(sharedBuffer_, 0, 4, false))
                return;

            int packetLen = BitConverter.ToInt32(sharedBuffer_, 0);
            if (packetLen > recvLength_)
                return;

            recvLoopBuffer_.Read(sharedBuffer_, 0, packetLen, true);
            recvLength_ -= packetLen;
            OnReceiveLogicData(sharedBuffer_, 4, packetLen - 4);
            tryTriggerRecvBuffer();
        }
    }
}
