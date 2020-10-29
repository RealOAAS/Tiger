using System;
namespace Tiger.Formats
{
    /// <summary>
    /// A class that represents entries in the entry table of packages (.pkg) files
    /// </summary>
    public class Entry
    {
        public UInt32 entry_a { get; set; }
        public UInt32 entry_b { get; set; }
        public UInt32 entry_c { get; set; }
        public UInt32 entry_d { get; set; }
        public UInt32 reference_id { get; set; }
        public UInt32 reference_package_id { get; set; }
        public UInt32 reference_unknown_id { get; set; }
        public UInt32 starting_block { get; set; }
        public UInt32 starting_block_offset { get; set; }
        public UInt32 file_size { get; set; }
        public UInt32 unknown { get; set; }
        public UInt32 flags { get; set; }
        public UInt32 subtype { get; set; }
        public UInt32 type { get; set; }

        /// <summary>
        /// The main constructor to the Entry class
        /// </summary>
        /// <param name="entry_a">The first 32 bits of the 128 bit entry</param>
        /// <param name="entry_b">The second 32 bits of the 128 bit entry</param>
        /// <param name="entry_c">The third 32 bits of the 128 bit entry</param>
        /// <param name="entry_d">The fourth 32 bits of the 128 bit entry</param>
        public Entry(UInt32 entry_a, UInt32 entry_b, UInt32 entry_c, UInt32 entry_d)
        {
            this.entry_a = entry_a;
            this.entry_b = entry_b;
            this.entry_c = entry_c;
            this.entry_d = entry_d;

            reference_id = this.entry_a & 0x1FFF;
            reference_package_id = (this.entry_a >> 13) & 0x3FF;
            reference_unknown_id = (this.entry_a >> 23) & 0x3FF;
            starting_block = this.entry_c & 0x3FFF;
            starting_block_offset = ((this.entry_c >> 14) & 0x3FFF) << 4;
            file_size = (this.entry_d & 0x3FFFFFF) << 4 | (this.entry_c >> 28) & 0xF;
            unknown = (this.entry_d >> 26) & 0x3F;

            flags = this.entry_b;
            subtype = (this.entry_b >> 6) & 0x7;
            type = (this.entry_b >> 9) & 0x7F;

            uint LastTwoFlags = reference_unknown_id & 0x3;
            reference_package_id = (LastTwoFlags == 1) ? reference_package_id : reference_package_id | (uint)((int)0x100 << (int)LastTwoFlags);
        }

        /// <summary>
        /// A method to get the number of blocks that the entry has
        /// </summary>
        /// <returns>The block count of the entry</returns>
        public UInt32 block_count()
        {
            return ((starting_block_offset) + file_size + 262144 - 1) / 262144;
        }
    }
}
