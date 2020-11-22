using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiger
{
    class Program
    {
        public static void Main(string[] args)
        {
            string destiny_path = @"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages";
            string extraction_path = @"C:\D2PkgExtractionPath";
            Extractor extractor = new Extractor(destiny_path, LoggerLevels.HighVerbouse);
        }
    }
}

namespace Tiger
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string packages_path = @"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages";
            string output_path = @"C:\DestinyExtractionPath";
            Extractor extractor = new Extractor(packages_path, LoggerLevels.HighVerbouse);

            foreach (Package package in extractor.master_packages_stream())
            {
                for(int entry_index = 0; entry_index<package.entry_table().Count; entry_index++)
                {
                    Parsers.ParsedFile file;

                    Formats.Entry entry = package.entry_table()[entry_index];
                    switch(entry.entry_a)
                    {
                        case (uint)Blocks.Type.StringBank:
                            file = new Parsers.StringBankParser(package, entry_index, extractor).Parse();
                            file.WriteToFile(output_path);
                            break;

                        case (uint)Blocks.Type.StringReferenceIndexer:
                            file = new Parsers.StringReferenceIndexerParser(package, entry_index, extractor).Parse();
                            file.WriteToFile(output_path);
                            break;
                    
                        //Other cases for other block types go here
                    }
                }
            }
        }
    }
}