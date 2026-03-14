using System.Collections.Immutable;
using Base16.ErrGen.ErrorTypes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Base16.ErrGen.Tests.ErrorTypes;

internal static class GeneratorTestHelper
{
    public static GeneratorResult RunGenerator(String source)
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

        var compilationErrors = outputCompilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        return new GeneratorResult(allDiagnostics, compilationErrors, generatedSources);
    }
}

internal sealed record GeneratorResult(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<Diagnostic> CompilationErrors,
    GeneratedSource[] GeneratedSources
)
{
    /// <summary>
    ///     Asserts that the generator produced no diagnostics and that the
    ///     generated code compiles without errors.
    /// </summary>
    public void AssertNoErrors()
    {
        Assert.Empty(Diagnostics);
        Assert.Empty(CompilationErrors);
    }

    public String? FindSource(String hintNamePart)
    {
        return GeneratedSources.FirstOrDefault(s => s.HintName.Contains(hintNamePart))?.Content;
    }
}

internal sealed record GeneratedSource(String HintName, String Content);
