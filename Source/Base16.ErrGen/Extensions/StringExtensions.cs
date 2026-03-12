using System;

namespace Base16.ErrGen.Extensions;

internal static class StringExtensions
{
    public static String ToCamelCase(this String source)
    {
        return Char.ToLowerInvariant(source[0]) + source.Substring(1);
    }
}
