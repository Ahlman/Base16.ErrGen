# ErrorBaseType Assembly Attribute Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow users to configure a base type (interface or class) that all generated error types inherit from via `[assembly: ErrorBaseType(typeof(T))]`.

**Architecture:** Add a new auto-generated `ErrorBaseTypeAttribute`, resolve it via `CompilationProvider` in the incremental pipeline, `Combine()` with the existing per-error-type pipeline, and conditionally emit inheritance clauses and skip `Message` generation. Diagnostics are emitted for invalid configurations.

**Tech Stack:** C# / Roslyn IIncrementalGenerator / xUnit

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs` | Modify | Add attribute emission, resolution pipeline, combine, modify code generation |
| `Source/Base16.ErrGen/ErrorTypes/ErrorBaseTypeInfo.cs` | Create | Equatable record holding resolved base type info |
| `Source/Base16.ErrGen/ErrorTypes/ErrorDiagnostics.cs` | Create | DiagnosticDescriptor constants for ERR001-ERR005 |
| `Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs` | Modify | Add 12 new test cases |

---

## Task 1: Add ErrorDiagnostics

**Files:**
- Create: `Source/Base16.ErrGen/ErrorTypes/ErrorDiagnostics.cs`

- [ ] **Step 1: Create the diagnostics file**

```csharp
using Microsoft.CodeAnalysis;

namespace Base16.ErrGen.ErrorTypes;

internal static class ErrorDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidBaseType = new(
        id: "ERR001",
        title: "Invalid ErrorBaseType",
        messageFormat: "ErrorBaseType: '{0}' must be a class or interface",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor StructCannotInheritClass = new(
        id: "ERR002",
        title: "Record struct cannot inherit from class",
        messageFormat: "ErrorBaseType: record struct '{0}' cannot inherit from class '{1}'",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor AbstractBaseType = new(
        id: "ERR003",
        title: "Abstract base type not allowed",
        messageFormat: "ErrorBaseType: '{0}' must not be abstract",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor GenericBaseType = new(
        id: "ERR004",
        title: "Generic base type not allowed",
        messageFormat: "ErrorBaseType: '{0}' must not be a generic type",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MessagePropertyNotString = new(
        id: "ERR005",
        title: "Message property must be String",
        messageFormat: "ErrorBaseType: '{0}.Message' must be of type String",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build Source/Base16.ErrGen/Base16.ErrGen.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
git add Source/Base16.ErrGen/ErrorTypes/ErrorDiagnostics.cs
git commit -m "Add diagnostic descriptors for ErrorBaseType validation"
```

---

## Task 2: Add ErrorBaseTypeInfo

**Files:**
- Create: `Source/Base16.ErrGen/ErrorTypes/ErrorBaseTypeInfo.cs`

- [ ] **Step 1: Create the equatable record**

```csharp
using System;

namespace Base16.ErrGen.ErrorTypes;

internal sealed record ErrorBaseTypeInfo(
    String FullyQualifiedName,
    Boolean IsInterface,
    Boolean HasMessageProperty
);
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build Source/Base16.ErrGen/Base16.ErrGen.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```
git add Source/Base16.ErrGen/ErrorTypes/ErrorBaseTypeInfo.cs
git commit -m "Add ErrorBaseTypeInfo record for pipeline data"
```

---

## Task 3: Emit ErrorBaseTypeAttribute in post-initialization

**Files:**
- Modify: `Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs:17-61`

- [ ] **Step 1: Write the failing test**

Add to `Test_ErrorTypesGenerator.cs`:

```csharp
[Fact]
public void Generates_ErrorBaseTypeAttribute()
{
    // Arrange
    var source = "";

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var attributeSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "class ErrorBaseTypeAttribute"
    );
    Assert.NotNull(attributeSource);
    Assert.Contains("System.AttributeTargets.Assembly", attributeSource);
    Assert.Contains("AllowMultiple = false", attributeSource);
    Assert.Contains("System.Type", attributeSource);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "Generates_ErrorBaseTypeAttribute"`
Expected: FAIL — `attributeSource` is null

- [ ] **Step 3: Add the attribute source to post-initialization**

In `ErrorTypesGenerator.cs`, inside the `RegisterPostInitializationOutput` lambda, after the `ErrorAttribute` `AddSource` call, add:

```csharp
ctx.AddSource(
    "Base16.ErrGen.ErrorBaseTypeAttribute.g.cs",
    """
    // <auto-generated/>
    #nullable enable

    namespace Base16.ErrGen;

    /// <summary>
    ///     Configures a base type (interface or class) that all generated error types inherit from.
    /// </summary>
    #pragma warning disable CS9113 // Parameter is unread.
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ErrorBaseTypeAttribute : System.Attribute
    {
        /// <summary>
        ///     The base type to inherit from.
        /// </summary>
        public System.Type Type { get; }

        public ErrorBaseTypeAttribute(System.Type type)
        {
            Type = type;
        }
    }
    #pragma warning restore CS9113 // Parameter is unread.
    """
);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "Generates_ErrorBaseTypeAttribute"`
Expected: PASS

- [ ] **Step 5: Commit**

```
git add Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs
git commit -m "Emit ErrorBaseTypeAttribute in post-initialization"
```

---

## Task 4: Add CompilationProvider resolution pipeline and Combine with existing pipeline

**Files:**
- Modify: `Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs:15-69`

- [ ] **Step 1: Add the resolution pipeline**

Add a new constant at the top of the class:

```csharp
private const String ErrorBaseTypeAttributeName = "Base16.ErrGen.ErrorBaseTypeAttribute";
```

Replace the current `Initialize` method body (after `RegisterPostInitializationOutput`) with:

```csharp
var baseTypeInfo = context.CompilationProvider.Select(
    static (compilation, ct) =>
    {
        var assemblyAttributes = compilation.Assembly.GetAttributes();
        var attr = assemblyAttributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == ErrorBaseTypeAttributeName
        );

        if (attr is null)
            return (Info: (ErrorBaseTypeInfo?)null, Diagnostic: (Diagnostic?)null);

        if (
            attr.ConstructorArguments.Length == 0
            || attr.ConstructorArguments[0].Value is not INamedTypeSymbol typeSymbol
        )
        {
            return (
                Info: null,
                Diagnostic: Diagnostic.Create(
                    ErrorDiagnostics.InvalidBaseType,
                    attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                    attr.ConstructorArguments.Length > 0
                        ? attr.ConstructorArguments[0].Value?.ToString() ?? "unknown"
                        : "unknown"
                )
            );
        }

        if (
            typeSymbol.TypeKind != TypeKind.Interface
            && typeSymbol.TypeKind != TypeKind.Class
        )
        {
            return (
                Info: null,
                Diagnostic: Diagnostic.Create(
                    ErrorDiagnostics.InvalidBaseType,
                    attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                    typeSymbol.ToDisplayString()
                )
            );
        }

        if (typeSymbol.IsAbstract && typeSymbol.TypeKind == TypeKind.Class)
        {
            return (
                Info: null,
                Diagnostic: Diagnostic.Create(
                    ErrorDiagnostics.AbstractBaseType,
                    attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                    typeSymbol.ToDisplayString()
                )
            );
        }

        if (typeSymbol.IsGenericType)
        {
            return (
                Info: null,
                Diagnostic: Diagnostic.Create(
                    ErrorDiagnostics.GenericBaseType,
                    attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                    typeSymbol.ToDisplayString()
                )
            );
        }

        var messageProperty = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "Message");

        if (messageProperty is not null && messageProperty.Type.SpecialType != SpecialType.System_String)
        {
            return (
                Info: null,
                Diagnostic: Diagnostic.Create(
                    ErrorDiagnostics.MessagePropertyNotString,
                    attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                    typeSymbol.ToDisplayString()
                )
            );
        }

        return (
            Info: (ErrorBaseTypeInfo?)new ErrorBaseTypeInfo(
                typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                typeSymbol.TypeKind == TypeKind.Interface,
                messageProperty is not null
            ),
            Diagnostic: null
        );
    }
);

var errorTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
    ErrorAttributeName,
    predicate: (node, _) => node is RecordDeclarationSyntax,
    transform: (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol
);

var combined = errorTypes.Combine(baseTypeInfo);

context.RegisterSourceOutput(
    baseTypeInfo,
    static (context, result) =>
    {
        if (result.Diagnostic is not null)
            context.ReportDiagnostic(result.Diagnostic);
    }
);

context.RegisterSourceOutput(combined, (context, pair) =>
{
    var (errorType, baseType) = pair;
    GenerateExtensionsForSucessResults(context, errorType, baseType.Info);
});
```

- [ ] **Step 2: Update the `GenerateExtensionsForSucessResults` signature**

Change the method signature to accept the base type info:

```csharp
private void GenerateExtensionsForSucessResults(
    SourceProductionContext context,
    INamedTypeSymbol errorType,
    ErrorBaseTypeInfo? baseTypeInfo
)
```

Do not change the method body yet — pass `null` handling through without behavioral changes.

- [ ] **Step 3: Verify all existing tests still pass**

Run: `dotnet test Source/Base16.ErrGen.Tests/`
Expected: All tests PASS (regression check)

- [ ] **Step 4: Commit**

```
git add Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs
git commit -m "Add CompilationProvider resolution pipeline for ErrorBaseType"
```

---

## Task 5: Emit base type in type declaration (interface on record class)

**Files:**
- Modify: `Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs:91-99`
- Modify: `Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void BaseType_Interface_On_Record_Class()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public interface IMyError
        {
            String Message { get; }
        }

        [assembly: ErrorBaseType(typeof(IMyError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record MyError"
    );
    Assert.NotNull(errorSource);
    Assert.Contains("public partial record MyError : global::TestNamespace.IMyError", errorSource);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Interface_On_Record_Class"`
Expected: FAIL — generated code does not contain `: TestNamespace.IMyError`

- [ ] **Step 3: Implement base type in type declaration**

In `GenerateExtensionsForSucessResults`, replace the type declaration block:

```csharp
// Before:
cb.AppendLines(
    $"""
    using System;

    namespace {errorType.ContainingNamespace.ToDisplayString()};

    {accessability} partial {recordType} {errorType.Name}
    """
);

// After:
var baseTypeSuffix = "";
if (baseTypeInfo is not null)
{
    if (!baseTypeInfo.IsInterface && isStruct)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                ErrorDiagnostics.StructCannotInheritClass,
                errorType.Locations.FirstOrDefault(),
                errorType.Name,
                baseTypeInfo.FullyQualifiedName
            )
        );
        return;
    }
    baseTypeSuffix = $" : {baseTypeInfo.FullyQualifiedName}";
}

cb.AppendLines(
    $"""
    using System;

    namespace {errorType.ContainingNamespace.ToDisplayString()};

    {accessability} partial {recordType} {errorType.Name}{baseTypeSuffix}
    """
);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Interface_On_Record_Class"`
Expected: PASS

- [ ] **Step 5: Run all tests to check for regressions**

Run: `dotnet test Source/Base16.ErrGen.Tests/`
Expected: All PASS

- [ ] **Step 6: Commit**

```
git add Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs
git commit -m "Emit base type inheritance clause in generated type declarations"
```

---

## Task 6: Interface on record struct + class on record class

**Files:**
- Modify: `Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs`

- [ ] **Step 1: Write test for interface on record struct**

```csharp
[Fact]
public void BaseType_Interface_On_Record_Struct()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public interface IMyError { }

        [assembly: ErrorBaseType(typeof(IMyError))]

        [Error("Something went wrong")]
        public partial record struct MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record struct MyError"
    );
    Assert.NotNull(errorSource);
    Assert.Contains("public partial record struct MyError : global::TestNamespace.IMyError", errorSource);
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Interface_On_Record_Struct"`
Expected: PASS (should already work from Task 5 implementation)

- [ ] **Step 3: Write test for class on record class**

```csharp
[Fact]
public void BaseType_Class_On_Record_Class()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public class MyBaseError { }

        [assembly: ErrorBaseType(typeof(MyBaseError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record MyError"
    );
    Assert.NotNull(errorSource);
    Assert.Contains("public partial record MyError : global::TestNamespace.MyBaseError", errorSource);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Class_On_Record_Class"`
Expected: PASS

- [ ] **Step 5: Commit**

```
git add Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs
git commit -m "Add tests for interface on struct and class on record class"
```

---

## Task 7: Skip Message when base type has it

**Files:**
- Modify: `Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs:111`
- Modify: `Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs`

- [ ] **Step 1: Write the failing test — interface with Message**

```csharp
[Fact]
public void BaseType_Interface_With_Message_Skips_Message_Generation()
{
    // Arrange
    var source = """
        using Base16.ErrGen;
        using System;

        namespace TestNamespace;

        public interface IMyError
        {
            String Message { get; }
        }

        [assembly: ErrorBaseType(typeof(IMyError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record MyError"
    );
    Assert.NotNull(errorSource);
    Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Interface_With_Message_Skips_Message_Generation"`
Expected: FAIL — Message property is still generated

- [ ] **Step 3: Implement conditional Message skip**

In `GenerateExtensionsForSucessResults`, replace the Message line:

```csharp
// Before:
cb.AppendLine("public String Message { get; private set; } = default!;");

// After:
var skipMessage = baseTypeInfo?.HasMessageProperty == true;
if (!skipMessage)
    cb.AppendLine("public String Message { get; private set; } = default!;");
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Interface_With_Message_Skips_Message_Generation"`
Expected: PASS

- [ ] **Step 5: Write test — interface without Message still generates it**

```csharp
[Fact]
public void BaseType_Interface_Without_Message_Generates_Message()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public interface IMyError { }

        [assembly: ErrorBaseType(typeof(IMyError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record MyError"
    );
    Assert.NotNull(errorSource);
    Assert.Contains("public String Message { get; private set; }", errorSource);
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Interface_Without_Message_Generates_Message"`
Expected: PASS

- [ ] **Step 7: Write test — class with Message skips generation**

```csharp
[Fact]
public void BaseType_Class_With_Message_Skips_Message_Generation()
{
    // Arrange
    var source = """
        using Base16.ErrGen;
        using System;

        namespace TestNamespace;

        public class MyBaseError
        {
            public String Message { get; set; } = default!;
        }

        [assembly: ErrorBaseType(typeof(MyBaseError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record MyError"
    );
    Assert.NotNull(errorSource);
    Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
}
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Class_With_Message_Skips_Message_Generation"`
Expected: PASS

- [ ] **Step 9: Run all tests**

Run: `dotnet test Source/Base16.ErrGen.Tests/`
Expected: All PASS

- [ ] **Step 10: Commit**

```
git add Source/Base16.ErrGen/ErrorTypes/ErrorTypesGenerator.cs Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs
git commit -m "Skip Message property generation when base type declares it"
```

---

## Task 8: Diagnostic tests (ERR001-ERR005)

**Files:**
- Modify: `Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs`

- [ ] **Step 1: Write test — ERR002 struct cannot inherit class**

```csharp
[Fact]
public void BaseType_Class_On_Record_Struct_Emits_ERR002()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public class MyBaseError { }

        [assembly: ErrorBaseType(typeof(MyBaseError))]

        [Error("Something went wrong")]
        public partial record struct MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Contains(diagnostics, d => d.Id == "ERR002");
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Class_On_Record_Struct_Emits_ERR002"`
Expected: PASS (already implemented in Task 5)

- [ ] **Step 3: Write test — ERR001 invalid type kind (enum, not class/interface)**

```csharp
[Fact]
public void BaseType_Enum_Type_Emits_ERR001()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public enum MyEnum { A, B }

        [assembly: ErrorBaseType(typeof(MyEnum))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Contains(diagnostics, d => d.Id == "ERR001");
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Enum_Type_Emits_ERR001"`
Expected: PASS

- [ ] **Step 5: Write test — ERR003 abstract class**

```csharp
[Fact]
public void BaseType_Abstract_Class_Emits_ERR003()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public abstract class MyBaseError { }

        [assembly: ErrorBaseType(typeof(MyBaseError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Contains(diagnostics, d => d.Id == "ERR003");
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Abstract_Class_Emits_ERR003"`
Expected: PASS

- [ ] **Step 7: Write test — ERR004 generic type**

```csharp
[Fact]
public void BaseType_Generic_Type_Emits_ERR004()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        public interface IMyError<T> { }

        [assembly: ErrorBaseType(typeof(IMyError<string>))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Contains(diagnostics, d => d.Id == "ERR004");
}
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_Generic_Type_Emits_ERR004"`
Expected: PASS

- [ ] **Step 9: Write test — ERR005 non-String Message**

```csharp
[Fact]
public void BaseType_NonString_Message_Emits_ERR005()
{
    // Arrange
    var source = """
        using Base16.ErrGen;
        using System;

        namespace TestNamespace;

        public interface IMyError
        {
            Int32 Message { get; }
        }

        [assembly: ErrorBaseType(typeof(IMyError))]

        [Error("Something went wrong")]
        public partial record MyError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Contains(diagnostics, d => d.Id == "ERR005");
}
```

- [ ] **Step 10: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_NonString_Message_Emits_ERR005"`
Expected: PASS

- [ ] **Step 11: Commit**

```
git add Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs
git commit -m "Add diagnostic tests for ERR001-ERR005"
```

---

## Task 9: Regression test and different namespace test

**Files:**
- Modify: `Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs`

- [ ] **Step 1: Write test — no attribute preserves existing behavior**

```csharp
[Fact]
public void No_BaseType_Attribute_Preserves_Existing_Behavior()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace TestNamespace;

        [Error("User '{Name:String}' was not found")]
        public partial record UserNotFoundError;
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record UserNotFoundError"
    );
    Assert.NotNull(errorSource);
    Assert.Contains("public partial record UserNotFoundError\n", errorSource);
    Assert.Contains("public String Message { get; private set; }", errorSource);
    Assert.Contains("public static UserNotFoundError FromName(String name)", errorSource);
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "No_BaseType_Attribute_Preserves_Existing_Behavior"`
Expected: PASS

- [ ] **Step 3: Write test — base type in different namespace uses fully qualified name**

```csharp
[Fact]
public void BaseType_In_Different_Namespace_Uses_Fully_Qualified_Name()
{
    // Arrange
    var source = """
        using Base16.ErrGen;

        namespace Other.Namespace
        {
            public interface IMyError { }
        }

        [assembly: ErrorBaseType(typeof(Other.Namespace.IMyError))]

        namespace TestNamespace
        {
            [Error("Something went wrong")]
            public partial record MyError;
        }
        """;

    // Act
    var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

    // Assert
    Assert.Empty(diagnostics);
    var errorSource = GeneratorTestHelper.FindGeneratedSource(
        generatedSources,
        "partial record MyError"
    );
    Assert.NotNull(errorSource);
    Assert.Contains("global::Other.Namespace.IMyError", errorSource);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Source/Base16.ErrGen.Tests/ --filter "BaseType_In_Different_Namespace_Uses_Fully_Qualified_Name"`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test Source/Base16.ErrGen.Tests/`
Expected: All PASS

- [ ] **Step 6: Commit**

```
git add Source/Base16.ErrGen.Tests/ErrorTypes/Test_ErrorTypesGenerator.cs
git commit -m "Add regression and namespace tests for ErrorBaseType"
```
