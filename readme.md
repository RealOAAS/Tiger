# Introduction
Tiger is a project entirely written in C# and its main purpose is to analyse the files of Destiny 2 and parse many of them. As the game progressed and grows, this implementation of extractor will grow and improve as well.

# Getting Started 
There are a lot of things that this extractor can do. Lets walk through a couple of them one by one and then hopefully by the end of these examples you will have a solid idea on how this library can be used.

## Example 1: Extracting binary entries to directory

If you want to get started with using Tiger and would like to perhaps try extract all of the entries present in a package without resolving them (parsing them into their known formats) then one of the things that you could do is use the methods in `Tiger.Extractor` to perform just that! The following C# code shows you how this can be done
```cs
using Tiger;
using System;
using System.IO;

namespace Program
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string packages_path = @"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages";
            string output_path = @"C:\DestinyExtractionPath";
            Extractor extractor = new Extractor(packages_path, LoggerLevels.HighVerbouse);

            foreach(Package package in extractor.master_packages_stream())
                extractor.extract_binary_package_to_folder(output_path, package);
        }
    }
}
```

Please do take note that in the above code sample, the `output_path` given must alreay exist. An exception will be thrown if it is not. After executing this code the entries will be found in the path given in folders following the name of the package. The file extension will be `.bin`. These files will not be parsed or processed in anyway. Just decompressed and/or decrypted and then written to the directory given.

To begin getting files that have already been analysed or processed into something that makes more sense, take a look at `Tiger.Parsers` which is able to parse a number of formats and then return a `Tiger.Parsers.ParsedFile` object to write them to file.

## Example 2: Extracting entries to files of known formats

You can extract specific entries (which we have analyzed and understood) to a known format by using the classes present in `Tiger.Parsers` which are capable of parsing a number of formats. As of now, they're capable of parsing files of the following formats:

    - 0x808099F1
    - 0x808099EF
    - 0x80805A09
    - 0x80803C12
    - 0x808097B8
    - type 32 subtype 1
    - type 40 subtype 1
    - type 48 subtype 1
    - type 26 subtype 1

To know more about what each one of these files mean or contain please take a look at `Tiger/formats.c` or `Tiger.Blocks.Types` for some information on them. Furthermore, take a look at how their parsers are implemented as they can give you a clearer idea on how it works.

To implement a very simple class that only parsed entries of the type `0x808099F1` and `0x80805A09` and then write them, what we can do would be as follows:
```cs
using Tiger;
using System;
using System.IO;

namespace Program
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string packages_path = @"C:\Program Files (x86)\Steam\steamapps\common\Destiny 2\packages";
            string output_path = @"C:\DestinyExtractionPath";
            Extractor extractor = new Extractor(packages_path, LoggerLevels.HighVerbouse);

            foreach (Package package in extractor.master_packages_stream())
            {
                for(int entry_index = 0; entry_index<package.entry_table().Count; entry_index++)
                {
                    Parsers.ParsedFile file;

                    Formats.Entry entry = package.entry_table()[entry_index];
                    switch(entry.entry_a)
                    {
                        case (uint)Blocks.Type.StringBank:
                            file = new Parsers.StringBankParser(package, entry_index, extractor).Parse();
                            file.WriteToFile(output_path);
                            break;

                        case (uint)Blocks.Type.StringReferenceIndexer:
                            file = new Parsers.StringReferenceIndexerParser(package, entry_index, extractor).Parse();
                            file.WriteToFile(output_path);
                            break;
                    
                        //Other cases for other block types go here
                    }
                }
            }
        }
    }
}
```

The above code checks for the type of the entry and if it matches the types implemented in `Tiger.Blocks.Types` then it writes it to the output path. Notet that whenever a `Parse()` method is called for any of the parsers, it returns a `Tiger.Parsers.ParsedFile`. This implementation allows for formats to always be serialized and ready to be written to a file which is in most cases the mian purpose of the analysis. If on the other hand, serialization is not the end goal, then there is one of two approaches that can be taken here:

    - Deserialize the object which will be easy since most of them are dictionaries anyway and Newtonsoft has all of the deserizalization functionality needed.
    - Use the function `ParseDeserialize()` in the specific parser that you're using. As of now, not all of the parsers include this method. However, in the future, it will be extended and included in all of the parsers so that this method is the main processing method and `Parse()` relies on it.