using System;

namespace Game
{
    class BlockBuffer : IBuffer
    {
        byte[] content_;
        int index_;
        int length_;
        BlockBufferMemory blockBufferMemory_;

        public byte[] Content => content_;
        public int Index => index_;
        public int Length => length_;

        internal void SetBuffer(byte[] content, int index, int length, BlockBufferMemory blockBufferMemory)
        {
            content_ = content;
            index_ = index;
            length_ = length;
            blockBufferMemory_ = blockBufferMemory;
        }

        public void CopyFrom(byte[] content, int index, int length)
        {
            Array.Copy(content, index, content_, index_, length);
        }

        public void Dispose()
        {
            if (blockBufferMemory_ != null)
            {
                blockBufferMemory_.FreeBuffer(index_);
                content_ = null;
                index_ = 0;
                length_ = 0;
                blockBufferMemory_ = null;
                BlockBufferMemory.Free(this);
            }
        }
    }
}
