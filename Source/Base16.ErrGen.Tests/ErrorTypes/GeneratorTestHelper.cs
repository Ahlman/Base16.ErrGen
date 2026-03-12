using System.Collections.Immutable;
using Base16.ErrGen.ErrorTypes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Base16.ErrGen.Tests.ErrorTypes;

internal static class GeneratorTestHelper
{
    public static (ImmutableArray<Diagnostic> Diagnostics, String[] GeneratedSources) RunGenerator(
        String source
    )
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
            .GeneratedTrees.Select(t => t.GetText().ToString())
            .ToArray();

        return (diagnostics, generatedSources);
    }

    public static String? FindGeneratedSource(String[] sources, String fileNamePart)
    {
        // The PostInitializationOutput sources come first, then the per-type sources.
        // We match by checking if the source contains distinguishing content.
        return sources.FirstOrDefault(s => s.Contains(fileNamePart));
    }
}
