using System.ComponentModel.DataAnnotations;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

public class RequiredAttributeTests
{
    [Fact]
    public void Infer_RequiredAttribute_As_NonNull()
    {
        SchemaBuilder.New()
            .AddQueryType<Foo>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Ignore_RequiredAttribute()
    {
        // arrange
        var inspector = new DefaultTypeInspector(ignoreRequiredAttribute: true);
        var services = new DictionaryServiceProvider(typeof(ITypeInspector), inspector);

        // act & assert
        SchemaBuilder.New()
            .AddQueryType<Foo>()
            .AddServices(services)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    public class Foo
    {
        [Required]
        public string? SomeString { get; }

        public string? AddFoo([Required] Bar? input) => throw new NotSupportedException();
    }

    public class Bar
    {
        [Required]
        public string? Foo { get; set; }
    }
}
