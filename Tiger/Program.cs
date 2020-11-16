using System;

namespace Tiger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Tiger.Extractor extractor = new Tiger.Extractor(@"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages", true);

            extractor.extract_binary_package_to_folder(@"C:\D2PkgExtractionPath", extractor.MasterPackageNames[12]);
        }
    }
}
