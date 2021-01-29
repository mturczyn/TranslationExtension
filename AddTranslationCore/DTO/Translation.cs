using AddTranslationCore.Abstractions;
using System;
using System.Globalization;

namespace AddTranslationCore.DTO
{
    public class Translation : BaseObservable, ICloneable
    {
        public Translation(string translationKey, string translationText, CultureInfo cultureInfo)
            => (TranslationKey, TranslationText, CultureInfo) = (translationKey, translationText, cultureInfo);


        private bool _isUnderEdition;
        public bool IsUnderEdition
        {
            get => _isUnderEdition;
            set => Set(value, ref _isUnderEdition);
        }

        private string _translationKey;
        public string TranslationKey
        {
            get => _translationKey;
            set => Set(value, ref _translationKey);
        }

        private string _translationText;
        public string TranslationText
        {
            get => _translationText;
            set => Set(value, ref _translationText);
        }

        private CultureInfo _cultureInfo;
        public CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set => Set(value, ref _cultureInfo);
        }

        public object Clone() => this.MemberwiseClone();
        
        #region Equality overrides, ToString override
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

        public override string ToString()
            => $"{CultureInfo} {TranslationKey} {TranslationText}";
        #endregion
    }
}