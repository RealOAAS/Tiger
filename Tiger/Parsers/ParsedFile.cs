using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.Parsers
{
    /// <summary>
    /// A class used to signify a parsed file. A parsed file contains expressions such as the extension and the file data used to
    /// identify a parsed file
    /// </summary>
    public class ParsedFile
    {
        public string extension { get; private set; }
        public byte[] data { get; private set; }

        private uint source_package_id;
        private uint source_entry_index;

        private static char[] trim_characters = new char[] { '.' };

        /// <summary>
        /// The main constructor the the PasedFile class. 
        /// </summary>
        /// <param name="extension">The extension to be used for the file. Example: "bin"</param>
        /// <param name="data">The data obtained from the parsed file</param>
        /// <param name="source_package_id">The ID of the package that the entry originates from</param>
        /// <param name="source_entry_index">The index of the origin entry in the entry table</param>
        public ParsedFile(string extension, byte[] data, uint source_package_index, uint source_entry_index)
        {
            this.extension = extension.Trim(trim_characters);
            this.data = data;
            this.source_entry_index = source_entry_index;
            this.source_package_id = source_package_index;
        }

        /// <summary>
        /// A method used to write all of the contents of the ParsedFile to a file 
        /// </summary>
        /// <param name="path">The path of the directory to extract the file to</param>
        public void WriteToFile(string path)
        {
            System.IO.File.WriteAllBytes( System.IO.Path.Combine( path, Tiger.Utils.entry_name(source_package_id, source_entry_index) ) + extension, data );
        }
    }
}
