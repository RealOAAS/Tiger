using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiger.Parsers
{
    /// <summary>
    /// A class made to parse AudioBanks. AudioBanks as of Destiny 2; Beyond Light
    /// Are blocks (type 8 entries) of the type 0x808097b8. These files contain a
    /// number of hashes that make references to audios and their string subtitles,
    /// as well as their narrators.
    /// 
    /// To note, as of Beyond Light, hashes_index_table_header.offset can either point
    /// to an entry of type 0x80809733 which is a single audio, or it point to a block
    /// of the type 0x8080972D which means that its an arraay of audio entries. When listening
    /// to these entries back to back they make a full sentence
    /// </summary>
    public class AudioBankParser
    {
        private static byte[] needle1 = new byte[] { 0x2D, 0x97, 0x80, 0x80 };
        private static byte[] needle2 = new byte[] { 0x33, 0x97, 0x80, 0x80 };

        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A constructor to the Audio Bank parser.
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public AudioBankParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.AudioBank)
                throw new Tiger.Parsers.InvalidTypeError($"Expected an AudioBank of the block type 0x{ ((uint)Tiger.Blocks.Type.AudioBank).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
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
        public AudioBankParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (this.entry.entry_a != (uint)Tiger.Blocks.Type.AudioBank)
                throw new Tiger.Parsers.InvalidTypeError($"Expected an AudioBank of the block type 0x{ ((uint)Tiger.Blocks.Type.AudioBank).ToString("X8") } but recieved a block of type 0x{this.entry.entry_a.ToString("X8")}");
        }

        /// <summary>
        /// A method used to Parse AudioBanks and return the output as a parsed file
        /// </summary>
        /// <returns>A parsed file of the output</returns>
        public ParsedFile Parse()
        {
            byte[] audio_bank_data = extractor.extract_entry_data(package, (int)entry_index).data;

            Dictionary<UInt64, UInt64> hashes_index_dict = new Dictionary<ulong, ulong>();
            Dictionary<uint, List<Dictionary<string, string>>> parsed_audio_bank = new Dictionary<uint, List<Dictionary<string, string>>>();

            using (MemoryStream mem_stream = new MemoryStream(audio_bank_data))
            {
                using (BinaryReader bin_reader = new BinaryReader(mem_stream))
                {
                    UInt64 file_size = bin_reader.ReadUInt64();
                    Blocks.Header hashes_table_header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());
                    Blocks.Header hashes_index_table_header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());

                    //Reading all of the hashes as well as their offsets and adding them tot the dictionary
                    mem_stream.Seek((uint)hashes_index_table_header.offset + 0x10, SeekOrigin.Begin);
                    for(int i = 0; i<(int)hashes_index_table_header.count; i++)
                        hashes_index_dict[bin_reader.ReadUInt64()] = (UInt64)mem_stream.Position + bin_reader.ReadUInt64();
                    
                    //Iterating over the hashes index dictionary entries to begin their analysis
                    foreach(KeyValuePair<UInt64, UInt64> hash_index_pair in hashes_index_dict)
                    {
                        mem_stream.Seek((long)hash_index_pair.Value - 0x4, SeekOrigin.Begin);
                        UInt32 block_header = bin_reader.ReadUInt32();

                        List<AudioBankEntryParser.AudioBankEntry> audio_entries;
                        try
                        {
                            if (block_header == 0x80809733)
                                audio_entries = new List<AudioBankEntryParser.AudioBankEntry>() { new AudioBankEntryParser(audio_bank_data[(Index)mem_stream.Position..(Index)(mem_stream.Position + 0x68)]).Parse() };
                            else if (block_header == 0x8080972D)
                                audio_entries = new AudioBankCollectionsParser(audio_bank_data[(Index)mem_stream.Position..]).Parse();
                            else if (block_header == 0x8080972A)
                            {
                                int index_of_collection = Tiger.Utils.BoyerMoore.IndexOf(audio_bank_data[(Index)mem_stream.Position..], needle1);
                                if (index_of_collection != -1)
                                    audio_entries = new AudioBankCollectionsParser(audio_bank_data[(Index)(mem_stream.Position + index_of_collection + 0x4)..]).Parse();
                                else
                                {
                                    int index_of_audio_entry = Tiger.Utils.BoyerMoore.IndexOf(audio_bank_data[(Index)mem_stream.Position..], needle2);
                                    audio_entries = new List<AudioBankEntryParser.AudioBankEntry>() { new AudioBankEntryParser(audio_bank_data[(Index)(mem_stream.Position + index_of_audio_entry + 0x4)..(Index)(mem_stream.Position + index_of_audio_entry + 0x4 + 0x68)]).Parse() };
                                }
                            }
                            else
                                throw new Exception($"Invalid Header type found at the offset {hash_index_pair.Value - 4}");
                        }
                        catch (EndOfStreamException ex)
                        {
                            continue;
                        }

                        //Working on the audio_entries obtained from the audio_bank
                        List<Dictionary<string, string>> parsed_audio_entries = new List<Dictionary<string, string>>();
                        foreach(AudioBankEntryParser.AudioBankEntry audio_entry in audio_entries)
                        {
                            if (audio_entry.number_of_audios == 0)
                                continue;

                            string narrator_name = extractor.string_lookup_table()[audio_entry.narrator_string_hash];

                            //Working on getting the string
                            string transcript = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<uint, string>>(Encoding.UTF8.GetString(new Tiger.Parsers.StringReferenceParser(audio_entry.subtitle_string_localizer_reference, extractor).Parse().data))[audio_entry.subtitle_stirng_hash];

                            //Getting the audio data
                            byte[] audio_ogg_data;
                            byte[] audio_reference_data = extractor.extract_entry_data(audio_entry.audio_reference).data;
                            using (MemoryStream audio_mem_stream = new MemoryStream(audio_reference_data))
                            {
                                using(BinaryReader audio_bin_reader = new BinaryReader(audio_mem_stream))
                                {
                                    audio_mem_stream.Seek(0x20, SeekOrigin.Begin);
                                    Blocks.Header audio_header = new Blocks.Header(audio_bin_reader.ReadUInt64(), (UInt64)audio_mem_stream.Position + audio_bin_reader.ReadUInt64());
                                    Debug.Assert(audio_header.count == 1);

                                    audio_mem_stream.Seek((uint)audio_header.offset + 0x10, SeekOrigin.Begin);
                                    Tiger.Utils.EntryReference audio_reference = new Utils.EntryReference(audio_bin_reader.ReadUInt32());

                                    audio_ogg_data = new Tiger.Parsers.RIFFAudioParser(audio_reference, extractor).Parse().data;
                                }
                            }

                            string audio_ogg_data_base64 = Convert.ToBase64String(audio_ogg_data);
                            parsed_audio_entries.Add(new Dictionary<string, string>()
                            {
                                {"narrator name", narrator_name },
                                {"narrator hash", audio_entry.narrator_string_hash.ToString() },
                                {"transcript hash", audio_entry.narrator_string_hash.ToString()},
                                {"transcript string", transcript },
                                {"audio hash", audio_entry.audio_hash.ToString() },
                                {"audio ogg data", audio_ogg_data_base64 },
                            });
                        }

                        parsed_audio_bank[(uint)hash_index_pair.Key] = parsed_audio_entries;
                    }
                }
            }

            string parsed_audio_bank_string = Newtonsoft.Json.JsonConvert.SerializeObject(parsed_audio_bank);
            return new ParsedFile("json", Encoding.UTF8.GetBytes(parsed_audio_bank_string), package.package_id, this.entry_index);
        }

        /// <summary>
        /// A private class made to parse AudioEntries. Audio Entries are described as being 
        /// blocks of the type 0x80809733
        /// </summary>
        private class AudioBankEntryParser
        {
            private byte[] audio_entry_bytes { get; set; }

            /// <summary>
            /// The main constructor to the AudioEntryParser class
            /// </summary>
            /// <param name="audio_entry_data"></param>
            public AudioBankEntryParser(byte[] audio_entry_data)
            {
                if (audio_entry_data.Length != 0x68)
                    throw new Exception($"Expected a byte array of data of the length 0x6c but instead recieved data of the length {audio_entry_data.Length.ToString("X4")}"); 

                this.audio_entry_bytes = audio_entry_data;
            }

            /// <summary>
            /// A method used to parse the Audio_entry_bytes and then return them as a coherent AudioBankEntry
            /// </summary>
            /// <returns> An AudioBankEntry object on the references as well as more data on the audio </returns>
            public AudioBankEntry Parse()
            {
                AudioBankEntry audio_entry = new AudioBankEntry();
                using(MemoryStream mem_stream = new MemoryStream(this.audio_entry_bytes))
                {
                    using(BinaryReader bin_reader = new BinaryReader(mem_stream))
                    {
                        mem_stream.Seek(4, SeekOrigin.Current);
                        audio_entry.audio_hash = bin_reader.ReadUInt32();

                        mem_stream.Seek(0x10, SeekOrigin.Current);
                        audio_entry.audio_reference_1 = new Tiger.Utils.EntryReference(bin_reader.ReadUInt32());
                        audio_entry.number_of_audios_1 = bin_reader.ReadUInt32();

                        mem_stream.Seek(0x8, SeekOrigin.Current);
                        audio_entry.subtitle_string_localizer_reference_1 = new Tiger.Utils.EntryReference(bin_reader.ReadUInt32());
                        audio_entry.subtitle_string_hash_1 = bin_reader.ReadUInt32();

                        mem_stream.Seek(0x8, SeekOrigin.Current);
                        audio_entry.audio_reference_2 = new Tiger.Utils.EntryReference(bin_reader.ReadUInt32());
                        audio_entry.number_of_audios_2 = bin_reader.ReadUInt32();

                        mem_stream.Seek(0x8, SeekOrigin.Current);
                        audio_entry.subtitle_string_localizer_reference_2 = new Tiger.Utils.EntryReference(bin_reader.ReadUInt32());
                        audio_entry.subtitle_string_hash_2 = bin_reader.ReadUInt32();

                        mem_stream.Seek(0xc, SeekOrigin.Current);
                        audio_entry.narrator_string_hash = bin_reader.ReadUInt32();
                    }
                }

                return audio_entry;
            }

            /// <summary>
            /// A class made to represent an entry in the AudioBank. As of Beyond Light,
            /// Entries in Audio banks have the block type of 0x80809733
            /// </summary>
            public class AudioBankEntry
            {
                public uint audio_hash { get; set; }
                public uint narrator_string_hash { get; set; }

                public Tiger.Utils.EntryReference audio_reference_1 { private get; set; }
                public uint number_of_audios_1 { private get; set; }

                public Tiger.Utils.EntryReference audio_reference_2 { private get; set; }
                public uint number_of_audios_2 { private get; set; }

                public Tiger.Utils.EntryReference subtitle_string_localizer_reference_1 { private get; set; }
                public uint subtitle_string_hash_1 { private get; set; }

                public Tiger.Utils.EntryReference subtitle_string_localizer_reference_2 { private get; set; }
                public uint subtitle_string_hash_2 { private get; set; }

                /// <summary>
                /// A prop whose main purpose is to determine which audio_refernce to choose
                /// </summary>
                public Tiger.Utils.EntryReference audio_reference
                {
                    get
                    {
                        if (audio_reference_1.entry_a == 0xFFFFFFFF || audio_reference_1.entry_a == 0x811C9DC5)
                            if (audio_reference_2.entry_a == 0xFFFFFFFF || audio_reference_2.entry_a == 0x811C9DC5)
                                throw new Exception("Audio references are both invalid");
                            else
                                return audio_reference_2;

                        return audio_reference_1;
                    }
                }

                /// <summary>
                /// A prop that returns the number of audios depending on the audio_reference chosen as correct
                /// </summary>
                public uint number_of_audios
                {
                    get
                    {
                        if (number_of_audios_1 == 0 && number_of_audios_2 == 0)
                            return 0;

                        return (audio_reference == audio_reference_1 ? number_of_audios_1 : number_of_audios_2);
                    }
                }

                /// <summary>
                /// A prop whose main purpose is to determine which subtitle_string_localizer_reference to choose
                /// </summary>
                public Tiger.Utils.EntryReference subtitle_string_localizer_reference
                {
                    get
                    {
                        if (subtitle_string_localizer_reference_1.entry_a == 0xFFFFFFFF || subtitle_string_localizer_reference_1.entry_a == 0x811C9DC5)
                            if (subtitle_string_localizer_reference_2.entry_a == 0xFFFFFFFF || subtitle_string_localizer_reference_2.entry_a == 0x811C9DC5)
                                throw new Exception("Audio Subtitle references are both invalid");
                            else
                                return subtitle_string_localizer_reference_2;

                        return subtitle_string_localizer_reference_1;
                    }
                }

                /// <summary>
                /// A prop that returns the string hash depending on the subtitle refernce chosen
                /// </summary>
                public uint subtitle_stirng_hash
                {
                    get
                    {
                        return (subtitle_string_localizer_reference == subtitle_string_localizer_reference_1 ? subtitle_string_hash_1 : subtitle_string_hash_2);
                    }
                }
            }
        }

        /// <summary>
        /// A private class used to parse collections of Audio Entries. These collections are 
        /// usually foudn with the block type of 0x8080972D
        /// </summary>
        private class AudioBankCollectionsParser
        {
            private byte[] audio_bank_bytes { get; set; } //This is the entirery of the audio bank AFTER the 80809733 identifier

            /// <summary>
            /// The main constructor to the AudioBankCollectionsParser class
            /// </summary>
            /// <param name="audio_bank_data">The entire audio_bank_data as bytes</param>
            /// <remarks>
            /// The audio_bank_data when provided will NOT be the entire bytes of the audio_bank. Only the protion
            /// right after the 0x80808733 identifier and then all the way to the end. The beginning part would be irrelevant in this case
            /// </remarks>
            public AudioBankCollectionsParser(byte[] audio_bank_data)
            {
                this.audio_bank_bytes = audio_bank_data;
            }

            /// <summary>
            /// A method used to parse the audio collection and then return it as a list of AudioEntry objects
            /// </summary>
            /// <returns> A list of AudioEntry objects </returns>
            public List<AudioBankEntryParser.AudioBankEntry> Parse()
            {
                List < AudioBankEntryParser.AudioBankEntry > audio_bank_entries = new List<AudioBankEntryParser.AudioBankEntry>();
            
                using(MemoryStream mem_stream = new MemoryStream(this.audio_bank_bytes))
                {
                    using (BinaryReader bin_reader = new BinaryReader(mem_stream))
                    {
                        mem_stream.Seek(0x18, SeekOrigin.Current);
                        Blocks.Header header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());

                        if(header.count == 0)
                        {
                            mem_stream.Seek(0x20, SeekOrigin.Current);
                            header = new Blocks.Header(bin_reader.ReadUInt64(), (UInt64)mem_stream.Position + bin_reader.ReadUInt64());
                            Debug.Assert(header.count < 10);

                            if (header.count == 0)
                                return audio_bank_entries;
                        }

                        mem_stream.Seek((uint)header.offset + 0x10, SeekOrigin.Begin);
                        uint offset = bin_reader.ReadUInt32();

                        try
                        {
                            mem_stream.Seek(offset - 4, SeekOrigin.Current);
                            for (int i = 0; i < (int)header.count; i++)
                            {
                                audio_bank_entries.Add(new AudioBankEntryParser(this.audio_bank_bytes[(Index)mem_stream.Position..(Index)(mem_stream.Position + 0x68)]).Parse());
                                mem_stream.Seek(0x70, SeekOrigin.Current);
                            }
                        }
                        catch
                        {
                            return audio_bank_entries;
                        }
                    }
                }

                return audio_bank_entries;
            }
        }
    }
}
