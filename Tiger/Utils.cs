using System;
using System.IO;
using Security.Cryptography;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Tiger
{
    public static class Utils
    {
        //Some of the variables needed in the 'decrypt' method are defined up here as static variabled so that they dont have to be redefined again everytime the method is called
        private static byte[] key_0 = new byte[16] { 0xD6, 0x2A, 0xB2, 0xC1, 0x0C, 0xC0, 0x1B, 0xC5, 0x35, 0xDB, 0x7B, 0x86, 0x55, 0xC7, 0xDC, 0x3B };
        private static byte[] key_1 = new byte[16] { 0x3A, 0x4A, 0x5D, 0x36, 0x73, 0xA6, 0x60, 0x58, 0x7E, 0x63, 0xE6, 0x76, 0xE4, 0x08, 0x92, 0xB5 };
        private static byte[] initial_nonce = new byte[12] { 0x84, 0xDF, 0x11, 0xC0, 0xAC, 0xAB, 0xFA, 0x20, 0x33, 0x11, 0x26, 0x99 };

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

        /// <summary>
        /// The method responsible for doing the decryption on blocks found to be encrypted. 
        /// </summary>
        /// <returns>
        /// A byte array (byte[]) containing the decrypted data.
        /// </returns>
        /// <param name="block_data">The data which we wish to decrypt. Given as a byte array</param>
        /// <param name="package_id">The PackageID for the package being decrypted. Given as an UInt16</param>
        /// <param name="block">The Block on which the decryption is happening. Given as an object of the type Block</param>
        public static byte[] decrypt(byte[] block_data, UInt16 package_id, Tiger.Formats.Block block)
        {
            byte[] decrypted_data = new byte[block.Size];

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
                        cryptoStream.Write(block_data, 0, (int)block.Size);
                        cryptoStream.FlushFinalBlock();
                        decrypted_data = decryption_result.ToArray();
                    }
                }
            }
            return decrypted_data;
        }
    }
}
