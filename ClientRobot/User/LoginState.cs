using System;

namespace Game
{
    /// <summary>
    /// 登录状态
    /// </summary>
    enum LoginState
    {
        /// <summary>
        /// 未登录
        /// </summary>
        None = 0,
        /// <summary>
        /// 登录中
        /// </summary>
        Logining,
        /// <summary>
        /// 已登录
        /// </summary>
        Logined,
    }
}
