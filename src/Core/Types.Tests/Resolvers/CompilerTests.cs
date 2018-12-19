using System;
using System.Reflection;
using HotChocolate.Resolvers;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class CompilerTests
    {
        [Fact]
        public void SimpleClassCompilation()
        {
            // arrange
            string sourceText = @"
                namespace FS
                {
                    public class Foo
                    {
                        public string Bar()
                        {
                            return ""Hello World"";
                        }
                    }
                }
            ";

            // act
            Assembly assembly = CSharpCompiler.Compile(sourceText);

            // assert
            object obj = assembly.CreateInstance("FS.Foo");
            Assert.NotNull(obj);

            MethodInfo method = obj.GetType().GetMethod("Bar");
            object result = method.Invoke(obj, Array.Empty<object>());
            Assert.IsType<string>(result);
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void SimpleClassWithReferenceToCurrentAssembly()
        {
            // arrange
            string sourceText = @"
                using HotChocolate.Resolvers;

                namespace FS
                {
                    public class DynamicFoo
                    {
                        public string Bar(Foo foo)
                        {
                            return foo.Bar;
                        }
                    }
                }
            ";

            // act
            Assembly assembly = CSharpCompiler.Compile(sourceText);

            // assert
            Foo foo = new Foo { Bar = Guid.NewGuid().ToString() };

            object obj = assembly.CreateInstance("FS.DynamicFoo");
            Assert.NotNull(obj);

            MethodInfo method = obj.GetType().GetMethod("Bar");
            object result = method.Invoke(obj, new object[] { foo });
            Assert.IsType<string>(result);
            Assert.Equal(foo.Bar, result);
        }
    }

    public class Foo
    {
        public string Bar { get; set; }
    }
}
