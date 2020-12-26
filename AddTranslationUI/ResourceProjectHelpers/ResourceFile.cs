using AddTranslationUI.DTO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace AddTranslationUI.ResourceProjectHelpers
{
    class ResourceFile
    {
        private readonly string _fullPath;

        public ResourceFile(string fullPath, bool isMainResource)
        {
            if (!File.Exists(fullPath)) throw new ArgumentException("Directory does not exist.");

            if (!isMainResource)
            {
                var fileName = Path.GetFileName(fullPath);
                var parts = fileName.Split('.');
                if (parts.Length < 3) throw new InvalidOperationException($"Resource file name should contain information about culture, while file name is {fileName}");
                // Resource file should by of form [base file name].[culture info abbreviation].resx, so here we get culture info.
                CultureInfo = new CultureInfo(parts[parts.Length - 2]);
            }
            else
            {
                CultureInfo = CultureInfo.InvariantCulture;
            }
            // If everything is set correctly, we set the path.
            _fullPath = fullPath;
        }
        
        public CultureInfo CultureInfo { get; }

        public Dictionary<string, Translation> GetTranslations()
        {
            using (var fileStream = new FileStream(_fullPath, FileMode.Open))
            {
                using (var xmlReader = XmlReader.Create(fileStream))
                {
                    while (!xmlReader.EOF)
                    {
#warning Zmienić na async
                        xmlReader.Read();
                        var name = xmlReader.GetAttribute("name");
                    }
                }

            }
            return null;
        }
    }
}
