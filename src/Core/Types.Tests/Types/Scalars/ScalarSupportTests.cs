using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HotChocolate
{
    public class ScalarSupportTests
    {
        [Theory]
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
            INamedType typeFound = schema.Types.FirstOrDefault(a => a.Name == type);

            //assert
            Assert.Null(typeFound);
        }

        [Theory]
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
            INamedType typeFound = schema.Types.FirstOrDefault(a => a.Name == type);

            //assert
            Assert.NotNull(typeFound);
        }

        [Theory]
        [InlineData("Long", typeof(long), "Overridden long")]
        [InlineData("Decimal", typeof(decimal), "Overridden decimal")]
        [InlineData("DateTime", typeof(DateTimeOffset), "Overridden DateTime")]
        public async void RegisterCustomExtendedScalarType(string name, Type type, string desc)
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
                    "{ __type(type: \"" + name + "\") { name, kind, description } }");

            //assert
            result.Snapshot("RegisterCustomExtendedScalarType" + name);
        }

        [Theory]
        [InlineData("Float", typeof(double), "Overridden Float")]
        [InlineData("String", typeof(string), "Overridden String")]
        [InlineData("Int", typeof(int), "Overridden Int")]
        [InlineData("Boolean", typeof(bool), "Overridden Boolean")]
        [InlineData("ID", typeof(string), "Overridden ID")]
        public async void ShouldNotAllowSwappingOfBaseScalarType(string name, Type type, string desc)
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
                    "{ __type(type: \"" + name + "\") { name, kind, description } }");

            //assert
            result.Snapshot("ShouldNotAllowSwappingOfBaseScalarType" + name);
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

        public override string Description { get; }

        public override Type ClrType { get; }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object Serialize(object value)
        {
            throw new NotImplementedException();
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            throw new NotImplementedException();
        }
    }
}
