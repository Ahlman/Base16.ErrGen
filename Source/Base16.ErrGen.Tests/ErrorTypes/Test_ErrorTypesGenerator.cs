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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var attributeSource = result.FindSource("ErrorAttribute.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("UserNotFoundError.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("UserNotFoundError.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("HttpError.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("UserNotFoundError.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("CustomError.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("InternalError.g.cs");
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("GreetingError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("String.Concat(", errorSource);
        Assert.Contains("\"Hello \"", errorSource);
        Assert.Contains("name", errorSource);
        Assert.Contains("\"!\"", errorSource);
    }

    [Fact]
    public void Generates_ErrorBaseTypeAttribute()
    {
        // Arrange
        var source = "";

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var attributeSource = result.FindSource("ErrorBaseTypeAttribute.g.cs");
        Assert.NotNull(attributeSource);
        Assert.Contains("System.AttributeTargets.Assembly", attributeSource);
        Assert.Contains("AllowMultiple = false", attributeSource);
        Assert.Contains("System.Type", attributeSource);
    }

    [Fact]
    public void BaseType_Interface_On_Record_Class()
    {
        // Arrange
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

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains(
            "public partial record MyError : global::TestNamespace.IMyError",
            errorSource
        );
    }

    [Fact]
    public void BaseType_Interface_On_Record_Struct()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError { }

            [Error("Something went wrong")]
            public partial record struct MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains(
            "public partial record struct MyError : global::TestNamespace.IMyError",
            errorSource
        );
    }

    [Fact]
    public void BaseType_NonRecord_Class_Emits_ERR008()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyBaseError))]

            namespace TestNamespace;

            public class MyBaseError { }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR008");
    }

    [Fact]
    public void BaseType_Interface_With_Message_Still_Generates_Message()
    {
        // Arrange
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

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("public String Message { get; private set; }", errorSource);
    }

    [Fact]
    public void BaseType_Interface_Without_Message_Generates_Message()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError))]

            namespace TestNamespace;

            public interface IMyError { }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("public String Message { get; private set; }", errorSource);
    }

    [Fact]
    public void BaseType_NonRecord_Class_With_Message_Emits_ERR008()
    {
        // Arrange
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

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR008");
    }

    [Fact]
    public void BaseType_Abstract_Record_With_Positional_Message_Generates_Base_Constructor_Call()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.Error))]

            namespace TestNamespace;

            public abstract record Error(String Message);

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains(": global::TestNamespace.Error", errorSource);
        Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
        Assert.Contains("base(message)", errorSource);
    }

    [Fact]
    public void BaseType_Record_With_Non_Message_Ctor_Params_Still_Sets_Message()
    {
        // Arrange — base record has ctor params but NOT Message,
        // so the generated type must set Message via object initializer
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.TracedError))]

            namespace TestNamespace;

            public abstract record TracedError(Guid TraceId);

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("Message = message,", errorSource);
        Assert.Contains("base(traceId)", errorSource);
    }

    [Fact]
    public void BaseType_Abstract_Record_With_Extra_Ctor_Params_Adds_To_Factory_And_Constructor()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.Error))]

            namespace TestNamespace;

            public abstract record Error(String Message, Guid UserId);

            [Error("The answer {Answer:Int32} was incorrect.")]
            public sealed partial record InvalidAnswer;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("InvalidAnswer.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains(
            "InvalidAnswer(string message, global::System.Guid userId) : base(message, userId)",
            errorSource
        );
        Assert.Contains("FromAnswer(global::System.Guid userId, Int32 answer)", errorSource);
        Assert.Contains("new InvalidAnswer(message, userId)", errorSource);
        Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
    }

    [Fact]
    public void Explicit_BaseType_Derived_Record_With_Inherited_Message_Skips_Message()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;
            using System;

            namespace TestNamespace;

            public abstract record Error(String Message);
            public abstract record TracedError(String Message, Guid TraceId) : Error(Message);

            [Error("Auth failed for '{UserId:String}'")]
            public partial record AuthError : TracedError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("AuthError.g.cs");
        Assert.NotNull(errorSource);
        Assert.DoesNotContain("public String Message { get; private set; }", errorSource);
        Assert.Contains("base(message, traceId)", errorSource);
    }

    [Fact]
    public void Explicit_BaseType_On_Record_Overrides_Assembly_ErrorBaseType()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.DefaultError))]

            namespace TestNamespace;

            public abstract record DefaultError(String Message);
            public abstract record SpecialError(String Message, Guid TraceId);

            [Error("Default error")]
            public partial record UsesDefault;

            [Error("Special error")]
            public partial record UsesExplicit : SpecialError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();

        var defaultSource = result.FindSource("UsesDefault.g.cs");
        Assert.NotNull(defaultSource);
        Assert.Contains(": global::TestNamespace.DefaultError", defaultSource);

        var explicitSource = result.FindSource("UsesExplicit.g.cs");
        Assert.NotNull(explicitSource);
        Assert.DoesNotContain(": global::", explicitSource);
        Assert.Contains("base(message, traceId)", explicitSource);
        Assert.Contains("global::System.Guid traceId", explicitSource);
    }

    [Fact]
    public void Explicit_BaseType_Without_Assembly_ErrorBaseType()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;
            using System;

            namespace TestNamespace;

            public abstract record AppError(String Message);

            [Error("Something failed")]
            public partial record MyError : AppError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.DoesNotContain(": global::", errorSource);
        Assert.Contains("base(message)", errorSource);
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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("GreetingError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("String.Concat(", errorSource);
        Assert.Contains("\"Hello \"", errorSource);
        Assert.Contains("name", errorSource);
        Assert.Contains("\"!\"", errorSource);
    }

    [Fact]
    public void BaseType_Record_Class_On_Record_Struct_Emits_ERR002()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;
            using System;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyBaseError))]

            namespace TestNamespace;

            public abstract record MyBaseError(String Message);

            [Error("Something went wrong")]
            public partial record struct MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR002");
    }

    [Fact]
    public void BaseType_Enum_Type_Emits_ERR001()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.MyEnum))]

            namespace TestNamespace;

            public enum MyEnum { A, B }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR001");
    }

    [Fact]
    public void BaseType_Generic_Type_Emits_ERR003()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            [assembly: ErrorBaseType(typeof(TestNamespace.IMyError<string>))]

            namespace TestNamespace;

            public interface IMyError<T> { }

            [Error("Something went wrong")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR003");
    }

    [Fact]
    public void BaseType_NonString_Message_Emits_ERR004()
    {
        // Arrange
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

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR004");
    }

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
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("UserNotFoundError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("public String Message { get; private set; }", errorSource);
        Assert.Contains("public static UserNotFoundError FromName(String name)", errorSource);
    }

    [Fact]
    public void BaseType_In_Different_Namespace_Uses_Fully_Qualified_Name()
    {
        // Arrange
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

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        result.AssertNoErrors();
        var errorSource = result.FindSource("MyError.g.cs");
        Assert.NotNull(errorSource);
        Assert.Contains("global::Other.Namespace.IMyError", errorSource);
    }

    [Fact]
    public void NonPartial_Record_Emits_ERR005()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Something went wrong")]
            public record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR005");
    }

    [Fact]
    public void Invalid_Template_Unclosed_Brace_Emits_ERR006()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Hello {World")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR006");
    }

    [Fact]
    public void Invalid_Template_Empty_Placeholder_Emits_ERR006()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Hello {}")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR006");
    }

    [Fact]
    public void Invalid_Template_Missing_Type_After_Colon_Emits_ERR006()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Hello {Name:}")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR006");
    }

    [Fact]
    public void Duplicate_Factory_Method_Names_Emits_ERR007()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("First error about {Name:String}")]
            [Error("Second error about {Name:String}")]
            public partial record MyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        Assert.Contains(result.Diagnostics, d => d.Id == "ERR007");
    }
}
