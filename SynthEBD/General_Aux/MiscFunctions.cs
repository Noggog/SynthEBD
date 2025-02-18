﻿namespace SynthEBD;

public class MiscFunctions
{
    public static bool StringHashSetsEqualCaseInvariant(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var s in a)
        {
            if (!b.Contains(s, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }
}