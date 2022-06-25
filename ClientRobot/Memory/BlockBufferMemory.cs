using System;
using System.Collections.Generic;

namespace Game
{
    class BlockBufferMemory
    {
        static Stack<BlockBuffer> pool_ = new Stack<BlockBuffer>();

        public static BlockBuffer Alloc() =>
            pool_.Count > 0 ? pool_.Pop() : new BlockBuffer();

        public static void Free(BlockBuffer block) =>
            pool_.Push(block);

        int currentIndex_;
        int blockSize_;
        int totalBufferSize_;
        byte[] memory_;
        Stack<int> freeIndexPool_;

        public BlockBufferMemory(int blockSize, int totalBufferSize)
        {
            currentIndex_ = 0;
            blockSize_ = blockSize;
            totalBufferSize_ = totalBufferSize;
            freeIndexPool_ = new Stack<int>();
            memory_ = new byte[totalBufferSize];
        }

        public BlockBuffer AllocBuffer(int size)
        {
            BlockBuffer r = null;
            if (freeIndexPool_.Count > 0)
            {
                r = Alloc();
                r.SetBuffer(memory_, freeIndexPool_.Pop(), size, this);
            }
            else if (currentIndex_ + blockSize_ <= totalBufferSize_)
            {
                r = Alloc();
                r.SetBuffer(memory_, currentIndex_, size, this);
                currentIndex_ += blockSize_;
            }
            return r;
        }

        public void FreeBuffer(int index)
        {
            freeIndexPool_.Push(index);
        }
    }
}
