using Xunit;

namespace Base16.ErrGen.Tests.ErrorTypes;

public class Test_ErrorTypesGenerator
{
    [Fact]
    public void Generates_ErrorAttribute()
    {
        // Arrange
        var source = "";

        // Act
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        var attributeSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "class ErrorAttribute"
        );
        Assert.NotNull(attributeSource);
        Assert.Contains("AllowMultiple = true", attributeSource);
    }

    [Fact]
    public void Generates_Factory_Method_For_Single_Untyped_Argument()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("User {Name} was not found")]
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
        Assert.Contains("public static UserNotFoundError FromName(Object? name)", errorSource);
        Assert.Contains("public String Message { get; private set; }", errorSource);
    }

    [Fact]
    public void Generates_Factory_Method_For_Single_Typed_Argument()
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
        Assert.Contains("public static UserNotFoundError FromName(String name)", errorSource);
    }

    [Fact]
    public void Generates_Factory_Method_For_Multiple_Arguments()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("{Method:String} {Path:String} returned {StatusCode:Int32}")]
            public partial record HttpError;
            """;

        // Act
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record HttpError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains(
            "public static HttpError FromMethodAndPathAndStatusCode(String method, String path, Int32 statusCode)",
            errorSource
        );
    }

    [Fact]
    public void Generates_Multiple_Factory_Methods_For_Multiple_Attributes()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("User '{Name:String}' was not found")]
            [Error("User with ID {Id:Int32} was not found")]
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
        Assert.Contains("public static UserNotFoundError FromName(String name)", errorSource);
        Assert.Contains("public static UserNotFoundError FromId(Int32 id)", errorSource);
    }

    [Fact]
    public void Generates_Correct_Namespace()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace My.Custom.Namespace;

            [Error("Something went wrong")]
            public partial record CustomError;
            """;

        // Act
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record CustomError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("namespace My.Custom.Namespace;", errorSource);
    }

    [Fact]
    public void Respects_Internal_Access_Modifier()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Internal error")]
            internal partial record InternalError;
            """;

        // Act
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record InternalError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("internal partial record InternalError", errorSource);
    }

    [Fact]
    public void Generates_String_Concat_With_Literal_And_Argument_Parts()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Hello {Name:String}!")]
            public partial record GreetingError;
            """;

        // Act
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record GreetingError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("String.Concat(", errorSource);
        Assert.Contains("\"Hello \"", errorSource);
        Assert.Contains("name", errorSource);
        Assert.Contains("\"!\"", errorSource);
    }

    [Fact]
    public void Generates_String_Concat_With_Literal_And_Argument_Parts_For_Struct()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Hello {Name:String}!")]
            public partial record struct GreetingError;
            """;

        // Act
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record struct GreetingError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("String.Concat(", errorSource);
        Assert.Contains("\"Hello \"", errorSource);
        Assert.Contains("name", errorSource);
        Assert.Contains("\"!\"", errorSource);
    }
}
