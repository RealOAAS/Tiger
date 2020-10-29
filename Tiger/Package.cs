using System;
using System.IO;
using System.Collections.Generic;

namespace Tiger
{
    /// <summary>
    /// A class used to describe the .pkg files in the Destiny game files.
    /// </summary>
    /// <remarks>
    /// You should ideally not need to instantiate this class manually. Use the factory methods in 
    /// Tiger.Extractor to instantiate packages using their name, package_id, and patch_id.
    /// </remarks>
    public class Package
    {
        private Tiger.Formats.Header header_holder = null;
        private List<Tiger.Formats.Entry> entry_table_holder = null;
        private List<Tiger.Formats.Block> block_table_holder = null;

        public string name { get; private set; }
        public string path { get; private set; }
        public uint patch_id { get { return this.header().patch_id; } }
        public uint package_id { get { return this.header().package_id; } }
        public string no_patch_id_name { get { return Tiger.Utils.remove_patch_id_from_name(this.name); } }

        /// <summary>
        /// The main constructor to the packages class 
        /// </summary>
        /// <param name="packages_path">The path to the packages directory</param>
        /// <param name="package_name">The package name. Example: w64_ui_09be_3.pkg</param>
        public Package(string packages_path, string package_name)
        {
            this.name = package_name;
            this.path = Path.Join(packages_path, name);
        }

        /// <summary>
        /// A method used to get the header of the package and parse it into a Tiger.Formats.Header
        /// </summary>
        /// <remarks>
        /// The reason why this method is used is to not parse the header unless it is needed. Thus,
        /// When a Package is initialized, the header is not parsed until it's actually called. Once
        /// the header has been parsed once, it wont be parsed again and again for efficiency.
        /// </remarks>
        /// <returns>A Tiger.Formats.Header of the parsed header</returns>
        public Tiger.Formats.Header header()
        {
            if (header_holder != null)
                return header_holder;

            Logger.log($"Header not cached. Building header for {this.name}");

            header_holder = new Tiger.Formats.Header();
            using (FileStream File = new FileStream(Path.Combine(this.path), FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader BinReader = new BinaryReader(File))
                {
                    header_holder.version = BinReader.ReadUInt16();
                    header_holder.platform = BinReader.ReadUInt16();
                    header_holder.package_id = BinReader.ReadUInt16();

                    File.Seek(16, 0);
                    header_holder.build_date = new DateTime(1970, 1, 1).AddSeconds(BinReader.ReadInt64());

                    File.Seek(26, 0);
                    header_holder.new_flag = BinReader.ReadUInt16();

                    File.Seek(32, 0);
                    header_holder.patch_id = BinReader.ReadUInt16();

                    File.Seek(36, 0);
                    header_holder.build_string = BinReader.ReadUInt32();

                    File.Seek(176, 0);
                    header_holder.signature_offset = BinReader.ReadUInt32();

                    File.Seek(272, 0);
                    header_holder.new_table_offset = BinReader.ReadUInt32();

                    Logger.log($"Package {name} new header structure: {header_holder.new_flag == 1}");

                    if (header_holder.new_flag != 1)
                    {
                        //This would mean that the package uses the old header structure
                        File.Seek(180, 0);
                        header_holder.entry_table_size = BinReader.ReadUInt32();
                        header_holder.entry_table_offset = BinReader.ReadUInt32();
                        header_holder.entry_table_hash = BinReader.ReadBytes(20);

                        header_holder.block_table_size = BinReader.ReadUInt32();
                        header_holder.block_table_offset = BinReader.ReadUInt32();
                        header_holder.block_table_hash = BinReader.ReadBytes(20);
                    }
                    else
                    {
                        //using the new package structure
                        File.Seek(header_holder.new_table_offset + 16, 0);
                        header_holder.entry_table_size = BinReader.ReadUInt16();

                        File.Seek(header_holder.new_table_offset + 24, 0);
                        header_holder.entry_table_offset = BinReader.ReadUInt32() + header_holder.new_table_offset + 40;

                        File.Seek(header_holder.new_table_offset + 32, 0);
                        header_holder.block_table_size = BinReader.ReadUInt16();

                        File.Seek(header_holder.new_table_offset + 40, 0);
                        header_holder.block_table_offset = BinReader.ReadUInt32() + header_holder.new_table_offset + 56;
                    }

                    Logger.log($"Package {name} has {header_holder.entry_table_size} entries at an offset of 0x{header_holder.entry_table_offset.ToString("X4")}");
                }
            }

            return header_holder;
        }  

        /// <summary>
        /// A method used to get the entry table for the package. 
        /// </summary>
        /// <returns>Returns a list of Tiger.Formats.Entry</returns>
        public List<Tiger.Formats.Entry> entry_table()
        {
            if (entry_table_holder != null)
                return entry_table_holder;

            entry_table_holder = new List<Tiger.Formats.Entry>();

            using (FileStream File = new FileStream(this.path, FileMode.Open, FileAccess.Read))
            {
                File.Seek(header().entry_table_offset, 0);
                using (BinaryReader BinReader = new BinaryReader(File))
                {
                    for (int i = 0; i < header().entry_table_size; i++)
                    {
                        UInt32 EntryA = BinReader.ReadUInt32();
                        UInt32 EntryB = BinReader.ReadUInt32();
                        UInt32 EntryC = BinReader.ReadUInt32();
                        UInt32 EntryD = BinReader.ReadUInt32();

                        entry_table_holder.Add(new Tiger.Formats.Entry(EntryA, EntryB, EntryC, EntryD));
                    }
                }
            }

            return entry_table_holder;
        }

        /// <summary>
        /// A method used to get the block table for the package
        /// </summary>
        /// <returns>Returns a list of Tiger.Formats.Block</returns>
        public List<Tiger.Formats.Block> block_table()
        {
            if (block_table_holder != null)
                return block_table_holder;

            block_table_holder = new List<Tiger.Formats.Block>();

            using (FileStream File = new FileStream(this.path, FileMode.Open, FileAccess.Read))
            {
                File.Seek(header().block_table_offset, 0);
                using (BinaryReader BinReader = new BinaryReader(File))
                {
                    for (int i = 0; i < header().block_table_size; i++)
                    {
                        block_table_holder.Add(new Tiger.Formats.Block()
                        {
                            Offset = BinReader.ReadUInt32(),
                            Size = BinReader.ReadUInt32(),
                            PatchID = BinReader.ReadUInt16(),
                            Flags = BinReader.ReadUInt16(),
                            Hash = BinReader.ReadBytes(20),
                            GCMTag = BinReader.ReadBytes(16),
                        });
                    }
                }
            }

            return block_table_holder;
        }
    }
}
