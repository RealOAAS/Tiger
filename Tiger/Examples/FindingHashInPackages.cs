using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiger.Examples
{
    /// <summary>
    /// An example of how to use the extractor class to find a specific hash
    /// that is somewhere in the entires. In this example, we will try to find
    /// the hash 00 00 D4 72 9D 23 F2 F5 across all packages and we will only
    /// find these hashes in entries of type 8 or 16
    /// </summary>
    class FindingHashInPackages
    {
        public static void Example()
        {
            byte[] needle = new byte[] { 0x00, 0x00, 0xAC, 0x9B, 0x31, 0x95, 0xDB, 0xB0 };

            string packages_path = @"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages";
            Extractor extractor = new Extractor(packages_path, LoggerLevels.HighVerbouse);

            //Iterating over all of the master packages
            Parallel.ForEach(extractor.master_packages_stream(), package =>
            {
                for(int entry_indexer = 0; entry_indexer<package.entry_table().Count; entry_indexer++)
                {
                    Formats.Entry entry = package.entry_table()[entry_indexer];
                    if(entry.type == 8 || entry.type == 16)
                    {
                        byte[] data = extractor.extract_entry_data(package, entry_indexer).data;
                        int index_of_needle = Tiger.Utils.BoyerMoore.IndexOf(data, needle);

                        if (index_of_needle != -1)
                            Console.WriteLine($"\t{package.no_patch_id_name} {Tiger.Utils.entry_name(package.package_id, (uint)entry_indexer)}");
                    }
                }
            });
        }
    }
}
