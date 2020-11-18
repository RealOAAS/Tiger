// DataTool.ConvertLogic.Sound class: https://github.com/overtools/OWLib/blob/develop/DataTool/ConvertLogic/Sound.cs
// Modified RevorbStd: https://github.com/xyx0826/revorbstd/tree/big-enough
// Modified librevorb.dll (RevorbStd dependency): https://github.com/xyx0826/librevorb/releases/tag/v0.5

using DataTool.ConvertLogic;
using RevorbStd;
using System.IO;

namespace Tiger.Parsers.Dependencies
{
    static class WemConverter
    {
        private const string CodebookPath = @"packed_codebooks_aoTuV_603.bin";

        private static bool _fileChecked;

        public static MemoryStream ConvertSoundFile(Stream stream)
        {
            if (!_fileChecked)
                CheckCodebookFile();

            var vorbisStream = new MemoryStream();
            using (Sound.WwiseRIFFVorbis vorbis = new Sound.WwiseRIFFVorbis(stream, CodebookPath))
            {
                vorbis.ConvertToOgg(vorbisStream);
                vorbisStream.Position = 0;
            }
            return Revorb.Jiggle(vorbisStream.GetBuffer());
        }

        private static void CheckCodebookFile()
        {
            if (!(_fileChecked = File.Exists(CodebookPath)))
            {
                throw new FileNotFoundException($"WEM conversion codebook {CodebookPath} is missing.");
            }
        }
    }
}
