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
            "public static HttpError FromMethodPathAndStatusCode(String method, String path, Int32 statusCode)",
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
    public void Generates_ErrorBaseTypeAttribute()
    {
        var source = "";
        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);
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

    [Fact]
    public void BaseType_Interface_On_Record_Class()
    {
        var source = """
            using System;
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError
            {
                String Message { get; }
            }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record MyError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains(
            "public partial record MyError : global::TestNamespace.IMyError",
            errorSource
        );
    }

    [Fact]
    public void BaseType_Interface_On_Record_Struct()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError { }

            [Error("Something went wrong")]
            public partial record struct MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record struct MyError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains(
            "public partial record struct MyError : global::TestNamespace.IMyError",
            errorSource
        );
    }

    [Fact]
    public void BaseType_Class_On_Record_Class()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyBaseError))]

            namespace TestNamespace;

            public class MyBaseError { }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record MyError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains(
            "public partial record MyError : global::TestNamespace.MyBaseError",
            errorSource
        );
    }

    [Fact]
    public void BaseType_Interface_With_Message_Skips_Message_Generation()
    {
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError
            {
                String Message { get; }
            }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record MyError"
        );
        Assert.NotNull(errorSource);
        Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
    }

    [Fact]
    public void BaseType_Interface_Without_Message_Generates_Message()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError { }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record MyError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("public String Message { get; private set; }", errorSource);
    }

    [Fact]
    public void BaseType_Class_With_Message_Skips_Message_Generation()
    {
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyBaseError))]

            namespace TestNamespace;

            public class MyBaseError
            {
                public String Message { get; set; } = default!;
            }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record MyError"
        );
        Assert.NotNull(errorSource);
        Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
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

    [Fact]
    public void BaseType_Class_On_Record_Struct_Emits_ERR002()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyBaseError))]

            namespace TestNamespace;

            public class MyBaseError { }

            [Error("Something went wrong")]
            public partial record struct MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ERR002");
    }

    [Fact]
    public void BaseType_Enum_Type_Emits_ERR001()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyEnum))]

            namespace TestNamespace;

            public enum MyEnum { A, B }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ERR001");
    }

    [Fact]
    public void BaseType_Generic_Type_Emits_ERR003()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError<string>))]

            namespace TestNamespace;

            public interface IMyError<T> { }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ERR003");
    }

    [Fact]
    public void BaseType_NonString_Message_Emits_ERR004()
    {
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError
            {
                Int32 Message { get; }
            }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ERR004");
    }

    [Fact]
    public void No_BaseType_Attribute_Preserves_Existing_Behavior()
    {
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("User '{Name:String}' was not found")]
            public partial record UserNotFoundError;
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record UserNotFoundError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("public String Message { get; private set; }", errorSource);
        Assert.Contains("public static UserNotFoundError FromName(String name)", errorSource);
    }

    [Fact]
    public void BaseType_In_Different_Namespace_Uses_Fully_Qualified_Name()
    {
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(Other.Namespace.IMyError))]

            namespace Other.Namespace
            {
                public interface IMyError { }
            }

            namespace TestNamespace
            {
                [Error("Something went wrong")]
                public partial record MyError;
            }
            """;

        var (diagnostics, generatedSources) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics);
        var errorSource = GeneratorTestHelper.FindGeneratedSource(
            generatedSources,
            "partial record MyError"
        );
        Assert.NotNull(errorSource);
        Assert.Contains("global::Other.Namespace.IMyError", errorSource);
    }
}
