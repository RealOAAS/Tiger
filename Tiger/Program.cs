using System;

namespace Tiger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Tiger.Extractor extractor = new Tiger.Extractor("/Volumes/WinToUSB/Program Files (x86)/Steam/steamapps/common/Destiny 2/packages", true);
        }
    }
}
