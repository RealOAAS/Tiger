using System;
using System.IO;
using System.Linq;
using System.Text;
using Security.Cryptography;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Tiger
{
    public static class Utils
    {
        //Some of the variables needed in the 'decrypt' method are defined up here as static variabled so that they dont have to be redefined again everytime the method is called
        private static byte[] key_0 = new byte[16] { 0xD6, 0x2A, 0xB2, 0xC1, 0x0C, 0xC0, 0x1B, 0xC5, 0x35, 0xDB, 0x7B, 0x86, 0x55, 0xC7, 0xDC, 0x3B };
        private static byte[] key_1 = new byte[16] { 0x3A, 0x4A, 0x5D, 0x36, 0x73, 0xA6, 0x60, 0x58, 0x7E, 0x63, 0xE6, 0x76, 0xE4, 0x08, 0x92, 0xB5 };
        private static byte[] initial_nonce = new byte[12] { 0x84, 0xCC, 0x11, 0xC0, 0xAC, 0xAB, 0xFA, 0x20, 0x33, 0x11, 0x26, 0x99 };

        /// <summary>
        /// A method used to remove the path and only leave the package name
        /// </summary>
        /// <param name="package_path">The path to the package</param>
        /// <returns>A string of the package name without the path</returns>
        public static string get_package_name_from_path(string package_path)
        {
            return package_path.Split('\\')[^1];
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

        /// <summary>
        /// A helper method used to give entires their name to ease their identification. As an example, if an entry is in the package
        /// with id 0934 and the entry is at the index of 012 (hex) then its name would be 0934_00000012. Providing easy naming scheme
        /// </summary>
        /// <param name="package">The package containing the entry</param>
        /// <param name="entry">The entry to find the name for</param>
        /// <returns>The name of the entry with the format packageid_entryIndex</returns>
        public static string entry_name(Tiger.Package package, Tiger.Formats.Entry entry)
        {
            return $"{package.package_id.ToString("X4")}_{package.entry_table().IndexOf(entry).ToString("X8")}".ToUpper();
        }

        /// <summary>
        /// A helper method used to give entires their name to ease their identification. As an example, if an entry is in the package
        /// with id 0934 and the entry is at the index of 012 (hex) then its name would be 0934_00000012. Providing easy naming scheme
        /// </summary>
        /// <param name="package_id">The package_id containing the entry</param>
        /// <param name="entry_index">The index of the entry to find the name for</param>
        /// <returns>The name of the entry with the format packageid_entryIndex</returns>
        public static string entry_name(uint package_id, uint entry_index)
        {
            return $"{package_id.ToString("X4")}_{entry_index.ToString("X8")}";
        }

        /// <summary>
        /// The method responsible for doing the decryption on blocks found to be encrypted. 
        /// </summary>
        /// <returns>A byte array (byte[]) containing the decrypted data.</returns>
        /// <param name="block_data">The data which we wish to decrypt. Given as a byte array</param>
        /// <param name="package_id">The PackageID for the package being decrypted. Given as an UInt16</param>
        /// <param name="block">The Block on which the decryption is happening. Given as an object of the type Block</param>
        public static byte[] decrypt(byte[] block_data, UInt16 package_id, Tiger.Formats.Block block)
        {
            byte[] decrypted_data = new byte[block.size];

            byte[] package_nonce = new byte[12];
            Array.Copy(initial_nonce, package_nonce, initial_nonce.Length);
            package_nonce[0] ^= (byte)(package_id >> 8 & 255);
            package_nonce[1] ^= (byte)38;
            package_nonce[11] ^= (byte)(package_id & 255);

            using (AuthenticatedAesCng EncryptionProvider = new AuthenticatedAesCng())
            {
                EncryptionProvider.CngMode = CngChainingMode.Gcm;
                EncryptionProvider.Key = (block.isAlternateKey()) ? key_1 : key_0;
                EncryptionProvider.IV = package_nonce;
                EncryptionProvider.Tag = block.GCMTag;

                using (MemoryStream decryption_result = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(decryption_result, EncryptionProvider.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(block_data, 0, (int)block.size);
                        cryptoStream.FlushFinalBlock();
                        decrypted_data = decryption_result.ToArray();
                    }
                }
            }
            return decrypted_data;
        }
   
        /// <summary>
        /// A wrapper to the oodle dll. 
        /// </summary>
        [DllImport(@"oo2core_8_win64.dll")]
        private static extern int OodleLZ_Decompress(byte[] compressed_bytes, int size_of_compressed_bytes, byte[] decompressed_bytes, int size_of_decompressed_bytes, uint a, uint b, ulong c, uint d, uint e, uint f, uint g, uint h, uint i, uint threadModule);

        /// <summary>
        /// A method responsible for decompressing the entires that require decompression. 
        /// </summary>
        /// <returns>A byte array (byte[]) containing the decompressed data.</returns>
        /// <param name="block_data">The data which we wish to decompress. Given as a byte array</param>
        public static byte[] decompress(byte[] block_data)
        {
            byte[] decompressed_data = new byte[0x40000];
            OodleLZ_Decompress(block_data, block_data.Length, decompressed_data, 0x40000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            return decompressed_data;
        }

        /// <summary>
        /// An entry reference object containing a package_id and an entry index
        /// </summary>
        public class EntryReference
        {
            public uint package_id { get; private set; }
            public uint entry_index { get; private set; }
            public uint entry_a { get; private set; }

            /// <summary>
            /// A constructor that is used to initialize a new EntryReference. It takes an entry_a and initializes a new
            /// object of the package_id and the entry_index being referenced
            /// </summary>
            /// <param name="entry_a">A uint of the entry_a data. Entry_a is uint32_t hash which makes references to entries</param>
            public EntryReference(uint entry_a)
            {
                this.entry_a = entry_a;

                entry_index = entry_a & 0x1FFF;
                package_id = (entry_a >> 13) & 0x3FF;
                uint reference_unknown_id = (entry_a >> 23) & 0x3FF;

                uint flags = reference_unknown_id & 0x3;
                package_id = (flags == 1) ? package_id : package_id | (uint)((int)0x100 << (int)flags);
            }

            /// <summary>
            /// A method used to get the string representation of where this entry is at in the entry table.
            /// Relies on the method Tiger.Utils.entry_name.
            /// </summary>
            /// <returns> A string representation of where the entry is at. </returns>
            public override string ToString()
            {
                return Tiger.Utils.entry_name(package_id, entry_index);
            }
        }

        /// <summary>
        /// A method used to convert a dictionary of index and strings to a list. 
        /// </summary>
        /// <remarks>
        /// This method is used typically with the Tiger.Parser.StringBankParser 
        /// class to convert the string dictionary.
        /// </remarks>
        /// <param name="dictionary_data">The dictionary data as a byte array</param>
        /// <returns>An array of strings foudn in the dictionary</returns>
        public static string[] dictionary_bytes_to_list(byte[] dictionary_data)
        {
            string dictionary_string = Encoding.UTF8.GetString(dictionary_data);
            Dictionary<int, string> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, string>>(dictionary_string);
            return dictionary.Select( p => p.Value).ToArray();
        }

        /// <summary>
        /// A class that implements methods from the BoyerMoore algorithim to search for a needle in a haystack
        /// </summary>
        public class BoyerMoore
        {
            /// <summary>
            /// A method used to find the index of a needle in haystack
            /// </summary>
            /// <param name="haystack">The large byte collection to search for bytes in</param>
            /// <param name="needle">The set of bytes to search for</param>
            /// <returns>The index of the needle in the haystack</returns>
            public static int IndexOf(byte[] haystack, byte[] needle)
            {
                if (needle.Length == 0)
                {
                    return 0;
                }

                int[] charTable = MakeCharTable(needle);
                int[] offsetTable = MakeOffsetTable(needle);
                for (int i = needle.Length - 1; i < haystack.Length;)
                {
                    int j;
                    for (j = needle.Length - 1; needle[j] == haystack[i]; --i, --j)
                    {
                        if (j == 0)
                        {
                            return i;
                        }
                    }

                    i += Math.Max(offsetTable[needle.Length - 1 - j], charTable[haystack[i]]);
                }

                return -1;
            }

            private static int[] MakeCharTable(byte[] needle)
            {
                const int ALPHABET_SIZE = 256;
                int[] table = new int[ALPHABET_SIZE];
                for (int i = 0; i < table.Length; ++i)
                {
                    table[i] = needle.Length;
                }

                for (int i = 0; i < needle.Length - 1; ++i)
                {
                    table[needle[i]] = needle.Length - 1 - i;
                }

                return table;
            }

            private static int[] MakeOffsetTable(byte[] needle)
            {
                int[] table = new int[needle.Length];
                int lastPrefixPosition = needle.Length;
                for (int i = needle.Length - 1; i >= 0; --i)
                {
                    if (IsPrefix(needle, i + 1))
                    {
                        lastPrefixPosition = i + 1;
                    }

                    table[needle.Length - 1 - i] = lastPrefixPosition - i + needle.Length - 1;
                }

                for (int i = 0; i < needle.Length - 1; ++i)
                {
                    int slen = SuffixLength(needle, i);
                    table[slen] = needle.Length - 1 - i + slen;
                }

                return table;
            }

            private static bool IsPrefix(byte[] needle, int p)
            {
                for (int i = p, j = 0; i < needle.Length; ++i, ++j)
                {
                    if (needle[i] != needle[j])
                    {
                        return false;
                    }
                }

                return true;
            }

            private static int SuffixLength(byte[] needle, int p)
            {
                int len = 0;
                for (int i = p, j = needle.Length - 1; i >= 0 && needle[i] == needle[j]; --i, --j)
                {
                    len += 1;
                }

                return len;
            }
        }
    
        /// <summary>
        /// A method used to take in a package id and an entry index and then return a hash that points
        /// or specifies this entry in the form of an EntryReference object. The hash is the store in
        /// the EntryReference.entry_a
        /// </summary>
        /// <param name="package_id">The package ID of the package storing the entry</param>
        /// <param name="entry_index">The index that the current entry is at</param>
        /// <returns> An EntryReference object of the details on the hash of this entry </returns>
        /// <remarks>
        /// Due to how Bungie generates these hashes, this method might have some errors sometimes. The main source
        /// of error that could be in this function has to do with the 'reference_unknown_id'. So far, only the last
        /// two bits of this variable is understood (which is whether to or by 0x400 or 0x800). The other ones are not
        /// undersood and this method assumes them to be equal to 0x100. When they're not equal to 0x100 the hash that 
        /// this function generates will no longer be accurate.
        /// </remarks>
        public static Tiger.Utils.EntryReference generate_reference_hash(uint package_id, uint entry_index)
        {
            //EntryA: CCCCCCCCCBBBBBBBBBBAAAAAAAAAAAAA

            // A:RefID: EntryA & 0x1FFF
            // B:RefPackageID: (EntryA >> 13) & 0x3FF
            // C:RefUnkID: (EntryA >> 23)

            uint last_two_flags;
            if (package_id >= 0x800)
            {
                last_two_flags = 3;
                package_id -= 0x800;
            }
            else if (package_id >= 0x400)
            {
                last_two_flags = 2;
                package_id -= 0x400;
            }
            else
                last_two_flags = 1;

            uint reference_unknown_id = 0x100 + last_two_flags;
            uint entry_a = (reference_unknown_id << 23) + (package_id << 13) + entry_index;

            return new EntryReference(entry_a);
        }

        /// <summary>
        /// A method used to find a specific block type in the entry table of packages. Useful when finding a list of all of the
        /// important files needed for the decryption and analysis. 
        /// </summary>
        /// <param name="search_block_type">The block type to search for. Example: 0x80809337</param>
        /// <param name="extractor">An extractor object used in this search</param>
        /// <param name="specific_packages">An optional argument. When this argument is set to a string value, then only packages with 'specific_packages' in their name will be searched</param>
        /// <returns></returns>
        public static List<EntryReference> find_blocks(uint search_block_type, Extractor extractor, string specific_packages = null)
        {
            List<EntryReference> found = new List<EntryReference>();

            foreach(Package package in extractor.master_packages_stream())
            {
                if (specific_packages != null && !package.no_patch_id_name.Contains(specific_packages))
                    continue;

                List<Formats.Entry> entry_table = package.entry_table();
                for(int i = 0; i<entry_table.Count; i++)
                    if (entry_table[i].entry_a == search_block_type)
                        found.Add(generate_reference_hash(package.package_id, (uint)i));
            }

            return found;        
        }
    }
}
