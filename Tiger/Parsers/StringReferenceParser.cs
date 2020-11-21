using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Tiger.Parsers
{
    class StringReferenceParser
    {
        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A constructor to the string references parser.
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public StringReferenceParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.StringReference)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a string refernces of the block type 0x{ ((uint)Tiger.Blocks.Type.StringReference).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
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
        public StringReferenceParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.StringReference)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a string refernces of the block type 0x{ ((uint)Tiger.Blocks.Type.StringReference).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
        }

        /// <summary>
        /// A parser method used to parse the String reference files and return them as a dictionary
        /// </summary>
        /// <returns>A ParsedFile object of a json object</returns>
        public ParsedFile Parse()
        {
            byte[] entry_data = extractor.extract_entry_data(this.package, (int)this.entry_index).data;
            Dictionary<uint, string> string_dictionary = new Dictionary<uint, string>();

            using (MemoryStream mem_stream = new MemoryStream(entry_data))
            {
                using (BinaryReader bin_reader = new BinaryReader(mem_stream))
                {
                    //Reading the headers to the file
                    UInt64 file_size = bin_reader.ReadUInt64();
                    Blocks.Header hashes_header = new Blocks.Header(bin_reader.ReadUInt64(), (uint)mem_stream.Position + bin_reader.ReadUInt64());
                    Tiger.Utils.EntryReference english_string_bank_reference = new Tiger.Utils.EntryReference(bin_reader.ReadUInt32());

                    string[] strings = Tiger.Utils.dictionary_bytes_to_list((new Tiger.Parsers.StringBankParser(extractor.package(english_string_bank_reference.package_id), (int)english_string_bank_reference.entry_index, extractor)).Parse().data);

                    mem_stream.Seek((int)hashes_header.offset + 0x10, SeekOrigin.Begin);
                    for (int i = 0; i< (int)hashes_header.count; i++)
                    {
                        string_dictionary[bin_reader.ReadUInt32()] = strings[i];
                    }
                }
            }

            return new ParsedFile("json", Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(string_dictionary)), package.package_id, entry_index);
        }

        /// <summary>
        /// A method that Parses the file but does not serialize it and returns it
        /// as an appropriate structure for the data
        /// </summary>
        /// <returns>A dictionary of the string hash and the string</returns>
        public Dictionary<uint, string> ParseDeserialize()
        {
            ParsedFile file = this.Parse();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<uint, string>>(Encoding.UTF8.GetString(file.data));
        }
    }
}
