using System;

namespace Game
{
    /// <summary>
    /// 缓冲区对象
    /// </summary>
    public interface IBuffer : IDisposable
    {
        /// <summary>
        /// 内容缓冲区
        /// </summary>
        byte[] Content { get; }
        /// <summary>
        /// 起始索引
        /// </summary>
        int Index { get; }
        /// <summary>
        /// 数据长度
        /// </summary>
        int Length { get; }

        /// <summary>
        /// 从源缓冲区拷贝内容
        /// </summary>
        /// <param name="content">源缓冲区</param>
        /// <param name="index">起始索引</param>
        /// <param name="length">数据长度</param>
        void CopyFrom(byte[] content, int index, int length);
    }
}
