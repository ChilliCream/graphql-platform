using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class DescriptorContextTests
{
    [Fact]
    public void Create_With_Custom_NamingConventions()
    {
        // arrange
        var options = new SchemaOptions();
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));
        var services = new DictionaryServiceProvider(
            typeof(INamingConventions),
            namingConventions);

        // act
        var context = DescriptorContext.Create(
            options,
            services,
            new FeatureCollection(),
            new SchemaBuilder.LazySchema(),
            new AggregateTypeInterceptor());

        // assert
        Assert.Equal(namingConventions, context.Naming);
        Assert.NotNull(context.TypeInspector);
        Assert.Equal(options, context.Options);
    }

    [Fact]
    public void Create_With_Custom_NamingConventions_AsIConvention()
    {
        // arrange
        var options = new SchemaOptions();
        var naming = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));

        var namingConventionKey =new ConventionKey(typeof(INamingConventions), null);
        var conventionRegistration = new ConventionRegistration(namingConventionKey, _ => naming);

        var features = new FeatureCollection();
        var feature = features.GetOrSet<TypeSystemConventionFeature>();
        feature.Conventions = feature.Conventions.Add(namingConventionKey, [conventionRegistration]);

        // act
        var context = DescriptorContext.Create(
            options,
            EmptyServiceProvider.Instance,
            features,
            new SchemaBuilder.LazySchema(),
            new AggregateTypeInterceptor());

        // assert
        Assert.Equal(naming, context.Naming);
        Assert.NotNull(context.TypeInspector);
        Assert.Equal(options, context.Options);
    }

    [Fact]
    public void Create_With_Custom_TypeInspector()
    {
        // arrange
        var options = new SchemaOptions();
        var inspector = new DefaultTypeInspector();
        var services = new DictionaryServiceProvider(
            typeof(ITypeInspector),
            inspector);

        // act
        var context = DescriptorContext.Create(
            options,
            services,
            new FeatureCollection(),
            new SchemaBuilder.LazySchema(),
            new AggregateTypeInterceptor());

        // assert
        Assert.Equal(inspector, context.TypeInspector);
        Assert.NotNull(context.Naming);
        Assert.Equal(options, context.Options);
    }

    [Fact]
    public void Create_Without_Services()
    {
        // arrange
        // act
        var context = DescriptorContext.Create();

        // assert
        Assert.NotNull(context.Options);
        Assert.NotNull(context.Naming);
        Assert.NotNull(context.TypeInspector);
    }
}
