using System;
using System.Collections.Generic;
using System.Text;

namespace Base16.ErrGen.Utils.Code;

internal abstract class CodeBuilder
{
    public const String Indent = "    ";

    protected sealed class DisposeAction(Action disposeAction) : IDisposable
    {
        public void Dispose()
        {
            disposeAction();
        }
    }

    private readonly StringBuilder _buffer = new();
    private Int32 _indent = 0;

    public override String ToString()
    {
        return _buffer.ToString();
    }

    public IDisposable PushIndent()
    {
        _indent++;

        return new DisposeAction(() =>
        {
            _indent--;
        });
    }

    public void AppendLine()
    {
        _buffer.AppendLine();
    }

    public void AppendLineIndented(String text)
    {
        using (PushIndent())
        {
            AppendLine(text);
        }
    }

    public void AppendLine(String text)
    {
        if (String.IsNullOrWhiteSpace(text))
        {
            _buffer.AppendLine();
            return;
        }

        for (var i = 0; i < _indent; i++)
        {
            _buffer.Append(Indent);
        }

        _buffer.AppendLine(text);
    }

    public void AppendLines(String lines)
    {
        var linesArray = lines.Split('\n');

        foreach (var line in linesArray)
        {
            AppendLine(line.Trim('\r'));
        }
    }

    public void AppendLines(IEnumerable<String> lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    public void AppendLinesIndented(IEnumerable<String> lines)
    {
        using (PushIndent())
        {
            AppendLines(lines);
        }
    }

    public void AppendLines(params String[] lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    public void AppendLinesIndented(params String[] lines)
    {
        using (PushIndent())
        {
            AppendLines(lines);
        }
    }
}
