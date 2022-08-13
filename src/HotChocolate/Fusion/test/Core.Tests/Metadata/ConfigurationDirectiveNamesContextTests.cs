using CookieCrumble;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

public class ConfigurationDirectiveNamesContextTests
{
    [Fact]
    public void NewContext_DefaultDirectiveNames()
    {
        // act
        var context = ConfigurationDirectiveNamesContext.Create();

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""variable"",
                    ""FetchDirective"": ""fetch"",
                    ""BindDirective"": ""bind"",
                    ""HttpDirective"": ""httpClient"",
                    ""FusionDirective"": ""fusion""
                }");
    }

    [Fact]
    public void NewContext_DirectiveNames_With_Prefix()
    {
        // act
        var context = ConfigurationDirectiveNamesContext.Create(prefix: "def");

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""def_variable"",
                    ""FetchDirective"": ""def_fetch"",
                    ""BindDirective"": ""def_bind"",
                    ""HttpDirective"": ""def_httpClient"",
                    ""FusionDirective"": ""fusion""
                }");
    }

    [Fact]
    public void NewContext_DirectiveNames_With_Prefix_PrefixSelf()
    {
        // act
        var context = ConfigurationDirectiveNamesContext.Create(prefix: "def", prefixSelf: true);

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""def_variable"",
                    ""FetchDirective"": ""def_fetch"",
                    ""BindDirective"": ""def_bind"",
                    ""HttpDirective"": ""def_httpClient"",
                    ""FusionDirective"": ""def_fusion""
                }");
    }

    [Fact]
    public void From_Document_No_Fusion_Directive()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(@"schema {  }");

        // act
        var context = ConfigurationDirectiveNamesContext.From(document);

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""variable"",
                    ""FetchDirective"": ""fetch"",
                    ""BindDirective"": ""bind"",
                    ""HttpDirective"": ""httpClient"",
                    ""FusionDirective"": ""fusion""
                }");
    }

    [Fact]
    public void From_Document_With_Fusion_Directive_No_Prefix()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(@"schema @fusion(version: 1) {  }");

        // act
        var context = ConfigurationDirectiveNamesContext.From(document);

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""variable"",
                    ""FetchDirective"": ""fetch"",
                    ""BindDirective"": ""bind"",
                    ""HttpDirective"": ""httpClient"",
                    ""FusionDirective"": ""fusion""
                }");
    }

    [Fact]
    public void From_Document_With_Fusion_Directive_With_Prefix()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(@"schema @fusion(prefix: ""abc"") {  }");

        // act
        var context = ConfigurationDirectiveNamesContext.From(document);

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""abc_variable"",
                    ""FetchDirective"": ""abc_fetch"",
                    ""BindDirective"": ""abc_bind"",
                    ""HttpDirective"": ""abc_httpClient"",
                    ""FusionDirective"": ""fusion""
                }");
    }

    [Fact]
    public void From_Document_With_Fusion_Directive_With_Prefix_PrefixSelf()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            @"schema @abc_fusion(prefix: ""abc"", prefixSelf: true) {  }");

        // act
        var context = ConfigurationDirectiveNamesContext.From(document);

        // assert
        Snapshot
            .Create()
            .Add(context)
            .MatchInline(
                @"{
                    ""VariableDirective"": ""abc_variable"",
                    ""FetchDirective"": ""abc_fetch"",
                    ""BindDirective"": ""abc_bind"",
                    ""HttpDirective"": ""abc_httpClient"",
                    ""FusionDirective"": ""abc_fusion""
                }");
    }
}
