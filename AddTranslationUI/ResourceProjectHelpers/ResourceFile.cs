using System;
using System.Globalization;
using System.IO;

namespace AddTranslationUI.ResourceProjectHelpers
{
    class ResourceFile
    {
        private readonly CultureInfo _cultureInfo;
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
                _cultureInfo = new CultureInfo(parts[parts.Length - 2]);
            }
            else
            {
                _cultureInfo = CultureInfo.InvariantCulture;
            }
            // If everything is set correctly, we set the path.
            _fullPath = fullPath;
        }
    }
}
