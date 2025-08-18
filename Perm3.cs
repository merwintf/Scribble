using System;
using System.Collections.Generic;
using System.Text;

static class IndexAlignedPermuter
{
    // Prints:
    // 1) Full-length combos of base.Count, using base by default, or any list[j][i] if available at index i.
    // 2) Optionally, prefix combos of length = minCount (min length across all lists),
    //    and you can choose to print only those that use at least one non-base value.
    public static void Print(
        IReadOnlyList<string> baseList,
        IReadOnlyList<IReadOnlyList<string>> otherLists,  // 0..M-1 replacement lists
        bool printPrefixes = true,
        bool onlyPrefixIfUsesReplacement = true)
    {
        int n = baseList.Count;
        int m = otherLists.Count;

        // optionsPerIndex[i] = how many choices at index i (1 for base + available replacements)
        var optionsPerIndex = new int[n];
        for (int i = 0; i < n; i++)
        {
            int opts = 1; // base
            for (int j = 0; j < m; j++)
                if (i < otherLists[j].Count) opts++;
            optionsPerIndex[i] = opts;
        }

        var sb = new StringBuilder(128);

        // ---- FULL LENGTH: mixed-radix counter over optionsPerIndex ----
        {
            var digits = new int[n]; // digit[i] in [0 .. optionsPerIndex[i)-1]; 0 means base, 1.. means otherLists[k]
            while (true)
            {
                sb.Clear();
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append('+');
                    int d = digits[i];
                    if (d == 0) sb.Append(baseList[i]);
                    else
                    {
                        // map digit (1..opts-1) -> which other list has this index
                        int skip = d - 1;
                        for (int j = 0; j < m; j++)
                        {
                            if (i < otherLists[j].Count)
                            {
                                if (skip == 0) { sb.Append(otherLists[j][i]); break; }
                                skip--;
                            }
                        }
                    }
                }
                Console.WriteLine(sb.ToString());

                // increment mixed-radix
                int pos = n - 1;
                for (; pos >= 0; pos--)
                {
                    digits[pos]++;
                    if (digits[pos] < optionsPerIndex[pos]) break;
                    digits[pos] = 0;
                }
                if (pos < 0) break; // done
            }
        }

        // ---- PREFIXES (length = minCount across all lists) ----
        if (printPrefixes)
        {
            int minCount = baseList.Count;
            for (int j = 0; j < m; j++)
                if (otherLists[j].Count < minCount) minCount = otherLists[j].Count;

            if (minCount > 0)
            {
                var optionsPerIndexPrefix = new int[minCount];
                for (int i = 0; i < minCount; i++)
                {
                    int opts = 1;
                    for (int j = 0; j < m; j++)
                        if (i < otherLists[j].Count) opts++;
                    optionsPerIndexPrefix[i] = opts;
                }

                var digits = new int[minCount];
                while (true)
                {
                    bool usesReplacement = false;

                    sb.Clear();
                    for (int i = 0; i < minCount; i++)
                    {
                        if (i > 0) sb.Append('+');
                        int d = digits[i];
                        if (d == 0) sb.Append(baseList[i]);
                        else
                        {
                            usesReplacement = true;
                            int skip = d - 1;
                            for (int j = 0; j < m; j++)
                            {
                                if (i < otherLists[j].Count)
                                {
                                    if (skip == 0) { sb.Append(otherLists[j][i]); break; }
                                    skip--;
                                }
                            }
                        }
                    }

                    if (!onlyPrefixIfUsesReplacement || usesReplacement)
                        Console.WriteLine(sb.ToString());

                    // increment mixed-radix
                    int pos = minCount - 1;
                    for (; pos >= 0; pos--)
                    {
                        digits[pos]++;
                        if (digits[pos] < optionsPerIndexPrefix[pos]) break;
                        digits[pos] = 0;
                    }
                    if (pos < 0) break; // done
                }
            }
        }
    }
}

class Program
{
    static void Main()
    {
        var baseList = new List<string> { "A", "B", "C" };
        var listX = new List<string> { "X", "Y" };
        var listP = new List<string> { "P" };          // example extra list
        var listQ = new List<string> { "Q", "R", "S" };// another extra list

        IndexAlignedPermuter.Print(
            baseList,
            new List<IReadOnlyList<string>> { listX, listP, listQ },
            printPrefixes: true,
            onlyPrefixIfUsesReplacement: true
        );
    }
}
