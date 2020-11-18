using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace Tiger.Parsers
{
    /// <summary>
    /// A class used to parse image files in Destiny 2: Beyond Light. 
    /// In this version of the game. A texture header is an entry of type
    /// 32 and subtype of 1. It has a 64 byte long header which this method 
    /// Can analyse, understand, and parse into something useful. As of now,
    /// the texture data themselves are stored in entries of type 40 and 48.
    /// </summary>
    class TextureParser
    {
        private Package header_package;
        private Formats.Entry header_entry;

        private Package texture_package;
        private Formats.Entry texture_entry;

        private Tiger.Extractor extractor;

        private static Process raw_tex_process = new Process();
        private static string temporary_extraction_path = "./expath_temp";
        
        /// <summary>
        /// A static constructor to the class used to execute static code to initialize the process
        /// </summary>
        static TextureParser()
        {
            raw_tex_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            raw_tex_process.StartInfo.FileName = "RawtexCmd.exe";

            System.IO.Directory.CreateDirectory(temporary_extraction_path);
        }

        /// <summary>
        /// A method used to initialize a new TextureParser and construct an object. Used to parse
        /// textures in Destiny 2 Beyond Light
        /// </summary>
        /// <param name="package">A package object of the package containing the entry</param>
        /// <param name="entry_index">The index of the entry containing the data</param>
        /// <param name="extractor">An extractor object passed to this class to perform some extraction of other needed files</param>
        /// <remarks>
        /// The extractor object passed to this class is typically the extractor object calling this class in the first place.
        /// This is done to reduce the latency and the initialization that each extractor must go through.
        /// </remarks>
        public TextureParser(Tiger.Package package, int entry_index, Tiger.Extractor extractor)
        {
            this.extractor = extractor;
            Formats.Entry temporary_entry = package.entry_table()[entry_index];
            if (temporary_entry.type == 32 && temporary_entry.subtype == 1)
            {
                header_package = package;
                header_entry = temporary_entry;

                texture_package = extractor.package(header_entry.reference_package_id);
                texture_entry = texture_package.entry_table()[(int)header_entry.reference_id];
            }
            else if( (temporary_entry.type == 40 || temporary_entry.type == 48) && temporary_entry.subtype == 1 )
            {
                texture_package = package;
                texture_entry = temporary_entry;

                header_package = extractor.package(texture_entry.reference_package_id);
                header_entry = header_package.entry_table()[(int)texture_entry.reference_id];
            }
            else
            {
                Logger.log($"Expected entry of type 32, 40, or 48 and subtype 1. Instead, obtained entry of type {temporary_entry.type} and subtype {temporary_entry.subtype}", LoggerLevels.HighVerbouse);
                throw new Tiger.Parsers.InvalidTypeError($"Expected entry of type 32, 40, or 48 and subtype 1. Instead, obtained entry of type {temporary_entry.type} and subtype {temporary_entry.subtype}");
            }
        }

        /// <summary>
        /// A method that parses the entries passed to it and returns a ParsedFile object of the data on this file
        /// </summary>
        /// <returns>A ParsedFile object of the texture</returns>
        public ParsedFile Parse()
        {
            byte[] header_data = extractor.extract_entry_data(header_package, header_entry).data;
            byte[] texture_data = extractor.extract_entry_data(texture_package, texture_entry).data;

            int header_entry_location = header_package.entry_table().IndexOf(header_entry);
            string image_name = Tiger.Utils.entry_name(header_package.package_id, (uint)header_entry_location);

            UInt16 height, width, dxgi_type;
            using (MemoryStream mem_stream = new MemoryStream(header_data))
            {
                using (BinaryReader bin_reader = new BinaryReader(mem_stream))
                {
                    mem_stream.Seek(0x4, 0);
                    dxgi_type = bin_reader.ReadUInt16();

                    mem_stream.Seek(0x22, 0);
                    width = bin_reader.ReadUInt16();
                    height = bin_reader.ReadUInt16();
                }
            }

            //Writing the texture data to the temporary_extraction_path to feed it into RawtexCmd.exe
            File.WriteAllBytes(Path.Combine(temporary_extraction_path, image_name + ".dat"), texture_data);

            raw_tex_process.StartInfo.Arguments = $"{'"' + Path.Combine(temporary_extraction_path, image_name + ".dat") + '"'} {dxgi_type} 0 {width} {height}";
            raw_tex_process.Start();
            raw_tex_process.WaitForExit();

            byte[] image_data = File.ReadAllBytes(Path.Combine(temporary_extraction_path, image_name + ".png"));

            File.Delete(Path.Combine(temporary_extraction_path, image_name + ".dat"));
            File.Delete(Path.Combine(temporary_extraction_path, image_name + ".dds"));
            File.Delete(Path.Combine(temporary_extraction_path, image_name + ".png"));

            return new ParsedFile("png", image_data, header_package.package_id, (uint) header_entry_location);
        }
    }
}
