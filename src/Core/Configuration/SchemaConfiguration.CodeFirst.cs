using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        #region RegisterType - Type

        public void RegisterType<T>()
            where T : class, INamedType
        {
            CreateAndRegisterType<T>();
        }

        public void RegisterQueryType<T>()
            where T : ObjectType
        {
            T type = CreateAndRegisterType<T>();
            Options.QueryTypeName = type.Name;
        }

        public void RegisterMutationType<T>()
            where T : ObjectType
        {
            T type = CreateAndRegisterType<T>();
            Options.MutationTypeName = type.Name;
        }

        public void RegisterSubscriptionType<T>()
            where T : ObjectType
        {
            T type = CreateAndRegisterType<T>();
            Options.SubscriptionTypeName = type.Name;
        }

        private T CreateAndRegisterType<T>()
            where T : class, INamedType
        {
            if (BaseTypes.IsNonGenericBaseType(typeof(T)))
            {
                throw new SchemaException(new SchemaError(
                    "You cannot add a type without specifing its " +
                    "name and attributes."));
            }

            T type = _serviceManager.GetService<T>();
            _types[type.Name] = type;
            return type;
        }

        #endregion

        #region RegisterType - Instance

        public void RegisterType<T>(T namedType)
            where T : class, INamedType
        {
            _types[namedType.Name] = namedType;
        }

        public void RegisterQueryType<T>(T objectType) where T : ObjectType
        {
            Options.QueryTypeName = objectType.Name;
            _types[objectType.Name] = objectType;
        }

        public void RegisterMutationType<T>(T objectType) where T : ObjectType
        {
            Options.MutationTypeName = objectType.Name;
            _types[objectType.Name] = objectType;
        }

        public void RegisterSubscriptionType<T>(T objectType) where T : ObjectType
        {
            Options.SubscriptionTypeName = objectType.Name;
            _types[objectType.Name] = objectType;
        }

        #endregion
    }
}
