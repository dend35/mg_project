using System;
using MapMerger.Core;

namespace MapMergerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            MapHelper.RenderMap(type: MapType.Normal);
            Console.ReadLine();
        }
    }
}
