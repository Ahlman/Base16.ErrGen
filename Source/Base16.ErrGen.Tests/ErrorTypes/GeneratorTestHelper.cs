using System.Collections.Immutable;
using Base16.ErrGen.ErrorTypes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Base16.ErrGen.Tests.ErrorTypes;

internal static class GeneratorTestHelper
{
    public static (
        ImmutableArray<Diagnostic> Diagnostics,
        GeneratedSource[] GeneratedSources
    ) RunGenerator(String source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !String.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new ErrorTypesGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics
        );

        var runResult = driver.GetRunResult();

        var generatedSources = runResult
            .Results.SelectMany(r => r.GeneratedSources)
            .Select(s => new GeneratedSource(s.HintName, s.SourceText.ToString()))
            .ToArray();

        var allDiagnostics = diagnostics
            .AddRange(runResult.Diagnostics)
            .AddRange(runResult.Results.SelectMany(r => r.Diagnostics));

        return (allDiagnostics, generatedSources);
    }

    public static String? FindGeneratedSource(GeneratedSource[] sources, String hintNamePart)
    {
        return sources.FirstOrDefault(s => s.HintName.Contains(hintNamePart))?.Content;
    }
}

internal sealed record GeneratedSource(String HintName, String Content);
