using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiger.Entries
{
    /// <summary>
    /// A static class of the subtypes of the entries found in the entry table
    /// </summary>
    public static class Subtypes
    {
        public enum ThirdParty
        {
            BKHD = 6,
            RIFF = 7
        }

        public enum Video
        {
            Unknown = 0,
            USM = 1
        }

        public enum Texture
        {
            DDS = 1,
        }
    }
}
