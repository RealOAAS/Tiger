using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tiger
{
    /// <summary>
    /// The main extractor class used to extract the packages
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
            this.verbouse = verbouse;
            this.PackagesPath = packages_path;
        }

        /// <summary>
        /// A method used to log a message on the screen depending on whether the extractor is verbouse
        /// </summary>
        /// <param name="message"></param>
        public void log(string message)
        {
            if (this.verbouse)
            {
                string TimeString = DateTime.Now.ToString("hh:mm:ss tt");
                Console.WriteLine($"[{TimeString}]: {message}");
            }
        }

        /// <summary>
        /// A method used to obtain the names of all of the master packages. 
        /// </summary>
        /// <remarks>A master package is one with the highest patch id amongst all of the other packages sharing the same package id</remarks>
        /// <returns>A list of strings of the master package names</returns>
        private List<string> get_master_packages_names()
        {
            this.log("Obtaining the names of the master packages");

            List<string> package_names = Directory.GetFiles(this.PackagesPath, "*.pkg").ToList().Select(package_name => Tiger.Utils.get_package_name_from_path(package_name)).ToList();
            this.log($"{package_names.Count()} packages found in the packages path");

            List<string> package_names_no_patch_id = package_names.Select(package_name => Tiger.Utils.remove_patch_id_from_name(package_name)).Distinct().ToList();
            this.log($"{package_names.Count()} packages resolved into {package_names_no_patch_id.Count()} unique packages");

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
            return m_pkg_names;
        }

        /// <summary>
        /// A method used to create a dictionary of the package ids and their respective master package names
        /// </summary>
        /// <returns>A dictionary of the package ids and the master package names</returns>
        private Dictionary<uint, string> get_master_packages_dict()
        {
            this.log("Creating the master packages dictionary");
            Dictionary<uint, string> m_pkg_dict = new Dictionary<uint, string>();

            List<string> m_pkg_names = this.MasterPackageNames == null ? get_master_packages_names() : this.MasterPackageNames; //checking if the MasterPackageNames is null
            foreach (string pkg_name in m_pkg_names)
            {
                uint pkg_id = pkg_name.Contains("_en_") ? Convert.ToUInt32(pkg_name.Split('_')[^3], 16) : Convert.ToUInt32(pkg_name.Split('_')[^2], 16);
                m_pkg_dict.Add(pkg_id, pkg_name);
            }
            return new Dictionary<uint, string>();
        }
    }
}
