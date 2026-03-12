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

    public static StringTemplate Parse(String template)
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
                parts.Add(new StringTemplateLiteralPart(template.Substring(open)));
                break;
            }

            var inner = template.Substring(open + 1, close - open - 1);
            var colonIndex = inner.IndexOf(':');
            if (colonIndex >= 0)
            {
                parts.Add(
                    new StringTemplateArgumentPart(
                        inner.Substring(0, colonIndex),
                        inner.Substring(colonIndex + 1)
                    )
                );
            }
            else
            {
                parts.Add(new StringTemplateArgumentPart(inner, null));
            }

            pos = close + 1;
        }

        return new StringTemplate(parts);
    }
}
