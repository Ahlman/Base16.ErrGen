using Base16.ErrGen.ErrorTypes;
using Xunit;

namespace Base16.ErrGen.Tests.ErrorTypes;

public class Test_StringTemplate
{
    [Fact]
    public void Can_Match_Untyped_Argument()
    {
        // Arrange
        var input = "Hello {Name}!";

        // Act
        var success = StringTemplate.TryParse(input, out var template, out var error);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(
            [
                new StringTemplateLiteralPart("Hello "),
                new StringTemplateArgumentPart("Name", null),
                new StringTemplateLiteralPart("!"),
            ],
            template!.Parts
        );
    }

    [Fact]
    public void Can_Match_String_Argument()
    {
        // Arrange
        var input = "Hello {Name:String}!";

        // Act
        var success = StringTemplate.TryParse(input, out var template, out var error);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(
            [
                new StringTemplateLiteralPart("Hello "),
                new StringTemplateArgumentPart("Name", "String"),
                new StringTemplateLiteralPart("!"),
            ],
            template!.Parts
        );
    }

    [Fact]
    public void Parse_PlainText_ReturnsSingleLiteral()
    {
        // Arrange
        var input = "hello world";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        var part = Assert.Single(result!.Parts);
        var literal = Assert.IsType<StringTemplateLiteralPart>(part);
        Assert.Equal("hello world", literal.Value);
    }

    [Fact]
    public void Parse_SingleArgument_ReturnsSingleArgument()
    {
        // Arrange
        var input = "{Name}";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        var part = Assert.Single(result!.Parts);
        var arg = Assert.IsType<StringTemplateArgumentPart>(part);
        Assert.Equal("Name", arg.Name);
        Assert.Null(arg.Type);
    }

    [Fact]
    public void Parse_ArgumentWithType_ParsesNameAndType()
    {
        // Arrange
        var input = "{Name:String}";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        var part = Assert.Single(result!.Parts);
        var arg = Assert.IsType<StringTemplateArgumentPart>(part);
        Assert.Equal("Name", arg.Name);
        Assert.Equal("String", arg.Type);
    }

    [Fact]
    public void Parse_MixedTemplate_ReturnsLiteralsAndArguments()
    {
        // Arrange
        var input = "Hello {Name}, you are {Age} years old";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        Assert.Equal(5, result!.Parts.Count);

        var lit0 = Assert.IsType<StringTemplateLiteralPart>(result.Parts[0]);
        Assert.Equal("Hello ", lit0.Value);

        var arg1 = Assert.IsType<StringTemplateArgumentPart>(result.Parts[1]);
        Assert.Equal("Name", arg1.Name);

        var lit2 = Assert.IsType<StringTemplateLiteralPart>(result.Parts[2]);
        Assert.Equal(", you are ", lit2.Value);

        var arg3 = Assert.IsType<StringTemplateArgumentPart>(result.Parts[3]);
        Assert.Equal("Age", arg3.Name);

        var lit4 = Assert.IsType<StringTemplateLiteralPart>(result.Parts[4]);
        Assert.Equal(" years old", lit4.Value);
    }

    [Fact]
    public void Parse_AdjacentArguments_ParsesBothSeparately()
    {
        // Arrange
        var input = "{First}{Second}";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        Assert.Equal(2, result!.Parts.Count);

        var arg0 = Assert.IsType<StringTemplateArgumentPart>(result.Parts[0]);
        Assert.Equal("First", arg0.Name);

        var arg1 = Assert.IsType<StringTemplateArgumentPart>(result.Parts[1]);
        Assert.Equal("Second", arg1.Name);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNoParts()
    {
        // Arrange
        var input = "";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        Assert.Empty(result!.Parts);
    }

    [Fact]
    public void Parse_UnclosedBrace_ReturnsError()
    {
        // Arrange
        var input = "hello {world";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out var error);

        // Assert
        Assert.False(success);
        Assert.Null(result);
        Assert.Contains("unclosed", error);
    }

    [Fact]
    public void Parse_EmptyPlaceholder_ReturnsError()
    {
        // Arrange
        var input = "hello {}";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out var error);

        // Assert
        Assert.False(success);
        Assert.Null(result);
        Assert.Contains("empty placeholder", error);
    }

    [Fact]
    public void Parse_MissingName_ReturnsError()
    {
        // Arrange
        var input = "hello {:String}";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out var error);

        // Assert
        Assert.False(success);
        Assert.Null(result);
        Assert.Contains("missing a name", error);
    }

    [Fact]
    public void Parse_MissingType_ReturnsError()
    {
        // Arrange
        var input = "hello {Name:}";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out var error);

        // Assert
        Assert.False(success);
        Assert.Null(result);
        Assert.Contains("missing a type", error);
    }

    [Fact]
    public void Parse_TrailingText_IncludedAsLiteral()
    {
        // Arrange
        var input = "{Name}!";

        // Act
        var success = StringTemplate.TryParse(input, out var result, out _);

        // Assert
        Assert.True(success);
        Assert.Equal(2, result!.Parts.Count);

        var arg0 = Assert.IsType<StringTemplateArgumentPart>(result.Parts[0]);
        Assert.Equal("Name", arg0.Name);

        var lit1 = Assert.IsType<StringTemplateLiteralPart>(result.Parts[1]);
        Assert.Equal("!", lit1.Value);
    }
}
