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
    /// A class used to parse USM files. In Destiny 2: Beyond Light USM files are 
    /// files of the type 27 and subtype 1.
    /// </summary>
    class USMVideoParser
    {
        private static string temporary_extraction_path = ".\\expath_temp";
        private static VGMToolbox.MpegStream.DemuxOptionsStruct options_struct = new VGMToolbox.MpegStream.DemuxOptionsStruct()
        {
            ExtractAudio = true,
            ExtractVideo = true,
            AddHeader = false,
            SplitAudioStreams = false,
            AddPlaybackHacks = false,
        };
        private static Process ffmpeg_process = new Process();
        private static Process audio_converter_process = new Process();

        private static uint video_hash = 40534656;
        private static uint music_hash = 40534641;
        private static uint english_audio_hash = 41534641;

        public Package package { get; private set; }
        public Formats.Entry entry { get; private set; }
        public uint entry_index { get; private set; }
        private Extractor extractor { get; set; }

        /// <summary>
        /// A static constructor used to initialize the temortary extraction path and initialize the ffmpeg process
        /// </summary>
        static USMVideoParser()
        {
            ffmpeg_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpeg_process.StartInfo.FileName = "ffmpeg.exe";

            audio_converter_process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            audio_converter_process.StartInfo.FileName = "./AudioConverter/AudConv.exe";

            Directory.CreateDirectory(temporary_extraction_path);
        }

        /// <summary>
        /// A constructor to the USM Video Parser
        /// </summary>
        /// <param name="package">The package to obtain the entry from</param>
        /// <param name="entry_index">The index of the entry in the package's entry table</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public USMVideoParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.package = package;
            this.entry = package.entry_table()[entry_index];
            this.extractor = extractor;
            this.entry_index = (uint)entry_index;

            if (entry.type != (int)Entries.Types.Video || entry.subtype != (int)Entries.Subtypes.Video.USM)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a USM file of the entry type {(int)Entries.Types.Video} and subtype {(int)Entries.Subtypes.Video.USM}. Instead, recieved an entry of type {entry.type} and subtype {entry.subtype}");
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
        public USMVideoParser(Tiger.Utils.EntryReference entry_reference, Tiger.Extractor extractor)
        {
            this.package = extractor.package(entry_reference.package_id);
            this.entry = this.package.entry_table()[(int)entry_reference.entry_index];
            this.extractor = extractor;
            this.entry_index = entry_reference.entry_index;

            if (entry.type != 27 || entry.subtype != 1)
                throw new Tiger.Parsers.InvalidTypeError($"Expected a USM file of the entry type 27 and subtype 1. Instead, recieved an entry of type {entry.type} and subtype {entry.subtype}");
        }

        public ParsedFile Parse()
        {
            byte[] entry_data = extractor.extract_entry_data(package, (int)entry_index).data;
            string file_name = Tiger.Utils.entry_name(package.package_id, entry_index);
            string file_path = Path.Combine(temporary_extraction_path, file_name);
            File.WriteAllBytes(file_path, entry_data);

            VGMToolbox.CriUsmStream usm_stream = new VGMToolbox.CriUsmStream(file_path);
            usm_stream.DemultiplexStreams(options_struct);

            //The files have now been split and are no longer in their USM format.
            bool hasMusic = File.Exists(Path.Combine(temporary_extraction_path, $"{file_name}_{music_hash}.adx"));
            bool hasAudio = File.Exists(Path.Combine(temporary_extraction_path, $"{file_name}_{english_audio_hash}.adx"));
            
            //Converting .adx audios to mp3
            if(hasAudio)
            {
                audio_converter_process.StartInfo.Arguments = $"-o \"{Path.Combine(temporary_extraction_path, file_name)}_{english_audio_hash}.mp3\" \"{Path.Combine(temporary_extraction_path, file_name)}_{english_audio_hash}.adx\" ";
                audio_converter_process.Start();
                audio_converter_process.WaitForExit();
            }
            if (hasMusic)
            {
                audio_converter_process.StartInfo.Arguments = $"-o \"{Path.Combine(temporary_extraction_path, file_name)}_{music_hash}.mp3\" \"{Path.Combine(temporary_extraction_path, file_name)}_{music_hash}.adx\" ";
                audio_converter_process.Start();
                audio_converter_process.WaitForExit();
            }

            //Adding the audios to the video file
            string ffmpeg_arguments = $"-i \"{Path.Combine(temporary_extraction_path, file_name)}_{video_hash}.m2v\" {(hasAudio ? ($"-i \"{Path.Combine(temporary_extraction_path, file_name)}_{english_audio_hash}.mp3\"") : "")} {(hasMusic ? ($"-i \"{Path.Combine(temporary_extraction_path, file_name)}_{music_hash}.mp3\"") : "")} -filter_complex \"[1]{(hasMusic && hasAudio ? "[2]" : "")}amix=inputs={(hasAudio && hasMusic ? 2 : 1)}[a]\" -map 0:v -map \"[a]\" -c:v copy \"{Path.Combine(temporary_extraction_path, file_name)}.mp4\"";
            ffmpeg_process.StartInfo.Arguments = ffmpeg_arguments;
            ffmpeg_process.Start();
            ffmpeg_process.WaitForExit();

            //Reading the video data
            byte[] video_data = File.ReadAllBytes(Path.Combine(temporary_extraction_path, $"{file_name}.mp4"));

            //Deleting all of the files in the temporary extraction path.
            string[] files = Directory.GetFiles(temporary_extraction_path);
            foreach (string filename in files)
                File.Delete(filename);

            return new ParsedFile("mp4", video_data, package.package_id, entry_index);
        }
    }
}
