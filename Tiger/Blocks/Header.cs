using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.Blocks
{
    public struct Header
    {
        public UInt64 count { get; private set; }
        public UInt64 offset { get; private set; }

        public Header(UInt64 count, UInt64 offset)
        {
            this.count = count;
            this.offset = offset;
        }
    }
}
