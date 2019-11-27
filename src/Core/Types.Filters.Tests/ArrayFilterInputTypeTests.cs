using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_ArraySimpleFilter_FooImplicitBarImplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterBool_FooExplicitBarExplicitAny()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarBool)
                    .BindExplicitly()
                    .AllowAny();
            }
            ));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDouble_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDouble)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                     );
            }
            ));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDoubleNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDoubleNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                     );
            }
            ));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterBool_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarBool)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                     );
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterBoolNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarBoolNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                     );
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterInt32_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarInt32)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                     );
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterInt32Nullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarInt32Nullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterInt64_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarInt64)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterInt64Nullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarInt64Nullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterSingle_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarSingle)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterSingleNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarSingleNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterInt16_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarInt16)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterInt16Nullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarInt16Nullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDatetime_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDatetime)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                     );
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDatetimeNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDatetimeNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDatetimeOffset_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDatetimeOffset)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDatetimeOffsetNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDatetimeOffsetNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterGuid_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarGuid)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterGuidNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarGuidNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_ArraySimpleFilterDecimal_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDecimal)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArraySimpleFilterDecimalNullable_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooSimple>(x =>
            {
                x.BindFieldsExplicitly()
                    .List(y => y.BarDecimalNullable)
                    .BindExplicitly()
                    .AllowSome(z => z.BindFieldsExplicitly()
                        .Filter(m => m.Element)
                        .BindFiltersExplicitly()
                        .AllowEquals());
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_ArrayStringFilter_FooImplicitBarImplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<FooString>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArrayStringFilter_FooImplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<FooString>(
                    x => x.List(y => y.BarString).BindImplicitly()
                        .AllowSome(
                            y => y.BindFieldsExplicitly()
                            .Filter(z => z.Element)
                            .BindFiltersExplicitly()
                            .AllowContains())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArrayObjectFilter_FooImplicitBarImplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArrayObjectFilter_FooImplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.List(y => y.BarNested).BindImplicitly()
                        .AllowSome(
                            y => y.BindFieldsExplicitly()
                            .Filter(z => z.Baz)
                            .BindFiltersExplicitly()
                            .AllowContains())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact(Skip = "Skipped till issue 1194 is solved")]
        public void Create_ArrayObjectFilter_FooImplicitMultipleBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.List(y => y.BarNested).BindImplicitly()
                        .AllowSome(
                            y => y.BindFieldsExplicitly()
                                .Filter(z => z.Baz)
                                .BindFiltersExplicitly()
                                .AllowContains())
                        .And()
                        .AllowNone(
                            y => y.BindFieldsExplicitly()
                                .Filter(z => z.Baz)
                                .BindFiltersExplicitly()
                                .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_ArrayObjectFilter_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.BindFieldsExplicitly()
                        .List(y => y.BarNested)
                        .AllowSome(
                            y => y.BindFieldsExplicitly()
                            .Filter(z => z.Baz)
                            .BindFiltersExplicitly()
                            .AllowContains())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArrayObjectFilter_FooExplicitBarExplicitByGeneric()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.BindFieldsExplicitly()
                        .List(y => y.BarNested)
                        .BindExplicitly()
                        .AllowSome<BarFilterType>()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ArrayObjectFilter_FooExplicitBarImplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.BindFieldsExplicitly()
                    .List(y => y.BarNested)
                    .AllowSome()));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_ArrayObjectFilter_BindExplicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(descriptor => descriptor
                    .BindFieldsExplicitly()
                    .List(x => x.BarNested)
                    .AllowSome(x => x.BindFieldsExplicitly()
                        .Filter(y => y.Baz)
                        .BindFiltersExplicitly()
                        .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }


        /// <summary>
        /// This test checks if the binding of all allow methods are correct
        /// </summary>
        [Fact]
        public void Create_ArrayFilter_Declare_Operators_Explicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.List(x => x.BarNested)
                    .BindExplicitly()
                    .AllowSome();
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Name_Explicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.List(x => x.BarNested)
                    .BindExplicitly()
                    .AllowSome()
                    .Name("custom_array");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Description_Explicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.List(x => x.BarNested)
                    .BindExplicitly()
                    .AllowSome()
                    .Description("custom_array_description");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_By_Name()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.List(x => x.BarNested)
                        .BindExplicitly()
                        .AllowSome()
                        .Directive("bar");
                }))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_By_Name_With_Argument()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.List(x => x.BarNested)
                        .BindExplicitly()
                        .AllowSome()
                        .Directive("bar",
                            new ArgumentNode("qux",
                                new StringValueNode("foo")));
                }))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.InputFieldDefinition)
                    .Argument("qux")
                    .Type<StringType>())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_With_Clr_Type()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.List(x => x.BarNested)
                        .BindExplicitly()
                        .AllowSome()
                        .Directive<Bar>();
                }))
                .AddDirectiveType(new DirectiveType<Bar>(d => d
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

           [Fact]    
        public void Create_ArraySimpleFilter_FooImplicitBarImplicit_Collection()    
        {    
            // arrange    
            // act    
            var schema = CreateSchema(new FilterInputType<FooSimpleCollection>());    

            // assert    
            schema.ToString().MatchSnapshot();    
        }
        

        public class FooSimpleCollection    
        {    
            public int[] Array { get; set; }    
            public int?[] ArrayNullable { get; set; }    

            public List<int> List { get; set; }    
            public List<int?> ListNullable { get; set; }    
            public Queue<int> Queue { get; set; }    
            public Queue<int?> QueueNullable { get; set; }    
            public Stack<int> Stack { get; set; }    
            public Stack<int?> StackNullable { get; set; }    
            public IList<int> IList { get; set; }    
            public IList<int?> IListNullable { get; set; }    
            public HashSet<int> HashSet { get; set; }    
            public HashSet<int?> HashSetNullable { get; set; }    
            public ICollection<int> ICollection { get; set; }    
            public ICollection<int?> ICollectionNullable { get; set; }    
            public IReadOnlyCollection<int> IReadOnlyCollection { get; set; }    
            public IReadOnlyCollection<int?> IReadOnlyCollectionNullable { get; set; }    
            public ISet<int> ISet { get; set; }    
            public ISet<int?> ISetNullable { get; set; }    
        }

        public class FooString
        {
            public IEnumerable<string> BarString { get; set; }
        }

        public class FooSimple
        {
            public IEnumerable<string> BarString { get; set; }
            public IEnumerable<bool> BarBool { get; set; }
            public IEnumerable<short> BarInt16 { get; set; }
            public IEnumerable<int> BarInt32 { get; set; }
            public IEnumerable<long> BarInt64 { get; set; }
            public IEnumerable<double> BarDouble { get; set; }
            public IEnumerable<float> BarSingle { get; set; }
            public IEnumerable<decimal> BarDecimal { get; set; }
            public IEnumerable<Guid> BarGuid { get; set; }
            public IEnumerable<DateTime> BarDatetime { get; set; }
            public IEnumerable<DateTimeOffset> BarDatetimeOffset { get; set; }
            public IEnumerable<bool?> BarBoolNullable { get; set; }
            public IEnumerable<short?> BarInt16Nullable { get; set; }
            public IEnumerable<int?> BarInt32Nullable { get; set; }
            public IEnumerable<long?> BarInt64Nullable { get; set; }
            public IEnumerable<double?> BarDoubleNullable { get; set; }
            public IEnumerable<float?> BarSingleNullable { get; set; }
            public IEnumerable<decimal?> BarDecimalNullable { get; set; }
            public IEnumerable<Guid?> BarGuidNullable { get; set; }
            public IEnumerable<DateTime?> BarDatetimeNullable { get; set; }
            public IEnumerable<DateTimeOffset?> BarDatetimeOffsetNullable { get; set; }
        }

        public class Foo
        {
            public string Bar { get; set; }
            public IEnumerable<Bar> BarNested { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.BindFieldsExplicitly()
                    .List(x => x.BarNested)
                    .AllowSome();
            }
        }

        public class BarFilterType
            : FilterInputType<Bar>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Bar> descriptor)
            {
                descriptor.BindFieldsExplicitly()
                    .Filter(x => x.Baz)
                    .BindFiltersExplicitly()
                    .AllowEquals()
                    .Name("test");
            }
        }
    }
}
