using System;
using System.IO;
using System.Threading.Tasks;

namespace Tiger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Tiger.Extractor extractor = new Tiger.Extractor(@"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages", true);

            //extractor.extract_binary_package_to_folder(@"C:\D2PkgExtractionPath", 0x102);

            //var x = new Tiger.Parsers.TextureParser(extractor.package(0x156), 8, extractor).Parse();
            //var n = new Tiger.Parsers.FontReferenceParser(extractor.package(0x100), 0x39, extractor).Parse();
        }
    }
}
