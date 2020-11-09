using System;

namespace Tiger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Tiger.Extractor extractor = new Tiger.Extractor(@"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages", true);

            byte[] data = extractor.extract_entry_data(extractor.MasterPackageNames[3], 19);
        }
    }
}
