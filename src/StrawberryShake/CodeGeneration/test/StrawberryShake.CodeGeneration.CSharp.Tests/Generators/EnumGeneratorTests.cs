using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class EnumGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly EnumGenerator _generator;

        public EnumGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new EnumGenerator();
        }

        [Fact]
        public void Generate_Enum_One_Element()
        {
            // arrange
            var descriptor = new EnumDescriptor(
                "Abc", 
                "Def", 
                new List<EnumValueDescriptor>
                {
                    new EnumValueDescriptor("Ghi")
                });

            // act
            _generator.Generate(_codeWriter, descriptor);

            // assert
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void Generate_Enum_Two_Elements()
        {
            // arrange
            var descriptor = new EnumDescriptor(
                "Abc", 
                "Def", 
                new List<EnumValueDescriptor>
                {
                    new EnumValueDescriptor("Ghi"),
                    new EnumValueDescriptor("Jkl")
                });

            // act
            _generator.Generate(_codeWriter, descriptor);

            // assert
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void Generate_Enum_One_Element_Underlying_Type()
        {
            // arrange
            var descriptor = new EnumDescriptor(
                "Abc", 
                "Def", 
                new List<EnumValueDescriptor>
                {
                    new EnumValueDescriptor("Ghi")
                },
                "global::Underlying.Type");

            // act
            _generator.Generate(_codeWriter, descriptor);

            // assert
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void Generate_Enum_One_Element_With_Value()
        {
            // arrange
            var descriptor = new EnumDescriptor(
                "Abc", 
                "Def", 
                new List<EnumValueDescriptor>
                {
                    new EnumValueDescriptor("Ghi", 123)
                });

            // act
            _generator.Generate(_codeWriter, descriptor);

            // assert
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
