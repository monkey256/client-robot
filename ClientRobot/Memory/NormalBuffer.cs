using System;

namespace Game
{
    class NormalBuffer : IBuffer
    {
        byte[] content_;

        public byte[] Content => content_;
        public int Index => 0;
        public int Length => content_.Length;

        public NormalBuffer(int length)
        {
            content_ = new byte[length];
        }

        public void CopyFrom(byte[] content, int index, int length)
        {
            Array.Copy(content, index, content_, 0, length);
        }

        public void Dispose()
        {
            content_ = null;
        }
    }
}
