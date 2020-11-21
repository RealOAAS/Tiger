using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tiger.Analysis
{
    /// <summary>
    /// A class made that is used to find the the number of occurance of each block
    /// type in the class and then output it as a csv file and also return it as 
    /// as string array.
    /// </summary>
    static class entry_counts_per_package
    {
        /// <summary>
        /// A method used to count the number of times that entirs of the tyoe 8 
        /// repeat within packages and then return it
        /// </summary>
        /// <param name="directory">The directory to output the csv file to (must already exist)</param>
        /// <param name="extractor">An extractor instance used to iterate through the entry table </param>
        public static void count(string directory, Extractor extractor)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"The directory given '{directory}' was not found");

            foreach(Package package in extractor.master_packages_stream())
            {
                Logger.log(package.no_patch_id_name, LoggerLevels.HighVerbouse);

                Dictionary<uint, int> package_counts = new Dictionary<uint, int>();
                for (int i = 0; i < package.entry_table().Count; i++)
                {
                    Formats.Entry entry = package.entry_table()[i];
                    if (entry.type == 8 || entry.type == 16)
                    {
                        if (!package_counts.ContainsKey(entry.entry_a))
                            package_counts[entry.entry_a] = 0;
                        package_counts[entry.entry_a]++;
                    }
                }

                var x = from entry in package_counts orderby entry.Value descending select entry;
                using (FileStream file = new FileStream(Path.Combine(directory, $"{package.no_patch_id_name}.csv"), FileMode.Create))
                {
                    using (StreamWriter streamWriter = new StreamWriter(file))
                    {
                        streamWriter.Write("hash,count\n");
                        foreach (KeyValuePair<uint, int> hash_count_pair in x)
                        {
                            streamWriter.Write($"0x{hash_count_pair.Key.ToString("X8")},{hash_count_pair.Value}\n");
                        }
                    }
                }
            }

        }
    }
}
