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
    public async Task Generates_Factory_Method_For_Single_Untyped_Argument()
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
        await VerifyGeneratedSource(result, "UserNotFoundError.g.cs");
    }

    [Fact]
    public async Task Generates_Factory_Method_For_Single_Typed_Argument()
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
        await VerifyGeneratedSource(result, "UserNotFoundError.g.cs");
    }

    [Fact]
    public async Task Generates_Factory_Method_For_Multiple_Arguments()
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
        await VerifyGeneratedSource(result, "HttpError.g.cs");
    }

    [Fact]
    public async Task Generates_Multiple_Factory_Methods_For_Multiple_Attributes()
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
        await VerifyGeneratedSource(result, "UserNotFoundError.g.cs");
    }

    [Fact]
    public async Task Generates_Correct_Namespace()
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
        await VerifyGeneratedSource(result, "CustomError.g.cs");
    }

    [Fact]
    public async Task Respects_Internal_Access_Modifier()
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
        await VerifyGeneratedSource(result, "InternalError.g.cs");
    }

    [Fact]
    public async Task Generates_String_Concat_With_Literal_And_Argument_Parts()
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
        await VerifyGeneratedSource(result, "GreetingError.g.cs");
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
    public async Task BaseType_Interface_On_Record_Class()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
    }

    [Fact]
    public async Task BaseType_Interface_On_Record_Struct()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
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
    public async Task BaseType_Interface_With_Message_Still_Generates_Message()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
    }

    [Fact]
    public async Task BaseType_Interface_Without_Message_Generates_Message()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
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
    public async Task BaseType_Abstract_Record_With_Positional_Message_Generates_Base_Constructor_Call()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
    }

    [Fact]
    public async Task BaseType_Record_With_Non_Message_Ctor_Params_Still_Sets_Message()
    {
        // Arrange
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
    }

    [Fact]
    public async Task BaseType_Abstract_Record_With_Extra_Ctor_Params_Adds_To_Factory_And_Constructor()
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
        await VerifyGeneratedSource(result, "InvalidAnswer.g.cs");
    }

    [Fact]
    public async Task Explicit_BaseType_Derived_Record_With_Inherited_Message_Skips_Message()
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
        await VerifyGeneratedSource(result, "AuthError.g.cs");
    }

    [Fact]
    public async Task Explicit_BaseType_On_Record_Overrides_Assembly_ErrorBaseType()
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
        await VerifyGeneratedSources(result, ["UsesDefault.g.cs", "UsesExplicit.g.cs"]);
    }

    [Fact]
    public async Task Explicit_BaseType_Without_Assembly_ErrorBaseType()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
    }

    [Fact]
    public async Task Generates_String_Concat_With_Literal_And_Argument_Parts_For_Struct()
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
        await VerifyGeneratedSource(result, "GreetingError.g.cs");
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
    public async Task No_BaseType_Attribute_Preserves_Existing_Behavior()
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
        await VerifyGeneratedSource(result, "UserNotFoundError.g.cs");
    }

    [Fact]
    public async Task BaseType_In_Different_Namespace_Uses_Fully_Qualified_Name()
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
        await VerifyGeneratedSource(result, "MyError.g.cs");
    }

    [Fact]
    public async Task Empty_Template_Generates_Parameterless_Factory()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("")]
            public partial record EmptyError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        await VerifyGeneratedSource(result, "EmptyError.g.cs");
    }

    [Fact]
    public async Task Whitespace_Only_Template_Generates_Parameterless_Factory()
    {
        // Arrange
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("   ")]
            public partial record WhitespaceError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        await VerifyGeneratedSource(result, "WhitespaceError.g.cs");
    }

    [Fact]
    public async Task Template_With_Duplicate_Argument_Name_Generates_Single_Property()
    {
        // Arrange — same argument appears twice in the template
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Expected {Value:String} but got {Value:String} again")]
            public partial record DuplicateArgError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        await VerifyGeneratedSource(result, "DuplicateArgError.g.cs");
    }

    [Fact]
    public async Task Template_With_Special_Characters_Escapes_Xml_In_Docs()
    {
        // Arrange — template contains characters that need XML escaping
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Expected {X:Int32} < {Y:Int32} && {X:Int32} > 0")]
            public partial record ComparisonError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        await VerifyGeneratedSource(result, "ComparisonError.g.cs");
    }

    [Fact]
    public async Task Template_With_Many_Arguments_Generates_Correct_Factory_Name()
    {
        // Arrange — 4 arguments should produce FromABCAndD
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("{A:String} {B:String} {C:String} {D:String}")]
            public partial record ManyArgsError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        await VerifyGeneratedSource(result, "ManyArgsError.g.cs");
    }

    [Fact]
    public async Task Multiple_Attributes_Sharing_Arguments_Generates_Shared_Properties()
    {
        // Arrange — both templates use Host but different additional args
        var source = """
            using Base16.ErrGen;

            namespace TestNamespace;

            [Error("Connection to '{Host:String}' timed out after {TimeoutMs:Int32}ms")]
            [Error("Connection to '{Host:String}' was refused on port {Port:Int32}")]
            public partial record ConnectionError;
            """;

        // Act
        var result = GeneratorTestHelper.RunGenerator(source);

        // Assert
        await VerifyGeneratedSource(result, "ConnectionError.g.cs");
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
