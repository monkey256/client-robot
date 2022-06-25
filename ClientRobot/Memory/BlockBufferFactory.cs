using System;
using System.Collections.Generic;

namespace Game
{
    class BlockBufferFactory
    {
        int blockSize_;
        int blockStepLength_;
        List<BlockBufferMemory> memoryList_;

        public BlockBufferFactory(int blockSize, int blockStepLength)
        {
            blockSize_ = blockSize;
            blockStepLength_ = blockStepLength;
            memoryList_ = new List<BlockBufferMemory>();
        }

        public BlockBuffer AllocBuffer(int size)
        {
            BlockBuffer r = null;
            foreach (var t in memoryList_)
            {
                if ((r = t.AllocBuffer(size)) != null)
                    break;
            }
            if (r == null)
            {
                var memory = new BlockBufferMemory(blockSize_, blockSize_ * blockStepLength_);
                memoryList_.Add(memory);
                r = memory.AllocBuffer(size);
            }
            return r;
        }
    }
}
