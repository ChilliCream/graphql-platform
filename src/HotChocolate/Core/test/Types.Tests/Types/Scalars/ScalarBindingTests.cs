using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types;

public class ScalarBindingTests
{
    [Fact]
    public void Ensure_That_Explicit_Binding_Behavior_Is_Respected_On_Scalars()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryA>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Ensure_That_Implicit_Binding_Behavior_Is_Respected_On_Scalars()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryB>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    public class QueryA
    {
        public Bar? Bar([GraphQLType(typeof(ExplicitBindingScalar))]int id) => new Bar();
    }

    public class QueryB
    {
        public Bar? Bar([GraphQLType(typeof(ImplicitBindingScalar))]int id) => new Bar();
    }

    public class Bar
    {
        public Baz? Baz { get; set; }
    }

    public class Baz
    {
        public int Text { get; set; }
    }

    public class ImplicitBindingScalar : ScalarType<int>
    {
        public ImplicitBindingScalar()
            : base("FOO", BindingBehavior.Implicit)
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public override object? ParseLiteral(IValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseValue(object? value)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }
    }

    public class ExplicitBindingScalar : ScalarType<int>
    {
        public ExplicitBindingScalar()
            : base("FOO", BindingBehavior.Explicit)
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public override object? ParseLiteral(IValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseValue(object? value)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }
    }
}
