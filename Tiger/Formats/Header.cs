using System;
namespace Tiger.Formats
{
    public enum PackageLanguage
    {
        None = 0,
        English = 1,
        French = 2,
        Italian = 3,
        German = 4,
        Spanish = 5,
        Japanese = 6,
        Portuguese = 7,
        Russian = 8,
        Polish = 9,
        Simplified_Chinese = 10,
        Traditional_Chinese = 11,
        Latin_American_Spanish = 12,
        Korean = 13,
    };

    /// <summary>
    /// A class used to represent the headers of the package (.pkg) files
    /// </summary>
    public class Header
    {
        public UInt16 version;
        public UInt16 platform;
        public UInt16 package_id;
        public bool isPackage;
        public bool isStartupPackage;
        public DateTime build_date;
        public UInt32 build_id;
        public UInt32 patch_id;
        public PackageLanguage language;
        public UInt32 signature_offset;
        public UInt32 entry_table_offset;
        public UInt32 entry_table_size;
        public UInt32 block_table_offset;
        public UInt32 block_table_size;
    }
}
