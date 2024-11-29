using HotChocolate.Types;

namespace HotChocolate.Data;

public class ProjectionAttributeTests
{
    [Fact]
    public void FirstOrDefault_Attribute()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<FirstOrDefaultQuery>()
            .AddProjections()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void SingleOrDefault_Attribute()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<SingleOrDefaultQuery>()
            .AddProjections()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FirstOrDefault_Attribute_CustomType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<FirstOrDefaultQuery>()
            .AddType<FooType>()
            .AddProjections()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void SingleOrDefault_Attribute_CustomType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<SingleOrDefaultQuery>()
            .AddType<FooType>()
            .AddProjections()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Name("Renamed");
            descriptor.Field(x => x.Bar).Name("renamed");
        }
    }

    public class FirstOrDefaultQuery
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Foo> GetFooQueryable() => throw new NotImplementedException();

        [UseFirstOrDefault]
        [UseProjection]
        public IEnumerable<Foo> GetFooEnumerable() => throw new NotImplementedException();
    }

    public class SingleOrDefaultQuery
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Foo> GetFooQueryable() => throw new NotImplementedException();

        [UseFirstOrDefault]
        [UseProjection]
        public IEnumerable<Foo> GetFooEnumerable() => throw new NotImplementedException();
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }
}
