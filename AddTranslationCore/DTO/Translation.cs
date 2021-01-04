using AddTranslationCore.Abstractions;
using System.Globalization;

namespace AddTranslationCore.DTO
{
    public class Translation : BaseObservable
    {
        public Translation(string translationKey, string translationText, CultureInfo cultureInfo)
            => (TranslationKey, TranslationText, CultureInfo) = (translationKey, translationText, cultureInfo);

        private string _translationKey;
        public string TranslationKey
        {
            get => _translationKey;
            set => SetPropertyAndRaise(value, ref _translationKey, nameof(TranslationKey));
        }

        private string _translationText;
        public string TranslationText
        {
            get => _translationText;
            set => SetPropertyAndRaise(value, ref _translationText, nameof(TranslationText));
        }

        private CultureInfo _cultureInfo;
        public CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set => SetPropertyAndRaise(value, ref _cultureInfo, nameof(CultureInfo));
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