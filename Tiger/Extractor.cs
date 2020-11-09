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
                MasterPackageNames = get_master_packages_names();
                MasterPackageDict = get_master_packages_dict();
            }
        }

        public List<string> MasterPackageNames { get; private set; }
        public Dictionary<uint, string> MasterPackageDict { get; private set; }
        public bool verbouse { get; set; }

        /// <summary>
        /// The main constructor to the extractor class
        /// </summary>
        /// <param name="packages_path">The path to the packages</param>
        /// <param name="verbouse">Allows for the extractor to print to the screen</param>
        public Extractor(string packages_path, bool verbouse)
        {
            Logger.verbouse = verbouse;
            this.PackagesPath = packages_path;

            //Check if the depenedencies are present, and if they're not all present, then extract them
            List<string> dependencies = new List<string>() {"oo2core_3_win64.dll", "RawtexCmd.exe", "texconv.exe" };
            foreach(string dependency in dependencies)
            {
                string filepath = Path.Join(Directory.GetCurrentDirectory(), dependency);
                Logger.log($"Dependency '{dependency}' is found? {File.Exists(filepath)}");

                if (!File.Exists( filepath ))
                {
                    //Extract the dependency if it isnt found.
                    Logger.log($"Extracting {dependency}");

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
            Logger.log("Extractor is being destroyed. Flushing the logger buffer");
            Logger.flush();
        }

        /// <summary>
        /// A method used to obtain the names of all of the master packages. 
        /// </summary>
        /// <remarks>A master package is one with the highest patch id amongst all of the other packages sharing the same package id</remarks>
        /// <returns>A list of strings of the master package names</returns>
        private List<string> get_master_packages_names()
        {
            Logger.log("Obtaining the names of the master packages");

            List<string> package_names = Directory.GetFiles(this.PackagesPath, "*.pkg").ToList().Select(package_name => Tiger.Utils.get_package_name_from_path(package_name)).ToList();
            Logger.log($"{package_names.Count()} packages found in the packages path");

            List<string> package_names_no_patch_id = package_names.Select(package_name => Tiger.Utils.remove_patch_id_from_name(package_name)).Distinct().ToList();
            Logger.log($"{package_names.Count()} packages resolved into {package_names_no_patch_id.Count()} unique packages");

            List<string> m_pkg_names = new List<string>();
            Parallel.ForEach(package_names_no_patch_id, pkg_name =>
            {
                for (int i = 10; i > 0; i--)
                {
                    if (File.Exists($"{this.PackagesPath}/{pkg_name}_{i}.pkg"))
                    {
                        m_pkg_names.Add($"{pkg_name}_{i}.pkg");
                        break;
                    }
                }
            });
            return m_pkg_names.OrderBy(x=>x).ToList();
        }

        /// <summary>
        /// A method used to create a dictionary of the package ids and their respective master package names
        /// </summary>
        /// <returns>A dictionary of the package ids and the master package names</returns>
        public Dictionary<uint, string> get_master_packages_dict()
        {
            Logger.log("Creating the master packages dictionary");
            Dictionary<uint, string> m_pkg_dict = new Dictionary<uint, string>();

            List<string> m_pkg_names = this.MasterPackageNames == null ? get_master_packages_names() : this.MasterPackageNames; //checking if the MasterPackageNames is null
            foreach (string pkg_name in m_pkg_names)
            {
                uint pkg_id = pkg_name.Contains("_en_") ? Convert.ToUInt32(pkg_name.Split('_')[^3], 16) : Convert.ToUInt32(pkg_name.Split('_')[^2], 16);
                m_pkg_dict.Add(pkg_id, pkg_name);
            }
            return m_pkg_dict;
        }

        /// <summary>
        /// A factory method used to initialize a Tiger.Package using the package name
        /// </summary>
        /// <param name="package_name">The name of the package. Example: w64_ui_09be_3.pkg</param>
        /// <returns>A Tiger.Package object</returns>
        public Tiger.Package package(string package_name)
        {
            return new Tiger.Package(this.packages_path, package_name);
        }

        /// <summary>
        /// A factory method used to initialize a Tiger.Package using the package_id
        /// </summary>
        /// <param name="package_id">The package id to the package. Example 0x9be</param>
        /// <remarks> Using this method will initialize the Tiger.Package with the master package. To initialize with a non master package, use the other function overloads </remarks>
        /// <returns>A Tiger.Package object</returns>
        public Tiger.Package package(uint package_id)
        {
            return new Tiger.Package(this.packages_path, MasterPackageDict[package_id]);
        }

        /// <summary>
        /// A factory method used to initialize a Tiger.Package using the package_id and patch_id
        /// </summary>
        /// <param name="package_id">The package id to the package. Example 0x9be</param>
        /// <param name="patch_id">The patch id to the package. Example 3</param>
        /// <returns>A Tiger.Package object</returns>
        public Tiger.Package package(uint package_id, uint patch_id)
        {
            return new Tiger.Package(this.packages_path, $"{Tiger.Utils.remove_patch_id_from_name(MasterPackageDict[package_id])}_{patch_id}.pkg");
        }

        /// <summary>
        /// A method used to extract the data of a single entry and then return it. 
        /// </summary>
        /// <returns>Returns a byte array containing the extracted data</returns>
        /// <param name="package">A Package object of the package containing the entry</param>
        /// <param name="entry">An Entry object of the entry being extracted</param>
        public byte[] extract_entry_data(Package package, Tiger.Formats.Entry entry)
        {
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

                    int BlockOffset = (current_block_index == entry.starting_block) ? (int)entry.starting_block_offset : 0;
                    int DataAvailable = ((int)(DecompressedBlock.Length - BlockOffset) < (int)(entry.file_size - extracted_data.Count())) ? ((int)DecompressedBlock.Length - BlockOffset) : ((int)entry.file_size - (int)extracted_data.Count());
                    extracted_data.AddRange(DecompressedBlock.Skip(BlockOffset).Take(DataAvailable));
                    current_block_index++;
                }
            }
            return extracted_data.ToArray();
        }

        /// <summary>
        /// A function overload for the 'extract_entry_data' method used to allow for the package to be accepted by its package name and index of the entry
        /// </summary>
        /// <returns>Returns a byte array containing the extracted data</returns>
        /// <param name="package_name">The name of the package containing the entry</param>
        /// <param name="entry_index">The index of the entry that we wish to extract in the entry table</param>
        public byte[] extract_entry_data(string package_name, int entry_index)
        {
            Tiger.Package package = this.package(package_name);
            return extract_entry_data(package, package.entry_table()[entry_index]);
        }

        /// <summary>
        /// A function overload for the 'extract_entry_data' method used to allow for the package to be accepted by its package_id and index of the entry
        /// </summary>
        /// <returns>Returns a byte array containing the extracted data</returns>
        /// <param name="package_name">The package_id of the package containing the entry</param>
        /// <param name="entry_index">The index of the entry that we wish to extract in the entry table</param>
        public byte[] extract_entry_data(uint package_id, int entry_index)
        {
            Tiger.Package package = this.package(package_id);
            return extract_entry_data(package, package.entry_table()[entry_index]);
        }
    }
}
