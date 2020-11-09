using System;
namespace Tiger.Formats
{
    /// <summary>
    /// A class representing a block in the package (.pkg) files
    /// </summary>
    public class Block
    {
        public UInt32 offset, size;
        public UInt16 patch_id, flags;
        public byte[] hash = new byte[20];
        public byte[] GCMTag = new byte[16];

        /// <summary>
        /// A method that determines if the block is encrypted
        /// </summary>
        /// <returns>true if the block is encrypted. false if otherwise</returns>
        public bool isEncrypted()
        {
            return (flags & 2) != 0;
        }

        /// <summary>
        /// A method that determines if a block is compressed
        /// </summary>
        /// <returns>true if the block is compressed. false if otherwise</returns>
        public bool isCompressed()
        {
            return (flags & 1) != 0;
        }

        /// <summary>
        /// A method that determines if a block uses the alternate key
        /// </summary>
        /// <returns>true if it uses an alternate key. false if otherwise</returns>
        public bool isAlternateKey()
        {
            return (flags & 4) != 0;
        }
    }
}
