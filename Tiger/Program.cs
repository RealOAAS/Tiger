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
            Extractor extractor = new Extractor(destiny_path, LoggerLevels.Disabled);
        }
    }
}
