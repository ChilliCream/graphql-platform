using System;
using HotChocolate.Internal;
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

            TypeReference typeReference = new TypeReference(typeof(T));
            _typeRegistry.RegisterType(typeReference);
            return _typeRegistry.GetType<T>(typeReference);
        }

        #endregion

        #region RegisterType - Instance

        public void RegisterType<T>(T namedType)
            where T : class, INamedType
        {
            _typeRegistry.RegisterType(namedType);
        }

        public void RegisterQueryType<T>(T objectType) where T : ObjectType
        {
            Options.QueryTypeName = objectType.Name;
            _typeRegistry.RegisterType(objectType);
        }

        public void RegisterMutationType<T>(T objectType) where T : ObjectType
        {
            Options.MutationTypeName = objectType.Name;
            _typeRegistry.RegisterType(objectType);
        }

        public void RegisterSubscriptionType<T>(T objectType) where T : ObjectType
        {
            Options.SubscriptionTypeName = objectType.Name;
            _typeRegistry.RegisterType(objectType);
        }

        #endregion

        #region Directives

        public void RegisterDirective<T>() where T : Directive
        {
            throw new NotImplementedException();
        }

        public void RegisterDirective<T>(T directive) where T : Directive
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
