using System.Runtime.CompilerServices;

namespace Base16.ErrGen.Tests;

internal static class VerifyModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        UseSourceFileRelativeDirectory("__snapshots__");
    }
}
