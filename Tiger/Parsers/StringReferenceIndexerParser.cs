using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tiger.Parsers
{
    /// <summary>
    /// A class used to parse the StringReferenceIndexer file which is a file
    /// of the block type 0x80805A09 as of Destiny 2: Beyond Light.
    /// </summary>
    class StringReferenceIndexerParser
    {
        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A constructor to the StringReferenceIndexerParser class
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public StringReferenceIndexerParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (entry.entry_a != (uint)Blocks.Type.StringReferenceIndexer)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a string refernces indexer of the block type 0x{ ((uint)Tiger.Blocks.Type.StringReferenceIndexer).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
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
        public StringReferenceIndexerParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (entry.entry_a != (uint)Blocks.Type.StringReferenceIndexer)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a string refernces indexer of the block type 0x{ ((uint)Tiger.Blocks.Type.StringReferenceIndexer).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}"); ;
        }

        /// <summary>
        /// A parser method used to parse the StringReferenceIndexer and then return it as a 
        /// dictionary blob
        /// </summary>
        /// <returns>A ParsedFile object of a json object</returns>
        public ParsedFile Parse()
        {
            Dictionary<uint, Dictionary<uint, string>> indexed_strings_dictionary = new Dictionary<uint, Dictionary<uint, string>>();

            byte[] file_data = extractor.extract_entry_data(package, (int)entry_index).data;
            using(MemoryStream mem_stream = new MemoryStream(file_data))
            {
                using (BinaryReader bin_reader = new BinaryReader(mem_stream))
                {
                    UInt64 file_size = bin_reader.ReadUInt64();
                    Blocks.Header header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());

                    mem_stream.Seek((uint)header.offset + 0x10, SeekOrigin.Begin);
                    for(int i = 0; i<(int)header.count; i++)
                    {
                        uint hash = bin_reader.ReadUInt32();
                        Tiger.Utils.EntryReference reference = new Utils.EntryReference(bin_reader.ReadUInt32());
                        if (reference.entry_a == 0xFFFFFFFF)
                            indexed_strings_dictionary[hash] = new Dictionary<uint, string>();
                        else
                            indexed_strings_dictionary[hash] = new Tiger.Parsers.StringReferenceParser(reference, extractor).ParseDeserialize();
                    }
                }
            }

            return new ParsedFile(".json", Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(indexed_strings_dictionary)), package.package_id, entry_index);
        }

        /// <summary>
        /// A method that Parses the file but does not serialize it and returns it
        /// as an appropriate structure for the data
        /// </summary>
        /// <returns>A dictionary of the string reference hash and a value of a dictionary of string hash and string</returns>
        public Dictionary<uint, Dictionary<uint, string>> ParseDeserialize()
        {
            ParsedFile file = this.Parse();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<uint, string>>>(Encoding.UTF8.GetString(file.data));
        }
    }
}
