using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tiger.Analysis
{
    /// <summary>
    /// A class used to count the overall occurance of entries in packages.
    /// No package name is mentioned. Just the total count of the entries
    /// </summary>
    public static class entry_counts_overall
    {
        /// <summary>
        /// A count method used to count the number of occurances of block types accross packages
        /// </summary>
        /// <param name="directory">The directory to output the csv file to </param>
        /// <param name="file_name">The filename of the csv file</param>
        /// <param name="extractor">An extractor object used in the analysis</param>
        public static void count(string directory, string file_name, Extractor extractor)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"The directory given '{directory}' was not found");

            Dictionary<uint, int> counts = new Dictionary<uint, int>();
            foreach(Package package in extractor.master_packages_stream())
            {
                for (int i = 0; i < package.entry_table().Count; i++)
                {
                    Formats.Entry entry = package.entry_table()[i];
                    if (entry.type == 8 || entry.type == 16)
                    {
                        if (!counts.ContainsKey(entry.entry_a))
                            counts[entry.entry_a] = 0;
                        counts[entry.entry_a]++;
                    }
                }
            }

            var x = from entry in counts orderby entry.Value descending select entry;
            Dictionary<uint, string> found_in = new Dictionary<uint, string>();
            foreach (KeyValuePair<uint, int> hash_count_pair in x)
            {
                if (hash_count_pair.Value == 1)
                {
                    foreach (Package package in extractor.master_packages_stream())
                    {
                        for (int i = 0; i < package.entry_table().Count; i++)
                        {
                            Formats.Entry entry = package.entry_table()[i];
                            if (entry.entry_a == hash_count_pair.Key)
                                found_in[hash_count_pair.Key] = package.no_patch_id_name;
                        }
                    }
                }
            }

            using (FileStream file = new FileStream(Path.Combine(directory, $"{file_name}.csv"), FileMode.Create))
            {
                using (StreamWriter streamWriter = new StreamWriter(file))
                {
                    streamWriter.Write("hash,count,found_in\n");
                    foreach (KeyValuePair<uint, int> hash_count_pair in x)
                    {
                        streamWriter.Write($"0x{hash_count_pair.Key.ToString("X8")},{hash_count_pair.Value},{(hash_count_pair.Value == 1 ? found_in[hash_count_pair.Key] : "")}\n");
                    }
                }
            }
        }
    }
}
