using System;
using System.Collections.Generic;

namespace Base16.ErrGen.ErrorTypes;

internal abstract record StringTemplatePart;

internal sealed record StringTemplateLiteralPart(String Value) : StringTemplatePart;

internal sealed record StringTemplateArgumentPart(String Name, String? Type) : StringTemplatePart;

internal sealed class StringTemplate
{
    public IReadOnlyList<StringTemplatePart> Parts { get; }

    private StringTemplate(IReadOnlyList<StringTemplatePart> parts)
    {
        Parts = parts;
    }

    public static Boolean TryParse(String template, out StringTemplate? result, out String? error)
    {
        var parts = new List<StringTemplatePart>();
        var pos = 0;

        while (pos < template.Length)
        {
            var open = template.IndexOf('{', pos);
            if (open < 0)
            {
                parts.Add(new StringTemplateLiteralPart(template.Substring(pos)));
                break;
            }

            if (open > pos)
            {
                parts.Add(new StringTemplateLiteralPart(template.Substring(pos, open - pos)));
            }

            var close = template.IndexOf('}', open);
            if (close < 0)
            {
                result = null;
                error = "unclosed placeholder '{' without matching '}'";
                return false;
            }

            var inner = template.Substring(open + 1, close - open - 1);

            if (String.IsNullOrWhiteSpace(inner))
            {
                result = null;
                error = "empty placeholder '{}' is not allowed";
                return false;
            }

            var colonIndex = inner.IndexOf(':');
            if (colonIndex >= 0)
            {
                var name = inner.Substring(0, colonIndex);
                var type = inner.Substring(colonIndex + 1);

                if (String.IsNullOrWhiteSpace(name))
                {
                    result = null;
                    error = $"placeholder '{{:{type}}}' is missing a name";
                    return false;
                }

                if (String.IsNullOrWhiteSpace(type))
                {
                    result = null;
                    error = $"placeholder '{{{name}:}}' is missing a type after ':'";
                    return false;
                }

                parts.Add(new StringTemplateArgumentPart(name, type));
            }
            else
            {
                parts.Add(new StringTemplateArgumentPart(inner, null));
            }

            pos = close + 1;
        }

        result = new StringTemplate(parts);
        error = null;
        return true;
    }
}
