using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiger.Entries
{
    enum Types
    {
        //Entries of this type have raw data
        RawData = 8,
        RawData1= 16,

        //Entries of this type are font files (OTF)
        FontFile = 24,

        //Thirdparty files which usually are audio or havok files
        ThirdParty = 26,

        //Entries of this type are usually videos
        Video = 27,

        //Entries of this type are texture headers and they make
        //references to texture data
        TextureHeader = 32,

        //Entries of this type are texture data
        TextureData = 40,
        TextureUIData = 48,
    }
}
