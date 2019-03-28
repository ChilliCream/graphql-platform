using System;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class ScalarSupportTests
    {
        [Theory]
        [InlineData("Byte")]
        [InlineData("Short")]
        [InlineData("Long")]
        [InlineData("DateTime")]
        [InlineData("Date")]
        [InlineData("Decimal")]
        [InlineData("Uuid")]
        [InlineData("Url")]
        public void GetExtendedScalarTypesWithoutRegistration(string type)
        {
            // arrange, schema extended types not registered
            var schema = Schema.Create(c => { });

            //act
            INamedType typeFound = schema.Types
                .FirstOrDefault(a => a.Name == type);

            //assert
            Assert.Null(typeFound);
        }

        [Theory]
        [InlineData("Byte")]
        [InlineData("Short")]
        [InlineData("Long")]
        [InlineData("DateTime")]
        [InlineData("Date")]
        [InlineData("Decimal")]
        [InlineData("Uuid")]
        [InlineData("Url")]
        public void GetExtendedScalarTypesWithRegistration(string type)
        {
            // arrange, schema extended types registered
            var schema = Schema.Create(c =>
            {
                c.RegisterExtendedScalarTypes();
            });

            //act
            INamedType typeFound = schema.Types
                .FirstOrDefault(a => a.Name == type);

            //assert
            Assert.NotNull(typeFound);
        }

        [Theory]
        [InlineData("Byte", typeof(byte), "Overridden byte")]
        [InlineData("Short", typeof(short), "Overridden short")]
        [InlineData("Long", typeof(long), "Overridden long")]
        [InlineData("Decimal", typeof(decimal), "Overridden decimal")]
        [InlineData("DateTime", typeof(DateTimeOffset), "Overridden DateTime")]
        public async void RegisterCustomExtendedScalarType(
            string name, Type type, string desc)
        {
            // arrange, register custom scalar type
            var customScalarType = new CustomScalarType(name, type, desc);
            var schema = Schema.Create(c =>
            {
                c.RegisterType(customScalarType);
            });

            // act, use introspection to retrieve registered type description
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    @"{
                        __type(name: """ + name + @""")
                        {
                            name
                            kind
                            description
                        }
                    }");

            //assert
            result.MatchSnapshot(SnapshotNameExtension.Create(name));
        }

        [Theory]
        [InlineData("Float", typeof(double), "Overridden Float")]
        [InlineData("String", typeof(string), "Overridden String")]
        [InlineData("Int", typeof(int), "Overridden Int")]
        [InlineData("Boolean", typeof(bool), "Overridden Boolean")]
        [InlineData("ID", typeof(string), "Overridden ID")]
        public async void ShouldNotAllowSwappingOfBaseScalarType(
            string name, Type type, string desc)
        {
            // arrange, register custom base scalar type
            var customBaseScalarType = new CustomScalarType(name, type, desc);
            var schema = Schema.Create(c =>
            {
                c.RegisterType(customBaseScalarType);
            });

            // act, use introspection to retrieve registered type description
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    @"{
                        __type(name: """ + name + @""")
                        {
                            name
                            kind
                            description
                        }
                    }");

            //assert
            result.MatchSnapshot("ShouldNotAllowSwappingOfBaseScalarType" + name);
        }
    }

    public class CustomScalarType
        : ScalarType
    {
        public CustomScalarType(string name, Type clrType, string description)
            : base(name)
        {
            ClrType = clrType;
            Description = description;
        }

        public override Type ClrType { get; }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotSupportedException();
        }

        public override object Serialize(object value)
        {
            throw new NotSupportedException();
        }

        public override bool TryDeserialize(
            object serialized, out object value)
        {
            throw new NotSupportedException();
        }
    }
}
