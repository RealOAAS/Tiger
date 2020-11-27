using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Tiger
{
    /// <summary>
    /// The main extractor class used to extract the packages. 
    /// </summary>
    public class Extractor
    {
        private string packages_path;
        public string PackagesPath
        {
            get { return packages_path; }
            set
            {
                packages_path = value;
                packages_lookup_table = generate_packages_dictionary();
                packages_id_lookup_table = generate_packages_id_lookup_table();
            }
        }

        public Dictionary<string, List<Package>> packages_lookup_table = new Dictionary<string, List<Package>>();
        public Dictionary<uint, List<Package>> packages_id_lookup_table = new Dictionary<uint, List<Package>>();
        public bool verbouse { get; set; }

        private Dictionary<uint, string> string_lookup_table_holder;
        private List<Dictionary<uint, string>> investment_globals_strings_holder;
        private Dictionary<uint, string> client_startup_strings_holder;

        /// <summary>
        /// The main constructor to the extractor class
        /// </summary>
        /// <param name="packages_path">The path to the packages</param>
        /// <param name="verbouse">Allows for the extractor to print to the screen</param>
        public Extractor(string packages_path, LoggerLevels logging_level)
        {
            Logger.logging_level = logging_level;
            this.PackagesPath = packages_path;

            //Check if the depenedencies are present, and if they're not all present, then extract them
            List<string> dependencies = new List<string>() {"oo2core_8_win64.dll", "RawtexCmd.exe", "texconv.exe", "packed_codebooks.bin", "packed_codebooks_aoTuV_603.bin", "librevorb.dll", "AudioConverter.zip" };
            foreach (string dependency in dependencies)
            {
                string filepath = Path.Join(Directory.GetCurrentDirectory(), dependency);
                Logger.log($"Dependency '{dependency}' is found? {File.Exists(filepath)}", LoggerLevels.HighVerbouse);

                if (!File.Exists( filepath ))
                {
                    //Extract the dependency if it isnt found.
                    Logger.log($"Extracting {dependency}", LoggerLevels.HighVerbouse);

                    Stream file_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Tiger.Resources.{dependency}");
                    File.WriteAllBytes(filepath, new BinaryReader(file_stream).ReadBytes((int)file_stream.Length));

                    if (dependency.Split(".")[^1] == "zip")
                    {
                        Logger.log($"Extracting data in: {dependency}", LoggerLevels.MediumVerbouse);
                        ZipFile.ExtractToDirectory(dependency, ".");
                    }
                }
            }
        }

        /// <summary>
        /// A destructor to the extractor class. Used when the object is being destroyed
        /// </summary>
        ~Extractor()
        {
            Logger.log("Extractor is being destroyed. Flushing the logger buffer", LoggerLevels.HighVerbouse);
            Logger.flush();
        }

        /// <summary>
        /// A method used to obtain the names of all of the master packages. 
        /// </summary>
        /// <remarks>A master package is one with the highest patch id amongst all of the other packages sharing the same package id</remarks>
        /// <returns>A list of strings of the master package names</returns>
        public List<string> master_packages_names()
        {
            List<string> mpkg_names = new List<string>();

            foreach (KeyValuePair<string, List<Package>> dictionary_entry in packages_lookup_table)
                mpkg_names.Add(dictionary_entry.Value[^1].name);

            return mpkg_names;
        }

        /// <summary>
        /// A method used to return the master packages as an IEnumerable to allow iteration over packages
        /// </summary>
        /// <param name="specific_package">A part of a string to find in packages. Example, if this 
        /// arguemnt is set to 'audio' then only packages with the name audio will be returned
        /// </param>
        /// <returns>A generator or an IEnumerable of Package objects</returns>
        public IEnumerable<Package> master_packages_stream(string specific_package=null)
        {
            foreach (KeyValuePair<uint, List<Package>> id_package_pair in packages_id_lookup_table)
                if(specific_package == null || id_package_pair.Value[^1].name.Contains(specific_package))
                    yield return id_package_pair.Value[^1];
        }

        /// <summary>
        /// A method used to generate the packages dictionary with is a dictionary that contains all of the packages
        /// Initialized and ready to be used. This is done so that a package does not need to be initialized multiple
        /// times when being used
        /// </summary>
        /// <returns>A dictionary with the key as the package name (patch id removed) and a value of a list of all packages with that name</returns>
        /// <remarks>
        /// The dictionary returned has the following format. 
        /// {
        ///     "w64_ui_0932": [Package("w64_ui_0932_0.pkg"), Package("w64_ui_0932_1.pkg")],
        ///     "w64_audio_0324": [Package("w64_audio_0324_0.pkg"), Package("w64_audio_0324_1.pkg"), Package("w64_audio_0324_2.pkg")],
        /// }
        /// Thus, the master package is the package always at the end
        /// </remarks>
        private Dictionary<string, List<Package>> generate_packages_dictionary()
        {
            Logger.log("Obtaining the names of the master packages names dictionary", LoggerLevels.HighVerbouse);

            string[] package_names = Directory.GetFiles(this.PackagesPath, "*.pkg").ToList().Select(package_name => Tiger.Utils.get_package_name_from_path(package_name)).ToArray();
            Logger.log($"{package_names.Count()} packages found in the packages path", LoggerLevels.HighVerbouse);

            Dictionary<string, List<Package>> package_lookup_temp = new Dictionary<string, List<Package>>();

            //Adding all of the packages to the dictionary 
            foreach (string package_name in package_names)
            {
                string package_name_no_patch_id = Tiger.Utils.remove_patch_id_from_name(package_name);
                if (!package_lookup_temp.ContainsKey(package_name_no_patch_id))
                    package_lookup_temp[package_name_no_patch_id] = new List<Package>();

                Package package = new Package(packages_path, package_name);

                package_lookup_temp[package_name_no_patch_id].Add(new Package(packages_path, package_name));
            }

            //Sorting the packages inside the dictionary in order of the patch_id, so that its [0, 1, 2, .....] so the 
            //packages are ordered in ascending order.
            foreach(KeyValuePair<string, List<Package>> dictionary_entry in package_lookup_temp)
                dictionary_entry.Value.Sort((x, y) => x.patch_id.CompareTo(y.patch_id));


            return package_lookup_temp;
        }

        /// <summary>
        /// A method used to generate the packages dictionary with is a dictionary that contains all of the packages
        /// Initialized and ready to be used. This is done so that a package does not need to be initialized multiple
        /// times when being used
        /// </summary>
        /// <returns>A dictionary with the key as the package id and a value of a list of all packages with that name</returns>
        /// <remarks>
        /// The dictionary returned has the following format. 
        /// {
        ///     0x0932: [Package("w64_ui_0932_0.pkg"), Package("w64_ui_0932_1.pkg")],
        ///     0x0324: [Package("w64_audio_0324_0.pkg"), Package("w64_audio_0324_1.pkg"), Package("w64_audio_0324_2.pkg")],
        /// }
        /// Thus, the master package is the package always at the end
        /// </remarks>
        private Dictionary<uint, List<Package>> generate_packages_id_lookup_table()
        {
            Dictionary<uint, List<Package>> temp_lookup = new Dictionary<uint, List<Package>>();

            foreach (KeyValuePair<string, List<Package>> dictionary_entry in packages_lookup_table)
                temp_lookup[dictionary_entry.Value[0].package_id] = dictionary_entry.Value;

            return temp_lookup;
        }

        #region package
        /// <summary>
        /// A factory method used to initialize a Tiger.Package using the package name
        /// </summary>
        /// <param name="package_name">The name of the package. Example: w64_ui_09be_3.pkg</param>
        /// <returns>A Tiger.Package object</returns>
        public Tiger.Package package(string package_name)
        {
            return packages_lookup_table[ Tiger.Utils.remove_patch_id_from_name(package_name) ][^1];
        }

        /// <summary>
        /// A factory method used to initialize a Tiger.Package using the package_id
        /// </summary>
        /// <param name="package_id">The package id to the package. Example 0x9be</param>
        /// <remarks> Using this method will initialize the Tiger.Package with the master package. To initialize with a non master package, use the other function overloads </remarks>
        /// <returns>A Tiger.Package object</returns>
        public Tiger.Package package(uint package_id)
        {
            return packages_id_lookup_table[package_id][^1];
        }

        /// <summary>
        /// A factory method used to initialize a Tiger.Package using the package_id and patch_id
        /// </summary>
        /// <param name="package_id">The package id to the package. Example 0x9be</param>
        /// <param name="patch_id">The patch id to the package. Example 3</param>
        /// <returns>A Tiger.Package object</returns>
        public Tiger.Package package(uint package_id, uint patch_id)
        {
            return packages_id_lookup_table[package_id][(int)patch_id];
        }
        #endregion

        #region extract_entry_data
        /// <summary>
        /// A method used to extract the data of a single entry and then return it. 
        /// </summary>
        /// <returns>A ParsedFile object of the data in the parsed file and its extension and metadata</returns>
        /// <param name="package">A Package object of the package containing the entry</param>
        /// <param name="entry_index">The index of the entry to extract</param>
        public Tiger.Parsers.ParsedFile extract_entry_data(Package package, int entry_index)
        {
            Tiger.Formats.Entry entry = package.entry_table()[entry_index];

            uint current_block_index = entry.starting_block;
            uint last_block_index = current_block_index + entry.block_count();
            uint loaded_block_index = 0xFFFFFFFF;

            List<byte> extracted_data = new List<byte>();
            while (current_block_index < last_block_index)
            {
                if (current_block_index != loaded_block_index)
                {
                    Tiger.Formats.Block block = package.block_table()[(int)current_block_index];
                    Tiger.Package referenced_package = this.package(package.package_id, block.patch_id);

                    byte[] block_data = new byte[block.size];
                    using (FileStream File = new FileStream(Path.Combine(referenced_package.path), FileMode.Open, FileAccess.Read))
                    {
                        File.Seek(block.offset, 0);
                        using (BinaryReader BinReader = new BinaryReader(File))
                        {
                            block_data = BinReader.ReadBytes((int)block.size);
                        }
                    }

                    byte[] DecryptedBlock = (block.isEncrypted()) ? Tiger.Utils.decrypt(block_data, package.header().package_id, block) : block_data;
                    byte[] DecompressedBlock = (block.isCompressed()) ? Tiger.Utils.decompress(DecryptedBlock) : DecryptedBlock;
                    loaded_block_index = current_block_index;

                    int block_offset = (current_block_index == entry.starting_block) ? (int)entry.starting_block_offset : 0;
                    int data_available = ((int)(DecompressedBlock.Length - block_offset) < (int)(entry.file_size - extracted_data.Count())) ? ((int)DecompressedBlock.Length - block_offset) : ((int)entry.file_size - (int)extracted_data.Count());
                    extracted_data.AddRange(DecompressedBlock.Skip(block_offset).Take(data_available));
                    current_block_index++;
                }
            }
            return new Parsers.ParsedFile("bin", extracted_data.ToArray(), package.package_id, (uint)entry_index);
        }

        /// <summary>
        /// A method used to extract the data of a single entry and then return it. 
        /// </summary>
        /// <returns>A ParsedFile object of the data in the parsed file and its extension and metadata</returns>
        /// <param name="package">A Package object of the package containing the entry</param>
        /// <param name="entry">An Entry object of the entry being extracted</param>
        public Tiger.Parsers.ParsedFile extract_entry_data(Package package, Tiger.Formats.Entry entry)
        {
            return extract_entry_data(package, package.entry_table().IndexOf(entry));
        }

        /// <summary>
        /// A method used to extract the data of a single entry and then return it. 
        /// </summary>
        /// <returns>A ParsedFile object of the data in the parsed file and its extension and metadata</returns>
        /// <param name="package_id">A Package ID of the package containing the entry</param>
        /// <param name="entry_index">The index of the entry to extract</param>
        public Tiger.Parsers.ParsedFile extract_entry_data(uint package_id, int entry_index)
        {
            Package package = this.package(package_id);
            return extract_entry_data(package, entry_index);
        }

        /// <summary>
        /// A method used to extract the data of a single entry and then return it. 
        /// </summary>
        /// <returns>A ParsedFile object of the data in the parsed file and its extension and metadata</returns>
        /// <param name="reference">A Tiger.Utils.EntryReference object that makes a reference to another entry</param>
        public Tiger.Parsers.ParsedFile extract_entry_data(Tiger.Utils.EntryReference reference)
        {
            return extract_entry_data(this.package(reference.package_id), (int)reference.entry_index);
        }
        #endregion

        #region extract_binary_package_to_folder
        /// <summary>
        /// A method used to extract all of the entries inside of a package to the extraction path
        /// </summary>
        /// <remarks>
        /// The term 'binary' in the function name means that this function writes .bin files to the extraction_path
        /// which are decrypted and decompressed blocks without any processing done to them
        /// </remarks>
        /// <param name="extraction_path">The path to extract the entries to</param>
        /// <param name="package">A package object of the package to extract</param>
        public void extract_binary_package_to_folder(string extraction_path, Tiger.Package package)
        {
            if (!Directory.Exists(extraction_path))
            {
                Logger.log($"The directiory '{extraction_path}' is not found", LoggerLevels.HighVerbouse);
                throw new DirectoryNotFoundException($"The directiory '{extraction_path}' is not found");
            }

            Directory.CreateDirectory(Path.Join(extraction_path, package.no_patch_id_name));

            Logger.log($"Extracting package: {package.name}", LoggerLevels.HighVerbouse);
            for(int entry_index = 0; entry_index< package.entry_table().Count(); entry_index++ )
            {
                extract_entry_data(package, entry_index).WriteToFile(Path.Combine(extraction_path, package.no_patch_id_name));
            }
        }

        /// <summary>
        /// A function override for extract_binary_package_to_folder that uses the package name and not a package object
        /// </summary>
        /// <param name="extraction_path">The path to extract the entries to</param>
        /// <param name="package_name">The name of the package to extract</param>
        public void extract_binary_package_to_folder(string extraction_path, string package_name)
        {
            extract_binary_package_to_folder(extraction_path, this.package(package_name));
        }

        /// <summary>
        /// A function override for extract_binary_package_to_folder that uses the package id and not a package object
        /// </summary>
        /// <param name="extraction_path">The path to extract the entries to</param>
        /// <param name="package_id">The id of the package to extract</param>
        public void extract_binary_package_to_folder(string extraction_path, uint package_id)
        {
            extract_binary_package_to_folder(extraction_path, this.package(package_id));
        }
        #endregion

        /// <summary>
        /// A method used to get the string lookup table. This method takes care of caching so that the
        /// table does not have to be generated multiple times and only needs a single initialization
        /// </summary>
        /// <returns>A dictionary of the string hashes and the strings</returns>
        public Dictionary<uint, string> string_lookup_table()
        {
            if (string_lookup_table_holder != null)
                return string_lookup_table_holder;

            Logger.log("Stirng lookup table initialization beginning", LoggerLevels.HighVerbouse);

            Dictionary<uint, string> strings_dict = new Dictionary<uint, string>();
            foreach(KeyValuePair<string, List<Package>> package_name_package_pair in packages_lookup_table)
            {
                Package master_package = package_name_package_pair.Value[^1];
                for(int i = 0; i<master_package.entry_table().Count; i++)
                {
                    if (master_package.entry_table()[i].entry_a != (uint)Blocks.Type.StringReference)
                        continue;

                    byte[] dictionary_blob = new Parsers.StringReferenceParser(master_package, i, this).Parse().data;
                    Dictionary<uint, string> package_strings_dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<uint, string>>(System.Text.Encoding.UTF8.GetString(dictionary_blob));
                    foreach (KeyValuePair<uint, string> hash_string_pair in package_strings_dict)
                        strings_dict[hash_string_pair.Key] = hash_string_pair.Value;
                }
            }

            Logger.log("String lookup table initialization completed", LoggerLevels.HighVerbouse);
            string_lookup_table_holder = strings_dict;
            return strings_dict;
        }

        /// <summary>
        /// A method used to get the strings in the client startup packages and then return 
        /// it if they're already initialized
        /// </summary>
        /// <returns>A dictionary of hash and string corresponding to this hash</returns>
        public Dictionary<uint, string> client_statup_strings()
        {
            if (client_startup_strings_holder != null)
                return client_startup_strings_holder;

            Dictionary<uint, string> temp_holder = new Dictionary<uint, string>();
            foreach(Package package in master_packages_stream("client_startup"))
            {
                for(int i=0; i<package.entry_table().Count;i++)
                {
                    if (package.entry_table()[i].entry_a != (uint)Blocks.Type.StringReference)
                        continue;

                    byte[] dictionary_blob = new Parsers.StringReferenceParser(package, i, this).Parse().data;
                    Dictionary<uint, string> package_strings_dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<uint, string>>(System.Text.Encoding.UTF8.GetString(dictionary_blob));
                    foreach (KeyValuePair<uint, string> hash_string_pair in package_strings_dict)
                        temp_holder[hash_string_pair.Key] = hash_string_pair.Value;
                }
            }

            client_startup_strings_holder = temp_holder;
            return temp_holder;
        }

        /// <summary>
        /// A method used to get the indexed investment globals strings
        /// </summary>
        /// <returns> A list which indexes investment global strings  </returns>
        public List<Dictionary<uint, string>> investment_globals_strings()
        {
            if (investment_globals_strings_holder != null)
                return investment_globals_strings_holder;

            List<Utils.EntryReference> index_of_indexer = Utils.find_blocks((uint)Blocks.Type.StringReferenceIndexer, this, "investment");
            System.Diagnostics.Debug.Assert(index_of_indexer.Count == 1);

            Dictionary<uint, Dictionary<uint, string>> parsed_indexer = new Tiger.Parsers.StringReferenceIndexerParser(index_of_indexer[0], this).ParseDeserialize();

            investment_globals_strings_holder = parsed_indexer.Select(p => p.Value).ToList();
            return investment_globals_strings_holder;
        }
    }
}
