#include <stdint.h>
#include <time.h>

enum package_language{
    None = 0,
    English = 1,
    French = 2,
    Italian = 3,
    German = 4,
    Spanish = 5,
    Japanese = 6,
    Portuguese = 7,
    Russian = 8,
    Polish = 9,
    Simplified_Chinese = 10,
    Traditional_Chinese = 11,
    Latin_American_Spanish = 12,
    Korean = 13,
};

struct Header{
    uint16_t version;               //0x0
    uint16_t platform;              //0x2
    uint32_t padding;               //0x4
    uint32_t field_8;               //0x8
    uint32_t field_c;               //0xc
    uint16_t package_id;            //0x10

    uint8_t isPackage;              //0x12
    uint8_t isStartupPackage;       //0x13
    uint32_t padding;               //0x14
    uint64_t field_18;              //0x18 0x74bad2d899aaf3c7 if the patch id is greater than 1 else 0x8fb7fc3de72dd3d5
    uint64_t build_date;            //0x20

    uint32_t build_id;              //0x28
    uint32_t padding;               //0x2c
    uint16_t patch_id;              //0x30

    uint16_t language;              //0x32
    uint32_t field_34;              //0x34
    uint32_t field_38;              //0x38
    uint32_t always_0x02;           //0x3c Always equal to 0x2
    uint32_t signature_offset;      //0x40

    uint32_t entry_table_offset;    //0x44
    uint32_t field_48;              //0x48
    uint8_t entry_table_hash[20];   //0x4c
    uint32_t entry_table_count;     //0x60

    uint32_t entry_table_offset;    //0x64
    uint32_t block_table_count;     //0x68
    uint32_t block_table_offset;    //0x6c
    uint32_t field_70;              //0x70

    uint32_t field_74;              //0x74
    uint8_t empty[164];             //0x78
    uint32_t deafbeef_offset;       //0x11c
    uint64_t package_size;          //0x120

    uint64_t field_128;             //0x128 0xfb438df4 for startup packages else 0x281141f
};

struct Entry{
    uint32_t    EntryA;
    uint32_t    EntryB;
    uint32_t    EntryC;
    uint32_t    EntryD;

    // [             EntryD              ] [             EntryC              ] 
    // GGGGGGFF FFFFFFFF FFFFFFFF FFFFFFFF FFFFEEEE EEEEEEEE EEDDDDDD DDDDDDDD

    // [             EntryB              ] [             EntryA              ]
    // 00000000 00000000 TTTTTTTS SS000000 CCCCCCCC CBBBBBBB BBBAAAAA AAAAAAAA

    // A:RefID: EntryA & 0x1FFF
    // B:RefPackageID: (EntryA >> 13) & 0x3FF
    // C:RefUnkID: (EntryA >> 23)
    // D:StartingBlock: EntryC & 0x3FFF
    // E:StartingBlockOffset: ((EntryC >> 14) & 0x3FFF) << 4
    // F:FileSize: (EntryD & 0x3FFFFFF) << 4 | (EntryC >> 28) & 0xF
    // G:Unknown: (EntryD >> 26) & 0x3F

    // Flags (Entry B)
    // S:SubType: (EntryB >> 6) & 0x7
    // T:Type:  (EntryB >> 9) & 0x7F
};

struct Block {
    uint32_t    Offset;
    uint32_t    Size;
    uint16_t    PatchID;
    uint16_t    Flags;
    uint8_t     Hash[0x14];
    uint8_t     GCMTag[0x10];

    // [     Flags     ]
    // 00000000 00000AEC

    // C:Compressed: Flags & 0x1
    // E:Encrypted: Flags & 0x2
    // A:AltKey: Flags & 0x4
};

//Entry Types and Subtypes
struct SPkgEntry_26 {
    // 3rd Party

    // SubType 6: BKHD
    // SubType 7: RIFF
};

struct SPkgEntry_27 {
    // USM Video

    // SubType 0: Unknown
    // SubType 1: USM Video
};

struct SPkgEntry_32 {
    // Texture Header

    //SubType 1: 64 byte long header
    //
};

struct SPkgEntry_32_1 {
    // Texture Header of SubType 1. 64 Byte long
    uint32_t texture_size;      //0x0
    uint32_t DXGI_FORMAT;       //0x4 found here https://docs.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format
    uint32_t field_08;          //0x8
    uint32_t field_0c;          //0xC

    uint32_t field_10;          //0x10 unknown
    uint32_t field_14;          //0x14 unknown
    uint32_t field_18;          //0x18
    uint32_t field_1c;          //0x1c

    uint16_t CAFE;              //0x20
    uint16_t width;             //0x22
    uint16_t height;            //0x24
    uint16_t field_26;          //0x26 unknown
    uint32_t field_28;          //0x28 unknown
    uint32_t field_2c;          //0x2c unknown

    uint32_t field_30;          //0x30 unknown
    uint32_t field_34;          //0x34 unknown
    uint32_t field_38;          //0x38 unknown
    uint32_t field_3c;          //0x3c unknown
};

//Block Types
struct SPkgBlock_808099F1 {
    // String bank file
};

struct SPkgBlock_808099EF {
    // Struct of the file containing string references and string hashes for different strings in different languages
};