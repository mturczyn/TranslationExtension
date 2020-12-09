using System;

namespace ConsoleTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(AlterLang.dx.blaString);
            AlterLang.dx.Culture = new System.Globalization.CultureInfo("en-US");
            Console.WriteLine(AlterLang.dx.blaString);
        }
    }
}
