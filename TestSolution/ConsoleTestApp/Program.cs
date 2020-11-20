using System;

namespace ConsoleTestApp
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(AlternativeLanguages.Resource.tesString123);
      Console.WriteLine(Languages.Resource.testString34);
      var str = Languages.Resource.finallyAddedString;
    }
  }
}
