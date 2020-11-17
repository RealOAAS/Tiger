using System;

namespace Tiger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Tiger.Extractor extractor = new Tiger.Extractor(@"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages", true);

            //extractor.extract_binary_package_to_folder(@"C:\D2PkgExtractionPath", 0x102);

            var x = new Tiger.Parsers.TextureParser(extractor.package(0x156), 8, extractor).Parse();
            var n = new Tiger.Parsers.StringBankParser(extractor.package(0x110), 0x29, extractor).Parse();
        }
    }
}
