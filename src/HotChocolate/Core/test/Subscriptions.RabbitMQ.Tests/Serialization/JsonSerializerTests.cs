using System;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class JsonSerializerTests
{
    class Test: IEquatable<Test>
    {
        public string Foo { get; set; } = "Foo";

        public bool Equals(Test? other)
            => other is not null && Foo == other.Foo;

        public override bool Equals(object? obj)
            => obj is Test t && Equals(t);

        public override int GetHashCode()
            => Foo.GetHashCode();
    }

    [Fact]
    public void Serialize()
    {
        ISerializer sut = new JsonSerializer();
        string @string = sut.Serialize(new Test { Foo = "Bar" });
        Assert.Equal("{\"$type\":\"HotChocolate.Subscriptions.RabbitMQ.Serialization.JsonSerializerTests+Test, HotChocolate.Subscriptions.RabbitMQ.Tests\",\"Foo\":\"Bar\"}", @string);
    }

    [Fact]
    public void Deserialize()
    {
        ISerializer sut = new JsonSerializer();
        Test test = sut.Deserialize<Test>("{\"$type\":\"HotChocolate.Subscriptions.RabbitMQ.Serialization.JsonSerializerTests+Test, HotChocolate.Subscriptions.RabbitMQ.Tests\",\"Foo\":\"Baz\"}");
        Assert.Equal(new Test { Foo = "Baz"}, test);
    }
}
