using AddTranslationCore.DTO;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace AddTranslationCore.ResourceProjectHelpers
{
    /// <summary>
    /// Represents single dictionary file.
    /// Handles getting translations from file.
    /// </summary>
    class ResourceFile
    {
        /// <summary>
        /// Name of a node in resources XML file. It contanis descendant element nameed 'value'.
        /// 'name' attribute stored key of a translation.
        /// </summary>
        private const string DATA_NODE = "data";
        /// <summary>
        /// Name of a node that contains translation text.
        /// </summary>
        private const string VALUE_NODE = "value";
        /// <summary>
        /// Name of a attribute which value is key of a translation.
        /// </summary>
        private const string NAME_ATTRIBUTE = "name";

        private readonly string _fullPath;
        private readonly ILog _logger;

        public ResourceFile(string fullPath, bool isMainResource)
        {
            _logger = LogManager.GetLogger(nameof(ResourceFile));
            if (!File.Exists(fullPath)) throw new ArgumentException("Directory does not exist.");
            
            IsMainResource = isMainResource;
            if (!IsMainResource)
            {
                var fileName = Path.GetFileName(fullPath);
                var parts = fileName.Split('.');
                if (parts.Length < 3) throw new InvalidOperationException($"Resource file name should contain information about culture, while file name is {fileName}");
                // Resource file should be of form [base file name].[culture info abbreviation].resx, so here we get culture info.
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
        /// <summary>
        /// Determines if file is the main language used in application.
        /// </summary>
        public bool IsMainResource { get; set; }

        /// <summary>
        /// Get translation with specific key. It reads XML file until translation
        /// with specified name is found.
        /// </summary>
        /// <param name="translationName">Name of translation.</param>
        /// <returns></returns>
        public Translation GetTranslation(string translationName)
        {
            var translation = new Translation(translationName, string.Empty, CultureInfo);
            using (var fileStream = new FileStream(_fullPath, FileMode.Open))
            {
                using (var xmlReader = XmlReader.Create(fileStream))
                {
                    while (!xmlReader.EOF)
                    {
#warning Zmienić na async
                        if(!xmlReader.ReadToFollowing(DATA_NODE))
                            break;
                        
                        var name = xmlReader.GetAttribute(NAME_ATTRIBUTE);
                        if(name == null)
                        {
                            _logger.Warn($"In {DATA_NODE} node, but attribute {NAME_ATTRIBUTE} is not found. Path of the file {_fullPath}");
                            continue;
                        }
                        
                        if (name != translationName) 
                            continue;
                        
                        _logger.Debug($"Found translation with name {translationName}");

                        if (!xmlReader.ReadToDescendant(VALUE_NODE))
                        {
                            _logger.Warn($"Could not read descendant with name {VALUE_NODE}, {nameof(name)} is {name}");
                            continue;
                        }

                        _logger.Debug($"Trying to read content of a {VALUE_NODE}");

                        if (!xmlReader.Read())
                        {
                            _logger.Warn($"Could not process XML reader after reading {VALUE_NODE} node.");
                            continue;
                        }

                        _logger.Debug($"Translation read inside {VALUE_NODE} node. Node type is {xmlReader.NodeType} and value is {xmlReader.Value ?? "NULL"}");

                        if (xmlReader.NodeType != XmlNodeType.Text)
                        {
                            _logger.Warn($"Unexpected node type, expected {XmlNodeType.Text}");
                            continue;
                        }
                        translation.TranslationText = xmlReader.Value;
                        return translation;
                    }
                }
            }
            return translation;
        }

        /// <summary>
        /// Gets all translation and returns them as dictionary with names as keys.
        /// CAUTION: operation reads whole file, so it might result in huge data loaded into memory.
        /// </summary>
        /// <returns></returns>
        public Translation[] GetTranslations(out string[] duplicatedKeys)
        {
            // For now we prohibit reading all translations from secondary file.
            // As user can choose arbitrary project as main project, here we
            // prevent reading many big files into memory.
            if (! IsMainResource)
            {
                throw new InvalidOperationException("Getting all translations is allowed only for main resource.");
            }
            var translations = new List<Translation>();
            var duplicates = new List<string>();
            using (var fileStream = new FileStream(_fullPath, FileMode.Open))
            {
                using (var xmlReader = XmlReader.Create(fileStream))
                {
                    while (!xmlReader.EOF)
                    {
#warning Zmienić na async
                        if(!xmlReader.ReadToFollowing(DATA_NODE))
                            break;
                        
                        var name = xmlReader.GetAttribute(NAME_ATTRIBUTE);
                        if(name == null)
                        {
                            _logger.Warn($"In {DATA_NODE} node, but attribute {NAME_ATTRIBUTE} is not found. Path of the file {_fullPath}");
                            continue;
                        }
                        if (!xmlReader.ReadToDescendant(VALUE_NODE))
                        {
                            _logger.Warn($"Could not read descendant with name {VALUE_NODE}, {nameof(name)} is {name}");
                            continue;
                        }

                        _logger.Debug($"Trying to read content of a {VALUE_NODE}");

                        if (!xmlReader.Read())
                        {
                            _logger.Warn($"Could not process XML reader after reading {VALUE_NODE} node.");
                            continue;
                        }

                        _logger.Debug($"Translation read inside {VALUE_NODE} node. Node type is {xmlReader.NodeType} and value is {xmlReader.Value ?? "NULL"}");

                        if (xmlReader.NodeType != XmlNodeType.Text)
                        {
                            _logger.Warn($"Unexpected node type, expected {XmlNodeType.Text}");
                            continue;
                        }

                        var translationText = xmlReader.Value;
                        _logger.Debug($"Adding translation: {nameof(name)} = {name}, {nameof(translationText)} = {translationText}");

                        var translation = new Translation(name, translationText, CultureInfo);
                        if(translations.Any(t => t.TranslationKey == translation.TranslationKey))
                        {
                            _logger.Warn($"We already read translation with key {translation.TranslationKey}.");
                            duplicates.Add(translation.TranslationKey);
                        }
                        translations.Add(translation);
                    }
                }
            }
            duplicatedKeys = duplicates.ToArray();
            return translations.ToArray();
        }
    }
}
