using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var list1 = new List<string> { "A", "B", "C" };
        var list2 = new List<string> { "X", "Y" }; // replacements for indices 0 and 1

        // Full length (3)
        foreach (var combo in IndexAligned(list1.Count, list1, list2))
            Console.WriteLine(string.Join("+", combo));

        // 2-length prefixes that use at least one replacement
        int L = Math.Min(list2.Count, list1.Count - 1);
        if (L > 0)
        {
            foreach (var combo in IndexAligned(L, list1, list2))
            {
                bool usesReplacement = false;
                for (int i = 0; i < L; i++)
                    if (combo[i] != list1[i]) { usesReplacement = true; break; }

                if (usesReplacement)
                    Console.WriteLine(string.Join("+", combo));
            }
        }
    }

    static IEnumerable<List<string>> IndexAligned(int len, List<string> baseList, List<string> replList)
    {
        int masks = 1 << len;
        for (int mask = 0; mask < masks; mask++)
        {
            var cur = new List<string>(len);
            bool ok = true;

            for (int i = 0; i < len; i++)
            {
                bool useRepl = (mask & (1 << i)) != 0;
                if (useRepl)
                {
                    if (i < replList.Count) cur.Add(replList[i]);
                    else { ok = false; break; }
                }
                else
                {
                    cur.Add(baseList[i]);
                }
            }

            if (ok) yield return cur;
        }
    }
}
