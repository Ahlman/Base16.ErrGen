using System.Runtime.CompilerServices;
using Base16.ErrGen.Tests.ErrorTypes;

namespace Base16.ErrGen.Tests;

internal static class VerifyUtils
{
    /// <summary>
    ///     Verifies the generated source for the given hint name part using Verify snapshot testing.
    ///     Snapshots are stored with a <c>.cs</c> extension and excluded from compilation via the csproj.
    /// </summary>
    public static SettingsTask VerifyGeneratedSource(
        GeneratorResult result,
        String hintNamePart,
        [CallerFilePath] String sourceFile = "",
        [CallerMemberName] String memberName = ""
    )
    {
        result.AssertNoErrors();

        var source = result.FindSource(hintNamePart);
        Assert.NotNull(source);

        return Verify(source, extension: "cs", sourceFile: sourceFile).UseMethodName(memberName);
    }

    /// <summary>
    ///     Verifies multiple generated sources in a single snapshot.
    ///     Each source is separated by a header comment with the hint name.
    /// </summary>
    public static SettingsTask VerifyGeneratedSources(
        GeneratorResult result,
        String[] hintNameParts,
        [CallerFilePath] String sourceFile = "",
        [CallerMemberName] String memberName = ""
    )
    {
        result.AssertNoErrors();

        var combined = String.Join(
            Environment.NewLine,
            hintNameParts.Select(hint =>
            {
                var source = result.FindSource(hint);
                Assert.NotNull(source);
                return $"// === {hint} ==={Environment.NewLine}{source}";
            })
        );

        return Verify(combined, extension: "cs", sourceFile: sourceFile).UseMethodName(memberName);
    }
}
