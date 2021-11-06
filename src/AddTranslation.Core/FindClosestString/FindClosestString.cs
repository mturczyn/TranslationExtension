using System;

namespace AddTranslationCore
{
    /// <summary>
    /// This class takes care of calculating distance between strings (words, sentences).
    /// Algorithm found on: https://stackoverflow.com/questions/5859561/getting-the-closest-string-m
    /// </summary>
    public static class FindClosestString
    {
        /// <summary>
        /// Gets the distance between sentences.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static double GetDistance(string s1, string s2)
        {
            // When trying to find the closest string, casing should be
            // completely ignored.
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            double wordValue = valueWords(s1, s2);
            double phraseValue = valuePhrase(s1, s2) - 0.8 * Math.Abs(s1.Length - s2.Length);

            var dist = Math.Min(wordValue, phraseValue) * 0.8D + Math.Max(wordValue, phraseValue) * 0.2;
            return dist;
        }

        /// <summary>
        /// Calculate the Levenshtein Distance between two strings (the number of insertions,
        /// deletions, and substitutions needed to transform the first string into the second)
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static int LevenshteinDistance(string s1, string s2)
        {
            int l1, l2;
            bool cost;
            int cI, cD, cS;

            l1 = s1.Length + 1;
            l2 = s2.Length + 1;
            int[,] D = new int[l1, l2];

            for (int i = 0; i < l1; i++) D[i, 0] = i;
            for (int i = 0; i < l2; i++) D[0, i] = i;

            for (int j = 1; j < l2; j++)
            {
                for (int i = 1; i < l1; i++)
                {
                    cost = s1[i - 1] == s2[j - 1];
                    cI = D[i - 1, j] + 1;
                    cD = D[i, j - 1] + 1;
                    cS = D[i - 1, j - 1];
                    if (!cost) cS++;

                    D[i, j] = Math.Min(cI, Math.Min(cD, cS));
                }
            }
            return D[l1 - 1, l2 - 1];
        }
        private static int valuePhrase(string s1, string s2) => LevenshteinDistance(s1, s2);
        private static int valueWords(string s1, string s2)
        {
            // TODO: Maybe it would be good to move those to settings
            var wordsS1 = s1.Split(new char[] { ' ', '_', '-' });
            var wordsS2 = s2.Split(new char[] { ' ', '_', '-' });
            int wordBest, wordsTotal = 0;

            foreach (var word1 in wordsS1)
            {
                wordBest = s2.Length;
                foreach (var word2 in wordsS2)
                {
                    var thisD = LevenshteinDistance(word1, word2);
                    if (thisD < wordBest) wordBest = thisD;
                    if (thisD == 0) break;
                }
                wordsTotal += wordBest;
            }

            return wordsTotal;
        }
    }
}
