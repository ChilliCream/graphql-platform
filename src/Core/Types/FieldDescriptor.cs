using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class FieldDescriptor
        : IFieldDescriptor
    {
        public FieldDescriptor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(name));
            }

            if (ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            Name = name;
        }

        public FieldDescriptor(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Property = property;
            Name = property.GetGraphQLName();
        }

        public FieldDescriptor(PropertyInfo property, string name)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Property = property;
            Name = name;
        }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public PropertyInfo Property { get; protected set; }

        public Type NativeType { get; protected set; }

        public string DeprecationReason { get; protected set; }

        public ImmutableList<ArgumentDescriptor> Arguments { get; protected set; }

        public FieldResolverDelegate Resolver { get; protected set; }

        public Field CreateField()
        {
            return new Field(new FieldConfig
            {
                Name = Name,
                Description = Description,
                DeprecationReason = DeprecationReason,
                Type = CreateType,
                Arguments = CreateArguments(),
                Resolver = CreateResolver
            });
        }

        private IOutputType CreateType(SchemaContext context)
        {
            return TypeConverter.CreateOutputType(context, NativeType);
        }

        private IEnumerable<InputField> CreateArguments()
        {

        }

        private FieldResolverDelegate CreateResolver(ISchemaContext context)
        {
            throw new NotImplementedException();
        }

        #region IFieldDescriptor

        IFieldDescriptor IFieldDescriptor.Description(string description)
        {
        }

        IFieldDescriptor IFieldDescriptor.Type<IOutputType>()
        {
            throw new NotImplementedException();
        }

        IFieldDescriptor IFieldDescriptor.DeprecationReason(string deprecationReason)
        {
            throw new NotImplementedException();
        }

        IFieldDescriptor IFieldDescriptor.Argument(string name, Action<IArgumentDescriptor> argument)
        {
            throw new NotImplementedException();
        }

        IFieldDescriptor IFieldDescriptor.Resolver(FieldResolverDelegate fieldResolver)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class MyFooType
        : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.GetType()).Type<ListType<MyFooType>>();
        }
    }

    public class Foo
    {

    }
}
