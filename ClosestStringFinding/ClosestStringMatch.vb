' Klasa obliczająca odległości między całymi zdaniami.
' Algorytm znaleziony na: https://stackoverflow.com/questions/5859561/getting-the-closest-string-match
Public Module ClosestStringMatch
    Public Function GetDistance(s1 As String, s2 As String) As Double
        Dim wordValue = valueWords(s1, s2)
        Dim phraseValue = valuePhrase(s1, s2) - 0.8 * Math.Abs(s1.Length - s2.Length)

        Return Math.Min(wordValue, phraseValue) * 0.8 + Math.Max(wordValue, phraseValue) * 0.2
    End Function

    'Calculate the Levenshtein Distance between two strings (the number of insertions,
    'deletions, and substitutions needed to transform the first string into the second)
    Private Function LevenshteinDistance(ByRef S1 As String, ByVal S2 As String) As Long
        Dim L1 As Long, L2 As Long, D(,) As Long 'Length of input strings and distance matrix
        Dim i As Long, j As Long, cost As Long 'loop counters and cost of substitution for current letter
        Dim cI As Long, cD As Long, cS As Long 'cost of next Insertion, Deletion and Substitution
        L1 = Len(S1) : L2 = Len(S2)
        ReDim D(0 To L1, 0 To L2)
        For i = 0 To L1 : D(i, 0) = i : Next i
        For j = 0 To L2 : D(0, j) = j : Next j

        For j = 1 To L2
            For i = 1 To L1
                cost = Math.Abs(StrComp(Mid$(S1, i, 1), Mid$(S2, j, 1), vbTextCompare))
                cI = D(i - 1, j) + 1
                cD = D(i, j - 1) + 1
                cS = D(i - 1, j - 1) + cost
                If cI <= cD Then 'Insertion or Substitution
                    If cI <= cS Then D(i, j) = cI Else D(i, j) = cS
                Else 'Deletion or Substitution
                    If cD <= cS Then D(i, j) = cD Else D(i, j) = cS
                End If
            Next i
        Next j
        LevenshteinDistance = D(L1, L2)
    End Function

    Private Function valuePhrase(ByRef S1$, ByRef S2$)
        valuePhrase = LevenshteinDistance(S1, S2)
    End Function

    Private Function valueWords(ByRef S1$, ByRef S2$)
        Dim wordsS1$(), wordsS2$()
        wordsS1 = SplitMultiDelims(S1, " _-")
        wordsS2 = SplitMultiDelims(S2, " _-")
        Dim word1%, word2%, thisD#, wordbest#
        Dim wordsTotal#
        For word1 = LBound(wordsS1) To UBound(wordsS1)
            wordbest = Len(S2)
            For word2 = LBound(wordsS2) To UBound(wordsS2)
                thisD = LevenshteinDistance(wordsS1(word1), wordsS2(word2))
                If thisD < wordbest Then wordbest = thisD
                If thisD = 0 Then GoTo foundbest
            Next word2
foundbest:
            wordsTotal = wordsTotal + wordbest
        Next word1
        valueWords = wordsTotal
    End Function

    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    ' SplitMultiDelims
    ' This function splits Text into an array of substrings, each substring
    ' delimited by any character in DelimChars. Only a single character
    ' may be a delimiter between two substrings, but DelimChars may
    ' contain any number of delimiter characters. It returns a single element
    ' array containing all of text if DelimChars is empty, or a 1 or greater
    ' element array if the Text is successfully split into substrings.
    ' If IgnoreConsecutiveDelimiters is true, empty array elements will not occur.
    ' If Limit greater than 0, the function will only split Text into 'Limit'
    ' array elements or less. The last element will contain the rest of Text.
    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Function SplitMultiDelims(ByRef Text As String, ByRef DelimChars As String,
            Optional ByVal IgnoreConsecutiveDelimiters As Boolean = False,
            Optional ByVal Limit As Long = -1) As String()
        Dim ElemStart As Long, N As Long, M As Long, Elements As Long
        Dim lDelims As Long, lText As Long
        Dim Arr() As String

        lText = Len(Text)
        lDelims = Len(DelimChars)
        If lDelims = 0 Or lText = 0 Or Limit = 1 Then
            ReDim Arr(0 To 0)
            Arr(0) = Text
            SplitMultiDelims = Arr
            Exit Function
        End If
        ReDim Arr(0 To IIf(Limit = -1, lText - 1, Limit))

        Elements = 0 : ElemStart = 1
        For N = 1 To lText
            If InStr(DelimChars, Mid(Text, N, 1)) Then
                Arr(Elements) = Mid(Text, ElemStart, N - ElemStart)
                If IgnoreConsecutiveDelimiters Then
                    If Len(Arr(Elements)) > 0 Then Elements = Elements + 1
                Else
                    Elements = Elements + 1
                End If
                ElemStart = N + 1
                If Elements + 1 = Limit Then Exit For
            End If
        Next N
        'Get the last token terminated by the end of the string into the array
        If ElemStart <= lText Then Arr(Elements) = Mid(Text, ElemStart)
        'Since the end of string counts as the terminating delimiter, if the last character
        'was also a delimiter, we treat the two as consecutive, and so ignore the last elemnent
        If IgnoreConsecutiveDelimiters Then If Len(Arr(Elements)) = 0 Then Elements = Elements - 1

        ReDim Preserve Arr(0 To Elements) 'Chop off unused array elements
        SplitMultiDelims = Arr
    End Function
End Module
