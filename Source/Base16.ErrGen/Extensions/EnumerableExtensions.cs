using System;
using System.Collections.Generic;

namespace Base16.ErrGen.Extensions;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector
    )
    {
        var hashSet = new HashSet<TKey>();

        foreach (var item in source)
        {
            if (hashSet.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }
}
