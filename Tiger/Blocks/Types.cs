using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.Blocks
{
    enum Type : uint
    {
        //A string bank is a file that contains the strings in an encrypted format. The
        //File does not make any other references or anything. It just contains the 
        //encrypted and tangled strings.
        StringBank = 0x808099F1,

        //A string reference file is a file that contains string references or reference
        //hashes as well as string hashes. It's uses to identify string banks in a specific
        //langiage and it also provides the strings with their hashes.
        StringReference = 0x808099EF,

        //A file that makes a reference to an OTF font which provides a name for it and
        //some metadata on the font
        FontReference = 0x80803C12,

        // A class made to parse AudioBanks. AudioBanks as of Destiny 2; Beyond Light
        // Are blocks (type 8 entries) of the type 0x808097b8. These files contain a
        // number of hashes that make references to audios and their string subtitles,
        // as well as their narrators.
        AudioBank = 0x808097B8
    }
}
