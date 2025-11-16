namespace HotChocolate.Types;

public class WrongAuthorizationAttributeAnalyzerTests
{
    [Fact]
    public async Task HotChocolate_AuthorizeAttribute_On_Class_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using HotChocolate.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             [Authorize]
             public static partial class ProductQueries
             {
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task HotChocolate_AuthorizeAttribute_On_Record_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using HotChocolate.Authorization;

             namespace TestNamespace;

             [ObjectType]
             [Authorize]
             public record Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task HotChocolate_AuthorizeAttribute_On_Resolver_Method_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using HotChocolate.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             public static partial class ProductQueries
             {
                 [Authorize]
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task HotChocolate_AuthorizeAttribute_On_Resolver_Property_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using HotChocolate.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             public static partial class ProductQueries
             {
                 [Authorize]
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task HotChocolate_AllowAnonymousAttribute_On_Resolver_Method_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using HotChocolate.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             public static partial class ProductQueries
             {
                 [AllowAnonymous]
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Class_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             [Authorize]
             public static partial class ProductQueries
             {
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Record_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType]
             [Authorize]
             public record Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Resolver_Method_In_Record_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType]
             public record Product
             {
                 [Authorize]
                 public string GetName() => "Test";
             }
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Resolver_Method_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             public static partial class ProductQueries
             {
                 [Authorize]
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AllowAnonymousAttribute_On_Resolver_Method_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType<Product>]
             public static partial class ProductQueries
             {
                 [AllowAnonymous]
                 public static string GetName() => "Test";
             }

             public class Product;
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Old_ObjectType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType]
             [Authorize]
             public class Product
             {
                 public string GetName() => "Test";
             }
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Resolver_Method_In_Old_ObjectType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [ObjectType]
             public class Product
             {
                 [Authorize]
                 public string GetName() => "Test";
             }
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_RootType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [QueryType]
             [Authorize]
             public static partial class ProductQueries
             {
                 public static string GetName() => "Test";
             }
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Microsoft_AuthorizeAttribute_On_Resolver_Method_In_RootType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
             using HotChocolate.Types;
             using HotChocolate.Types.Relay;
             using Microsoft.AspNetCore.Authorization;

             namespace TestNamespace;

             [QueryType]
             public static partial class ProductQueries
             {
                 [Authorize]
                 public static string GetName() => "Test";
             }
             """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
