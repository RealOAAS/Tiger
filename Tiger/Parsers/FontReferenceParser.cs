using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Tiger.Parsers
{
    class FontReferenceParser
    {
        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A constructor to the font references parser.
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public FontReferenceParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.FontReference)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a font refernces of the block type 0x{ ((uint)Tiger.Blocks.Type.FontReference).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
        }

        /// <summary>
        /// A constructor to the string references parser.
        /// </summary>
        /// <param name="entry_reference">An entry reference object containing information on the package and entry containing the entry</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public FontReferenceParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.FontReference)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a font refernces of the block type 0x{ ((uint)Tiger.Blocks.Type.FontReference).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
        }

        /// <summary>
        /// A method used to parse the FontReference file and return a ParsedFile 
        /// </summary>
        /// <returns>A ParsedFile object of the font data</returns>
        public ParsedFile Parse()
        {
            byte[] entry_data = extractor.extract_entry_data(package, (int)entry_index).data;

            byte[] font_data;
            string font_full_name;
            using (MemoryStream mem_stream = new MemoryStream(entry_data))
            {
                using(BinaryReader bin_reader = new BinaryReader(mem_stream))
                {
                    UInt64 file_size = bin_reader.ReadUInt64();
                    Utils.EntryReference font_reference = new Utils.EntryReference((uint)bin_reader.ReadUInt64());
                    UInt64 name_offset = (UInt64)mem_stream.Position + (UInt64)bin_reader.ReadUInt64();
                    UInt64 font_size = bin_reader.ReadUInt64();
                    UInt64 block_type = bin_reader.ReadUInt64();
                    UInt64 name_size = bin_reader.ReadUInt64();

                    font_full_name = Encoding.UTF8.GetString(bin_reader.ReadBytes((int)name_size -1 ));
                    font_data = extractor.extract_entry_data(font_reference).data;
                }
            }

            string font_name = font_full_name.Split(".")[0];
            string font_extension = font_full_name.Split(".")[1];

            return new ParsedFile(font_extension, font_data, package.package_id, entry_index, font_name);
        }
    }
}
