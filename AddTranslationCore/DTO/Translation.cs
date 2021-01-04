using AddTranslationCore.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AddTranslationCore.DTO
{
    public class Translation : BaseObservable
    {
        private readonly Dictionary<CultureInfo, string> _translations = new Dictionary<CultureInfo, string>();

        public ObservableCollection<CultureInfo> AvailableTranslations { get; } = new ObservableCollection<CultureInfo>();

        private string _translationKey;
        public string TranslationKey
        {
            get => _translationKey;
            set => SetPropertyAndRaise(value, ref _translationKey, nameof(TranslationKey));
        }

        private CultureInfo _selectedCultureInfo;
        public CultureInfo SelectedCultureInfo
        {
            get => _selectedCultureInfo;
            set
            {
                if (!SetPropertyAndRaise(value, ref _selectedCultureInfo, nameof(SelectedCultureInfo))) return;
                SelectedTranslationText = _translations[value];
            }
        }

        private string _selectedTranslationText;
        public string SelectedTranslationText
        {
            get => _selectedTranslationText;
            set => SetPropertyAndRaise(value, ref _selectedTranslationText, nameof(SelectedTranslationText));
        }

        public bool AddTranslation(CultureInfo ci, string translationText)
        {
            if (_translations.ContainsKey(ci))
            {
                return false;
            }
            _translations.Add(ci, translationText);
            AvailableTranslations.Add(ci);
            return true;
        }

        public void RemoveTranslation(CultureInfo ci)
        {
            _translations.Remove(ci);
            AvailableTranslations.Remove(ci);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Translation t)) return false;
            return this == t;
        }

        public static bool operator ==(Translation t1, Translation t2) => t1.TranslationKey == t2.TranslationKey;

        public static bool operator !=(Translation t1, Translation t2) => !(t1 == t2);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + _translationKey.GetHashCode();
            return hash;
        }
    }
}