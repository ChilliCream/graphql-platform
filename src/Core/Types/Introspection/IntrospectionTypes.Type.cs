using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Introspection
{
    internal static partial class IntrospectionTypes
    {
        public static readonly Func<ISchemaContext, ObjectTypeConfig> __Type = c => new ObjectTypeConfig
        {
            Name = _typeName,
            Description =
                "The fundamental unit of any GraphQL Schema is the type. There are " +
                "many kinds of types in GraphQL as represented by the `__TypeKind` enum." +
                "\n\nDepending on the kind of a type, certain fields describe " +
                "information about that type. Scalar types provide no information " +
                "beyond a name and description, while Enum types provide their values. " +
                "Object and Interface types provide the fields they describe. Abstract " +
                "types, Union and Interface, provide the Object types possible " +
                "at runtime. List and NonNull types compose other types.",
            IsIntrospection = true,
            Fields = new[]
            {
                new Field(new FieldConfig
                {
                    Name = "kind",
                    Type = () => new NonNullType(c.GetOutputType(_typeKindName)),
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        if(!type.TryGetKind(out TypeKind kind))
                        {
                            return new QueryError("Unknown kind of type: " + type);
                        }
                        return kind;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "name",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        if(type is INamedType n)
                        {
                            return n.Name;
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "description",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        if(type is INamedType n)
                        {
                            return n.Description;
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "fields",
                    Type = () => new ListType(new NonNullType(c.GetOutputType(_fieldName))),
                    Arguments = new []
                    {
                        new InputField(new InputFieldConfig
                        {
                            Name = "includeDeprecated",
                            Type = () => c.BooleanType(),
                            DefaultValue = () => new BooleanValueNode(false)
                        })
                    },
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        bool includeDeprecated = ctx.Argument<bool>("includeDeprecated");
                        if(type.IsObjectType() || type.IsInterfaceType())
                        {
                            IReadOnlyDictionary<string, Field> fields =
                                ((IHasFields)type).Fields;
                            if(!includeDeprecated)
                            {
                                return fields.Values.Where(t => !t.IsDeprecated);
                            }
                            return fields.Values;
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "interfaces",
                    Type = () => new ListType(new NonNullType(c.GetOutputType(_typeName))),
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        if(type is ObjectType ot)
                        {
                            return ot.Interfaces.Values;
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "possibleTypes",
                    Type = () =>  new ListType(new NonNullType(c.GetOutputType(_directiveName))),
                    Resolver = () => (ctx, ct) =>
                    {
                        INamedType type = ctx.Parent<INamedType>();
                        if(type.IsAbstractType())
                        {
                            return ctx.Schema.GetPossibleTypes(type);
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "enumValues",
                    Type = () => new ListType(new NonNullType(c.GetOutputType(_enumValueName))),
                    Arguments = new[]
                    {
                        new InputField(new InputFieldConfig
                        {
                            Name = "includeDeprecated",
                            Type = () => c.BooleanType(),
                            DefaultValue = () => new BooleanValueNode(false)
                        })
                    },
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        bool includeDeprecated = ctx.Argument<bool>("includeDeprecated");
                        if(type is EnumType et)
                        {
                            IReadOnlyCollection<EnumValue> values = et.Values;
                            if(!includeDeprecated)
                            {
                                return values.Where(t => !t.IsDeprecated);
                            }
                            return values;
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "inputFields",
                    Type = () => new ListType(new NonNullType(c.GetOutputType(_inputValueName))),
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        if(type is InputObjectType iot)
                        {
                            return iot.Fields.Values;
                        }
                        return null;
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "ofType",
                    Type = () => c.GetOutputType(_typeName),
                    Resolver = () => (ctx, ct) =>
                    {
                        IType type = ctx.Parent<IType>();
                        if(type is ListType lt)
                        {
                            return lt.ElementType;
                        }
                        else if(type is NonNullType nnt)
                        {
                            return nnt.Type;
                        }
                        else
                        {
                            return null;
                        }
                    }
                })
            }
        };
    }
}
