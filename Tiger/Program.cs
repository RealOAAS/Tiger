using System;
using System.Text;
using System.Collections.Generic;
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

            var x = new Tiger.Parsers.AudioBankParser(extractor.package(0x106), 0x1ffa, extractor).Parse();
        }
    }
}
