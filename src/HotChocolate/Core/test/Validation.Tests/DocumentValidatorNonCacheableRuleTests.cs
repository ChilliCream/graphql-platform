using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation;

public class DocumentValidatorNonCacheableRuleTests
{
    [Fact]
    public void Validate_Should_NotRunNonCacheableRules_When_DocumentHasNoIntrospectionField()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var pool = new CountingContextPool();
        var validator = CreateBuilder(pool).Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(1, pool.Rented);
    }

    [Fact]
    public void Validate_Should_NotRunNonCacheableRules_When_DocumentHasNoQueryOperation()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var pool = new CountingContextPool();
        var validator = CreateBuilder(pool).Build();
        var document = Utf8GraphQLParser.Parse("mutation { fieldB }");
        validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(1, pool.Rented);
    }

    [Fact]
    public void Validate_Should_ReportIntrospectionNotAllowed_When_DocumentWasValidForAnAllowedRequest()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var validator = CreateBuilder(new CountingContextPool()).Build();
        var document = Utf8GraphQLParser.Parse("{ __schema { queryType { name } } }");
        var allowed = new FeatureCollection();
        allowed.Set(new IntrospectionRequestOverrides(IsAllowed: true));
        var allowedResult = validator.Validate(schema, default, document, allowed);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(allowedResult.HasErrors);
        Assert.Equal(
            "Introspection is not allowed for the current request.",
            Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Validate_Should_RunNonCacheableRules_When_IntrospectionWasAllowedForTheFirstRequest()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var pool = new CountingContextPool();
        var validator = CreateBuilder(pool).Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        var allowed = new FeatureCollection();
        allowed.Set(new IntrospectionRequestOverrides(IsAllowed: true));
        validator.Validate(schema, default, document, allowed);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(2, pool.Rented);
    }

    [Fact]
    public void Validate_Should_RunNonCacheableRules_When_ARuleDoesNotReportARequestIndependentResult()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var rule = new CountingRule();
        var validator = CreateBuilder(new CountingContextPool()).AddRule((_, _) => rule).Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(2, rule.Calls);
    }

    [Fact]
    public void Validate_Should_RunLaterNonCacheableRules_When_IntrospectionRuleResultIsRequestIndependent()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var validator = CreateBuilder(new CountingContextPool())
            .AddMaxExecutionDepthRule(5, allowRequestOverrides: true)
            .Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        var lowerLimit = new FeatureCollection();
        lowerLimit.Set(new MaxExecutionDepthRequestOverrides(MaxAllowedDepth: 1));
        var firstResult = validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(schema, default, document, lowerLimit, onlyNonCacheable: true);

        // assert
        Assert.False(firstResult.HasErrors);
        Assert.Equal(
            "The GraphQL document has an execution depth of 2 which exceeds the max allowed execution depth of 1.",
            Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Validate_Should_NotRunNonCacheableRules_When_FragmentHasNoIntrospectionField()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var pool = new CountingContextPool();
        var validator = CreateBuilder(pool).Build();
        var document = Utf8GraphQLParser.Parse("{ ...a } fragment a on Query { dog { name } }");
        validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(1, pool.Rented);
    }

    [Fact]
    public void Validate_Should_ReportIntrospectionNotAllowed_When_FragmentWasValidForAnAllowedRequest()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var validator = CreateBuilder(new CountingContextPool()).Build();
        var document = Utf8GraphQLParser.Parse(
            "{ ...a } fragment a on Query { __schema { queryType { name } } }");
        var allowed = new FeatureCollection();
        allowed.Set(new IntrospectionRequestOverrides(IsAllowed: true));
        var allowedResult = validator.Validate(schema, default, document, allowed);

        // act
        var result = validator.Validate(schema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(allowedResult.HasErrors);
        Assert.Equal(
            "Introspection is not allowed for the current request.",
            Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Validate_Should_NotKeepSchemaAlive_When_DocumentIsStillReferenced()
    {
        // arrange
        var validator = CreateBuilder(new CountingContextPool()).Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        var schema = ValidateWithNewSchema(validator, document);

        // act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // assert
        Assert.False(schema.TryGetTarget(out _));
        GC.KeepAlive(document);
        GC.KeepAlive(validator);
    }

    [Fact]
    public void Validate_Should_RunNonCacheableRules_When_SchemaIsAnotherInstance()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var otherSchema = ValidationUtils.CreateSchema();
        var pool = new CountingContextPool();
        var validator = CreateBuilder(pool).Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(otherSchema, default, document, onlyNonCacheable: true);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(2, pool.Rented);
    }

    [Fact]
    public void Validate_Should_RunAllRules_When_DocumentIsValidatedInFullAgain()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var pool = new CountingContextPool();
        var validator = CreateBuilder(pool).Build();
        var document = Utf8GraphQLParser.Parse("{ dog { name } }");
        validator.Validate(schema, default, document);

        // act
        var result = validator.Validate(schema, default, document);

        // assert
        Assert.False(result.HasErrors);
        Assert.Equal(2, pool.Rented);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ISchemaDefinition> ValidateWithNewSchema(
        DocumentValidator validator,
        DocumentNode document)
    {
        var schema = ValidationUtils.CreateSchema();
        validator.Validate(schema, default, document);
        validator.Validate(schema, default, document, onlyNonCacheable: true);
        return new WeakReference<ISchemaDefinition>(schema);
    }

    private static DocumentValidatorBuilder CreateBuilder(CountingContextPool pool)
    {
        var services = new ServiceCollection()
            .AddSingleton<ObjectPool<DocumentValidatorContext>>(pool)
            .BuildServiceProvider();

        return DocumentValidatorBuilder.New()
            .SetServices(services)
            .AddIntrospectionAllowedRule()
            .ModifyOptions(o => o.DisableIntrospection = true);
    }

    private sealed class CountingContextPool : ObjectPool<DocumentValidatorContext>
    {
        private readonly DocumentValidatorContextPool _pool = new();

        public int Rented { get; private set; }

        public override DocumentValidatorContext Get()
        {
            Rented++;
            return _pool.Get();
        }

        public override void Return(DocumentValidatorContext obj) => _pool.Return(obj);
    }

    private sealed class CountingRule : IDocumentValidatorRule
    {
        public int Calls { get; private set; }

        public ushort Priority => ushort.MaxValue;

        public bool IsCacheable => false;

        public void Validate(DocumentValidatorContext context, DocumentNode document) => Calls++;
    }
}
