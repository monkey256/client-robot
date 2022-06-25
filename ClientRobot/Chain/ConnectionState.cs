using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>
    /// 连接状态
    /// </summary>
    enum ConnectionState
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        /// <summary>
        /// 正在连接
        /// </summary>
        Connecting,
        /// <summary>
        /// 已连接
        /// </summary>
        Established,
        /// <summary>
        /// 已断开
        /// </summary>
        Disconnected,
    }
}
