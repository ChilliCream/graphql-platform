using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

            string typeResultDescription = JObject.Parse(result.ToString())
                ["data"]["__type"]["description"]
                .ToString();

            //assert
            Assert.Equal(desc, typeResultDescription);
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

            string typeResultDescription = JObject.Parse(result.ToString())
                ["data"]["__type"]["description"]
                .ToString();

            //assert
            Assert.NotEqual(desc, typeResultDescription);
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
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is FloatValueNode
                || literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is FloatValueNode floatLiteral)
            {
                return double.Parse(
                    floatLiteral.Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }

            if (literal is IntValueNode node)
            {
                return double.Parse(node.Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is double d)
            {
                return new FloatValueNode(SerializeDouble(d));
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()),
                nameof(value));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is double d)
            {
                return d;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is double d)
            {
                value = d;
                return true;
            }

            value = null;
            return false;
        }

        private static double ParseDouble(string value) =>
            double.Parse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture);

        private static string SerializeDouble(double value) =>
            value.ToString("E", CultureInfo.InvariantCulture);
    }
}
