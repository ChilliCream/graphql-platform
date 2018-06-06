using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;
using Xunit;

namespace HotChocolate.Types
{
    public class TypeInspectorTests
    {
        [Fact]
        public void Case4()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(NonNullType<ListType<NonNullType<StringType>>>);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case3_1()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(ListType<NonNullType<StringType>>);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case3_2()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(NonNullType<ListType<StringType>>);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case2_1()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(NonNullType<StringType>);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<NonNullType>(type);
            type = ((NonNullType)type).Type as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case2_2()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(ListType<StringType>);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<ListType>(type);
            type = ((ListType)type).ElementType as IOutputType;
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void Case1()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(StringType);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void DotNetStringType()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(StringType));
            Type nativeType = typeof(string);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<StringType>(type);
        }

        [Fact]
        public void DotNetIntType()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(IntType));
            Type nativeType = typeof(int);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<NonNullType>(type);
            Assert.IsType<IntType>(((NonNullType)type).Type);
        }

        [Fact]
        public void DotNetNullableIntType()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(IntType));
            Type nativeType = typeof(int?);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<IntType>(type);
        }

        [Fact]
        public void DotNetBoolType()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(BooleanType));
            Type nativeType = typeof(bool);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<NonNullType>(type);
            Assert.IsType<BooleanType>(((NonNullType)type).Type);
        }

        [Fact]
        public void DotNetNullableBoolType()
        {
            // arrange
            TypeRegistry typeRegistry = new TypeRegistry();
            typeRegistry.RegisterType(typeof(BooleanType));
            Type nativeType = typeof(bool?);

            // act
            TypeInspector typeInspector = new TypeInspector();
            IOutputType type = typeInspector.CreateOutputType(typeRegistry, nativeType);

            // assert
            Assert.IsType<BooleanType>(type);
        }
    }
}
