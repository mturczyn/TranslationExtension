using AddTranslationCore.ViewModel;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AddTranslationCore.Model
{
    /// <summary>
    /// Represents single dictionary file.
    /// Handles getting translations from file.
    /// </summary>
    public class ResourceFile
    {
        public event Action<string[]> DuplicatedKeysFound;

        /// <summary>
        /// Name of a root node in resources XML file. It contains all other nodes.
        /// </summary>
        private const string ROOT_NODE = "root";
        /// <summary>
        /// Name of a node in resources XML file. It contains descendant element named 'value'.
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

        private const string SPACE_ATTRIBUTE = "space";

        private const string SPACE_ATTRIBUTE_VALUE = "preserve";

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
        /// Saves new translation to current project's dictionary.
        /// </summary>
        /// <param name="newTranslation"></param>
        /// <returns></returns>
        public bool SaveTranslation(Translation newTranslation)
        {
            _logger.Debug($"Saving {newTranslation} into {CultureInfo} ({IsMainResource}) language");
            var document = XDocument.Load(_fullPath);
            var rootNode = document.Element(ROOT_NODE);
            if(rootNode == null)
            {
                _logger.Warn("Trying to save translation, but could not find a root node of a document");
                return false;
            }
            var valueNode = new XElement(VALUE_NODE, newTranslation.Text);
            var dataNode = new XElement(DATA_NODE);
            dataNode.Add(new XAttribute(NAME_ATTRIBUTE, newTranslation.Key));
            dataNode.Add(new XAttribute(XNamespace.Xml + SPACE_ATTRIBUTE, SPACE_ATTRIBUTE_VALUE));
            dataNode.Add(valueNode);
            rootNode.Add(dataNode);
            document.Save(_fullPath);
            
            return true;
        }

        /// <summary>
        /// Updated edited translation based on passed original key.
        /// </summary>
        /// <param name="editedTranslation">Edited translation, that will be saved.</param>
        /// <param name="originalTranslationKey">Key of translation before edition (so it can be found in a file and updated).</param>
        /// <returns></returns>
        public bool SaveTranslation(Translation editedTranslation, string originalTranslationKey)
        {
            _logger.Debug($"Saving edited {editedTranslation} into {CultureInfo} ({IsMainResource}) language");
            var document = XDocument.Load(_fullPath);
            var rootNode = document.Element(ROOT_NODE);
            if (rootNode == null)
            {
                _logger.Warn("Trying to save translation, but could not find a root node of a document");
                return false;
            }
            // We don't use Signle method here, as we allow possibility to have duplicate keys.
            // But for sure it should exist in a file.
            var foundElement = rootNode.Elements(DATA_NODE).First(e => e.Attribute(NAME_ATTRIBUTE)?.Value == originalTranslationKey);
            foundElement.SetAttributeValue(NAME_ATTRIBUTE, editedTranslation.Key);
            foundElement.SetElementValue(VALUE_NODE, editedTranslation.Text);
            document.Save(_fullPath);

            return true;
        }

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
            using (var xmlReader = XmlReader.Create(fileStream))
            {
                while (!xmlReader.EOF)
                {
#warning Zmienić na async
                    if (!xmlReader.ReadToFollowing(DATA_NODE))
                        break;

                    var name = xmlReader.GetAttribute(NAME_ATTRIBUTE);
                    if (name == null)
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
                    translation.Text = xmlReader.Value;
                    return translation;
                }
            }
            return translation;
        }

        /// <summary>
        /// Gets all translations from a file.
        /// CAUTION: operation reads whole file, so it might result in huge data loaded into memory.
        /// </summary>
        /// <returns></returns>
        public Translation[] GetTranslations()
        {
            var translations = new List<Translation>();
            var duplicates = new List<string>();
            using (var fileStream = new FileStream(_fullPath, FileMode.Open))
            {
                using (var xmlReader = XmlReader.Create(fileStream))
                {
                    while (!xmlReader.EOF)
                    {
#warning Zmienić na async
                        if (!xmlReader.ReadToFollowing(DATA_NODE))
                            break;

                        var name = xmlReader.GetAttribute(NAME_ATTRIBUTE);
                        if (name == null)
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
                        if (translations.Any(t => t.Key == translation.Key))
                        {
                            _logger.Warn($"We already read translation with key {translation.Key}.");
                            duplicates.Add(translation.Key);
                        }
                        translations.Add(translation);
                    }
                }
            }
            if (duplicates.Any())
                DuplicatedKeysFound?.Invoke(duplicates.ToArray());
            return translations.ToArray();
        }
    }
}
