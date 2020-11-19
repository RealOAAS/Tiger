using System;
using System.IO;
using Newtonsoft;
using System.Text;
using System.Linq;
using System.Collections.Generic;


namespace Tiger.Parsers
{
    /// <summary>
    /// A string bank is a file that contains the strings in an encrypted format. The
    /// File does not make any other references or anything. It just contains the 
    ///encrypted and tangled strings.
    /// </summary>
    class StringBankParser
    {
        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A constructor to the string bank parser.
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public StringBankParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.StringBank)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a string bank of the block type 0x{ ((uint)Tiger.Blocks.Type.StringBank).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
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
        public StringBankParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.StringBank)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a string bank of the block type 0x{ ((uint)Tiger.Blocks.Type.StringBank).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
        }

        /// <summary>
        /// A method used to parse the string banks and return it it as a ParsedFile
        /// </summary>
        /// <returns>A ParsedFile object containing the strings in the string bank</returns>
        /// <remarks>
        /// Take note that the result of this parse is a dictionary made up of these strings and their
        /// index inside of this string bank. The dictionary is serialized and is its saved as a bytes
        /// array in the ParsedFile object.
        /// </remarks>
        public ParsedFile Parse()
        {
            byte[] string_bank_blob = extractor.extract_entry_data(package, (int)entry_index).data;

            int string_count, blocks_count;
            List<int> string_entry_count = new List<int>();
            List<StringBankParser.String> destiny_strings = new List<StringBankParser.String>();

            using(MemoryStream mem_stream = new MemoryStream(string_bank_blob))
            {
                using(BinaryReader bin_reader = new BinaryReader(mem_stream))
                {
                    int file_size = bin_reader.ReadInt32();

                    mem_stream.Seek(4, SeekOrigin.Current);
                    Blocks.Header encryption_metadata_header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());
                    Blocks.Header header_2 = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());
                    Blocks.Header encrypted_data_section_header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());
                    Blocks.Header encrypted_data_block_header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());
                    Blocks.Header strings_meta_data_header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());

                    string_count = (int)strings_meta_data_header.count;
                    blocks_count = (int)encryption_metadata_header.count;

                    //Getting the number of strings in a single string entry takes for later usage
                    mem_stream.Seek((uint)strings_meta_data_header.offset + 0x10, SeekOrigin.Begin);
                    for(int i = 0; i< (int)strings_meta_data_header.count; i++ )
                    {
                        mem_stream.Seek(8, SeekOrigin.Current);
                        string_entry_count.Add(bin_reader.ReadInt32());
                        mem_stream.Seek(4, SeekOrigin.Current);
                    }

                    //Reading the data in the EncryptionMetaDataHeader and adding strings based on that.
                    mem_stream.Seek((uint)encryption_metadata_header.offset + 0x10, SeekOrigin.Begin);
                    for(int i = 0; i< (int)encryption_metadata_header.count; i++)
                    {
                        StringBankParser.String temporary_string_object = new StringBankParser.String();

                        mem_stream.Seek(8, SeekOrigin.Current);
                        temporary_string_object.offset = (uint)mem_stream.Position + bin_reader.ReadUInt32();

                        mem_stream.Seek(8, SeekOrigin.Current);
                        temporary_string_object.bytes_count = bin_reader.ReadUInt16();
                        temporary_string_object.string_length = bin_reader.ReadUInt16();
                        temporary_string_object.key = bin_reader.ReadUInt16();

                        mem_stream.Seek(6, SeekOrigin.Current);
                        destiny_strings.Add(temporary_string_object);
                    }

                    //Getting the encrypted string data for the strings
                    foreach(StringBankParser.String d_string in destiny_strings)
                    {
                        mem_stream.Seek(d_string.offset, SeekOrigin.Begin);
                        d_string.encrypted_string_data = bin_reader.ReadBytes((int)d_string.bytes_count);
                    }
                }
            }

            //Preparing for the strings to be returned
            List<string> return_list = new List<string>();
            List<StringBankParser.String> strings_copy = destiny_strings.ConvertAll(x => x.Clone());

            try
            {
                foreach (int single_count in string_entry_count)
                {
                    StringBuilder string_builder = new StringBuilder();

                    int str_count = single_count == 0 ? 0 : single_count;
                    for (int i = 0; i < str_count; i++)
                    {
                        string_builder.Append(strings_copy[0]);
                        strings_copy.Remove(strings_copy[0]);
                    }

                    return_list.Add(string_builder.ToString());
                }
            }
            catch { }

            char[] trim_characters = new char[] { ' ' };
            string[] strings = return_list.ConvertAll(x => x.Trim(trim_characters)).ToArray();

            //Putting all of the strings in a dictionary
            Dictionary<int, string> strings_dictionary = new Dictionary<int, string>();
            for(int i = 0; i<strings.Length; i++)
            {
                strings_dictionary[i] = strings[i];
            }
            string strings_json = Newtonsoft.Json.JsonConvert.SerializeObject(strings_dictionary);
            return new ParsedFile(".json", Encoding.UTF8.GetBytes(strings_json), package.package_id, entry_index);
        }

        /// <summary>
        /// Private class that defines a string in a string bank. This class should never be used outside of a string bank since it has no real usage outside of it.
        /// </summary>
        private class String
        {
            public uint offset { get; set; }
            public uint bytes_count { get; set; }
            public uint string_length { get; set; }
            public uint key { get; set; }
            public byte[] encrypted_string_data { get; set; }

            /// <summary>
            /// A method used to clone the object. Used to kep the original object safe
            /// </summary>
            /// <returns>
            /// An identical instance of the object
            /// </returns>
            public StringBankParser.String Clone()
            {
                return new StringBankParser.String()
                {
                    offset = this.offset,
                    bytes_count = this.bytes_count,
                    string_length = this.string_length,
                    key = this.key,
                    encrypted_string_data = this.encrypted_string_data,
                };
            }

            /// <summary>
            /// A method to convert the encrypted string to a normal string
            /// </summary>
            /// <returns>A decrypted string</returns>
            override public string ToString()
            {
                int counter = 0;
                byte[] modified_encryped_string_data = new byte[encrypted_string_data.Length];

                try
                {
                    while (counter < encrypted_string_data.Length)
                    {
                        if (encrypted_string_data[counter] < 0xC0)
                        {
                            modified_encryped_string_data[counter] = encrypted_string_data[counter];
                            counter++;
                        }
                        else if (encrypted_string_data[counter] > 0xC0 && encrypted_string_data[counter] < 0xD0)
                        {
                            modified_encryped_string_data[counter + 0] = (byte)(encrypted_string_data[counter + 0] - this.key);
                            modified_encryped_string_data[counter + 1] = encrypted_string_data[counter + 1];
                            counter += 2;
                        }
                        else if (encrypted_string_data[counter] > 0xE0 && encrypted_string_data[counter] < 0xEE)
                        {
                            modified_encryped_string_data[counter + 0] = (byte)(encrypted_string_data[counter + 0] - this.key);
                            modified_encryped_string_data[counter + 1] = (byte)(encrypted_string_data[counter + 1] - this.key);
                            modified_encryped_string_data[counter + 2] = encrypted_string_data[counter + 2];

                            if (encrypted_string_data[counter] == 0xE1 && encrypted_string_data[counter + 1] == 0xBF)
                            {
                                uint CharValue = BitConverter.ToUInt32(new byte[] { encrypted_string_data[counter + 2], encrypted_string_data[counter + 1], encrypted_string_data[counter], 0 }, 0);
                                uint ModifiedCharValue = CharValue + 0xC0C0;
                                byte[] ModifiedCharBytes = BitConverter.GetBytes(ModifiedCharValue);

                                modified_encryped_string_data[counter + 0] = (byte)(ModifiedCharBytes[2] - this.key);
                                modified_encryped_string_data[counter + 1] = (byte)(ModifiedCharBytes[1] - this.key);
                                modified_encryped_string_data[counter + 2] = ModifiedCharBytes[0];
                            }
                            counter += 3;
                        }
                        else
                            return "Non-Supported Language";
                    }
                }
                catch
                {
                    return "Non-Supported Language";
                }

                byte[] DecryptedString = modified_encryped_string_data.ToList().ConvertAll(x => (byte)(x + this.key)).ToArray();
                return Encoding.UTF8.GetString(DecryptedString);
            }
        }
    }
}
