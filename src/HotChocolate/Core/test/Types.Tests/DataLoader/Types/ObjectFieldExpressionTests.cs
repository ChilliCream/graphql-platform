
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectFieldExpressionTests
    {
        [Fact]
        public void Infer_Field_Types_From_Expression()
        {
            SchemaBuilder.New()
                .AddQueryType<Foo>(d =>
                {
                    d.Name("Query");
                    d.Field(t => t.Bar.Text);
#if !NETCOREAPP2_1 && !NETCOREAPP3_1
                    d.Field(t => t.Bars.Select(t => t.Text)).Name("texts");
#endif
                })
                .Create()
                .ToString()
#if NETCOREAPP2_1 || NETCOREAPP3_1
                .MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
                .MatchSnapshot();
#endif
        }

        [Fact]
        public void Execute_Expression_Fields()
        {
            SchemaBuilder.New()
                .AddQueryType<Foo>(d =>
                {
                    d.Name("Query");
                    d.Field(t => t.Bar.Text);
                    d.Field(t => t.Bars.Select(t => t.Text)).Name("texts");
                    d.Field(t => t.Bars.Select(t => t.Text).FirstOrDefault()).Name("firstText");
                })
                .Create()
                .MakeExecutable()
                .Execute("{ text texts firstText }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void Execute_Complex_Expression_Fields()
        {
            SchemaBuilder.New()
                .AddQueryType<Foo>(d =>
                {
                    d.Name("Query");
                    d.Field(t => t.Bar.Count + t.Bar.Text.Length).Name("calc");
                })
                .Create()
                .MakeExecutable()
                .Execute("{ calc }")
                .ToJson()
                .MatchSnapshot();
        }

        public class Foo
        {
            public IEnumerable<Bar> Bars => new[] { new Bar() };

            public Bar Bar => new Bar();

            public string Field = "ABC";
        }

        public class Bar
        {
            public string Text { get; } = "Hello";

            public int Count { get; } = 1;
        }
    }
}