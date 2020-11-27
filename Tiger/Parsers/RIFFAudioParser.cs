using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tiger.Parsers
{
    /// <summary>
    /// A class used to parse RIFF audio files in Destiny 2: Beyond Light.
    /// As of beyond light, RIFF file formats are entries with the entry
    /// type 26 and subtype 7. The first four bytes in a RIFF file is a
    /// magic or header number of 'RIFF'.
    /// </summary>
    public class RIFFAudioParser
    {
        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A constructor to the RIFF Audio Parser
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public RIFFAudioParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (entry.type != (int)Entries.Types.ThirdParty || entry.subtype != (int)Entries.Subtypes.ThirdParty.RIFF)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a RIFF file of the entry type {(int)Entries.Types.ThirdParty} and subtype {(int)Entries.Subtypes.ThirdParty.RIFF}. Instead, recieved an entry of type {entry.type} and subtype {entry.subtype}");
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
        public RIFFAudioParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (entry.type != 26 || entry.subtype != 7)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a RIFF file of the entry type 26 and subtype 7. Instead, recieved an entry of type {entry.type} and subtype {entry.subtype}");
        }

        /// <summary>
        /// A method used to parse RIFF WEM files and then return it as a ParsedFile with .ogg data
        /// </summary>
        /// <returns>A ParsedFile object of the process RIFF data as .ogg files</returns>
        public ParsedFile Parse()
        {
            byte[] riff_data = extractor.extract_entry_data(package, (int)entry_index).data;
            byte[] processed_audio;

            using(MemoryStream mem_stream = new MemoryStream(riff_data))
            {
                processed_audio = Tiger.Parsers.Dependencies.WemConverter.ConvertSoundFile(mem_stream).ToArray();
            }
            return new ParsedFile("ogg", processed_audio, package.package_id, entry_index);
        }
    }
}
