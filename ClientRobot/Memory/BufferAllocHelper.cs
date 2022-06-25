using System;

namespace Game
{
    /// <summary>
    /// 分配缓冲区辅助类
    /// </summary>
    public static class BufferAllocHelper
    {
        static BlockBufferFactory bufferFactory32_;
        static BlockBufferFactory bufferFactory64_;
        static BlockBufferFactory bufferFactory128_;
        static BlockBufferFactory bufferFactory256_;
        static BlockBufferFactory bufferFactory512_;
        static BlockBufferFactory bufferFactory1024_;
        static BlockBufferFactory bufferFactory2048_;
        static BlockBufferFactory bufferFactory4096_;

        static BufferAllocHelper()
        {
            bufferFactory32_ = new BlockBufferFactory(32, 8192);
            bufferFactory64_ = new BlockBufferFactory(64, 4096);
            bufferFactory128_ = new BlockBufferFactory(128, 2048);
            bufferFactory256_ = new BlockBufferFactory(256, 1024);
            bufferFactory512_ = new BlockBufferFactory(512, 512);
            bufferFactory1024_ = new BlockBufferFactory(1024, 256);
            bufferFactory2048_ = new BlockBufferFactory(2048, 128);
            bufferFactory4096_ = new BlockBufferFactory(4096, 64);
        }

        /// <summary>
        /// 分配指定大小的缓冲区
        /// </summary>
        /// <param name="size">指定大小</param>
        /// <returns>IBuffer对象</returns>
        public static IBuffer AllocBuffer(int size)
        {
            if (size < 0)
                return null;
            else if (size <= 32)
                return bufferFactory32_.AllocBuffer(size);
            else if (size <= 64)
                return bufferFactory64_.AllocBuffer(size);
            else if (size <= 128)
                return bufferFactory128_.AllocBuffer(size);
            else if (size <= 256)
                return bufferFactory256_.AllocBuffer(size);
            else if (size <= 512)
                return bufferFactory512_.AllocBuffer(size);
            else if (size <= 1024)
                return bufferFactory1024_.AllocBuffer(size);
            else if (size <= 2048)
                return bufferFactory2048_.AllocBuffer(size);
            else if (size <= 4096)
                return bufferFactory4096_.AllocBuffer(size);
            return new NormalBuffer(size);
        }
    }
}
