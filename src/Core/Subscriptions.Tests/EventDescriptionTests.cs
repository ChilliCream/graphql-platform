using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Subscriptions
{
    public class EventDescriptionTests
    {
        [Fact]
        public void EventDescription_CreateWithoutArguments()
        {
            // arrange
            // act
            var description = new EventDescription("event");

            // assert
            Assert.Equal("event", description.Name);
            Assert.Empty(description.Arguments);
        }

        [Fact]
        public void EventDescription_CreateWithArgument()
        {
            // arrange
            // act
            var description = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            // assert
            Assert.Equal("event", description.Name);
            Assert.Collection(description.Arguments,
                arg =>
                {
                    Assert.Equal("foo", arg.Name.Value);
                    Assert.Equal("bar", ((StringValueNode)arg.Value).Value);
                });
        }

        [Fact]
        public void EventDescription_Equals_True()
        {
            // arrange
            var a = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            var b = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EventDescription_ObjectValue_Equals_True()
        {
            // arrange
            var a = new EventDescription("event",
                new ArgumentNode("foo", new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "abc"))));

            var b = new EventDescription("event",
                new ArgumentNode("foo", new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "abc"))));

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EventDescription_EqualsWithNoArgs_True()
        {
            // arrange
            var a = new EventDescription("event");

            var b = new EventDescription("event");

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EventDescription_Equals_False()
        {
            // arrange
            var a = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            var b = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("baz")));

            var c = new EventDescription("event_2",
                new ArgumentNode("foo", new StringValueNode("bar")));

            // act
            bool atob = a.Equals(b);
            bool atoc = a.Equals(c);

            // assert
            Assert.False(atob);
            Assert.False(atoc);
        }

        [Fact]
        public void EventDescription_ToString()
        {
            // arrange
            var a = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            var b = new EventDescription("event");

            // act
            string result_a = a.ToString();
            string result_b = b.ToString();

            // assert
            Assert.Equal("event(foo: \"bar\")", result_a);
            Assert.Equal("event", result_b);
        }

        [Fact]
        public void EventDescription_GetHashCode()
        {
            // arrange
            var a = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            var b = new EventDescription("event",
                new ArgumentNode("foo", new StringValueNode("bar")));

            var c = new EventDescription("event_2",
                new ArgumentNode("foo", new StringValueNode("bar")));

            // act
            int result_a = a.GetHashCode();
            int result_b = b.GetHashCode();
            int result_c = c.GetHashCode();

            // assert
            Assert.Equal(result_a, result_b);
            Assert.NotEqual(result_a, result_c);
        }

        [Fact]
        public void EventDescription_ObjectValue_GetHashCode()
        {
            // arrange
            var a = new EventDescription("event",
                new ArgumentNode("foo", new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "abc"))));

            var b = new EventDescription("event",
                new ArgumentNode("foo", new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "abc"))));

            // act
            int hasha = a.GetHashCode();
            int hashb = b.GetHashCode();

            // assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void EventDescription_Parse_WithArguments()
        {
            // arrange
            string s = "event(foo: \"bar\")";

            // act
            EventDescription description = EventDescription.Parse(s);

            // assert
            Assert.Equal("event", description.Name);
            Assert.Collection(description.Arguments,
                t =>
                {
                    Assert.Equal("foo", t.Name.Value);
                    Assert.Equal("bar", t.Value.Value);
                });
        }

        [Fact]
        public void EventDescription_Parse_WithoutArguments()
        {
            // arrange
            string s = "event";

            // act
            EventDescription description = EventDescription.Parse(s);

            // assert
            Assert.Equal("event", description.Name);
            Assert.Empty(description.Arguments);
        }
    }
}
