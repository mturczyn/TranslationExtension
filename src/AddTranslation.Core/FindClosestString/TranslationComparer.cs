using AddTranslation.Core.ViewModel;
using System.Collections.Generic;

namespace AddTranslation.Core
{
    public class TranslationComparer : IComparer<Translation>
    {
        private string _textToCompareBy;

        public TranslationComparer(string textToCompareBy) => _textToCompareBy = textToCompareBy;

        public int Compare(Translation x, Translation y)
        {
            var compareResult = FindClosestString.GetDistance(_textToCompareBy, x.Text) - FindClosestString.GetDistance(_textToCompareBy, y.Text);
            if (compareResult > 0) return 1;
            else if (compareResult == 0) return 0;
            else return -1;
        }
    }
}
