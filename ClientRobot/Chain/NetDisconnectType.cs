using System;
using System.Collections.Generic;

namespace Game
{
    enum NetDisconnectType
    {
        /// <summary>
        /// 主动断开
        /// </summary>
        ActiveDisconnect = 1,
        /// <summary>
        /// 远程断开
        /// </summary>
        RemoteDisconnect,
        /// <summary>
        /// 网络发生错误
        /// </summary>
        NetError,
    }
}
