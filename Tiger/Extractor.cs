using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Versioning;
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

        /// <summary>
        /// The main constructor to the extractor class
        /// </summary>
        /// <param name="packages_path">The path to the packages</param>
        /// <param name="verbouse">Allows for the extractor to print to the screen</param>
        public Extractor(string packages_path, bool verbouse)
        {
            Logger.logging_level = LoggerLevels.MediumVerbouse;
            this.PackagesPath = packages_path;

            master_packages_names();

            //Check if the depenedencies are present, and if they're not all present, then extract them
            List<string> dependencies = new List<string>() {"oo2core_8_win64.dll", "RawtexCmd.exe", "texconv.exe" };
            foreach(string dependency in dependencies)
            {
                string filepath = Path.Join(Directory.GetCurrentDirectory(), dependency);
                Logger.log($"Dependency '{dependency}' is found? {File.Exists(filepath)}", LoggerLevels.HighVerbouse);

                if (!File.Exists( filepath ))
                {
                    //Extract the dependency if it isnt found.
                    Logger.log($"Extracting {dependency}", LoggerLevels.HighVerbouse);

                    Stream file_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Tiger.Resources.{dependency}");
                    File.WriteAllBytes(filepath, new BinaryReader(file_stream).ReadBytes((int)file_stream.Length));
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

            Directory.CreateDirectory(Path.Join(extraction_path, package.name));

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


    }
}
