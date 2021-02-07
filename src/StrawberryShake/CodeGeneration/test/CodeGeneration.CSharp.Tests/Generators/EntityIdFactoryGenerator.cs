using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class EntityIdFactoryGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly EntityIdFactoryGenerator _generator;

        public EntityIdFactoryGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new EntityIdFactoryGenerator();
        }

        [Fact]
        public void GenerateEntityIdFactory()
        {
            // arrange
            var descriptor = new EntityIdFactoryDescriptor(
                "EntityIdFactory",
                new List<EntityIdDescriptor>
                {
                    new EntityIdDescriptor(
                        "Foo",
                        "Foo",
                        new List<EntityIdDescriptor> {
                            new EntityIdDescriptor("id", "String")
                        }),
                    new EntityIdDescriptor(
                        "Bar",
                        "Bar",
                        new List<EntityIdDescriptor> {
                            new EntityIdDescriptor("id", "Int16"),
                            new EntityIdDescriptor("b", "Boolean")
                        })
                });

            // act
            _generator.Generate(_codeWriter, descriptor, out _);

            // assert
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
