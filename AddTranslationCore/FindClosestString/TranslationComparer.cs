using AddTranslationCore.DTO;
using System.Collections.Generic;

namespace AddTranslationCore
{
    public class TranslationComparer : IComparer<Translation>
    {
        private string _textToCompareBy;

        public TranslationComparer(string textToCompareBy) => _textToCompareBy = textToCompareBy;

        public int Compare(Translation x, Translation y)
        {
            var compareResult = FindClosestString.GetDistance(_textToCompareBy, x.TranslationText) - FindClosestString.GetDistance(_textToCompareBy, y.TranslationText);
            if (compareResult > 0) return 1;
            else if (compareResult == 0) return 0;
            else return -1;
        }
    }
}
