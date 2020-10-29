using System;
namespace Tiger.Formats
{
    /// <summary>
    /// A class used to represent the headers of the package (.pkg) files
    /// </summary>
    public class Header
    {
        public UInt16 version;
        public UInt16 platform;
        public UInt16 package_id;
        public DateTime build_date;
        public UInt16 new_flag;
        public UInt16 patch_id;
        public UInt32 build_string;
        public UInt32 signature_offset;
        public UInt32 entry_table_size;
        public UInt32 entry_table_offset;
        public byte[] entry_table_hash = new byte[20];
        public UInt32 block_table_size;
        public UInt32 block_table_offset;
        public byte[] block_table_hash = new byte[20];
        public UInt32 new_table_offset;
    }
}
