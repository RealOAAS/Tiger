namespace Tiger
{
    public static class Utils
    {
        /// <summary>
        /// A method used to remove the path and only leave the package name
        /// </summary>
        /// <param name="package_path">The path to the package</param>
        /// <returns>A string of the package name without the path</returns>
        public static string get_package_name_from_path(string package_path)
        {
            return package_path.Split('/')[^1];
        }

        /// <summary>
        /// A method used to remove the patch id from the package name passed to it
        /// </summary>
        /// <param name="package_name">The name of the package</param>
        /// <returns>A string of the name of the package without the patch id</returns>
        public static string remove_patch_id_from_name(string package_name)
        {
            return string.Join('_', package_name.Split('_')[0..^1]);
        }
    }
}
