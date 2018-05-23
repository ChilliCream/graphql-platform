using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal static partial class IntrospectionTypes
    {
        public static readonly Func<ISchemaContext, EnumTypeConfig> __TypeKind = c => new EnumTypeConfig
        {
            Name = _typeKindName,
            Description = "An enum describing what kind of type a given `__Type` is.",
            IsIntrospection = true,
            Values = new[] {
                new EnumValue(new EnumValueConfig
                {
                    Description = "Indicates this type is a scalar.",
                    Value = TypeKind.Scalar
                }),
                new EnumValue(new EnumValueConfig
                {
                    Description =
                        "Indicates this type is an object. " +
                        "`fields` and `interfaces` are valid fields.",
                    Value = TypeKind.Object
                }),
                new EnumValue(new EnumValueConfig
                {
                    Description =
                        "Indicates this type is an interface. " +
                        "`fields` and `possibleTypes` are valid fields.",
                    Value = TypeKind.Interface
                }),
                new EnumValue(new EnumValueConfig
                {
                    Description =
                        "Indicates this type is a union. " +
                        "`possibleTypes` is a valid field.",
                    Value = TypeKind.Union
                }),
                new EnumValue(new EnumValueConfig
                {
                    Description =
                        "Indicates this type is an enum. " +
                        "`enumValues` is a valid field.",
                    Value = TypeKind.Enum
                }),
                new EnumValue(new EnumValueConfig
                {
                    Name = "INPUT_OBJECT",
                    Description =
                        "Indicates this type is an input object. " +
                        "`inputFields` is a valid field.",
                    Value = TypeKind.InputObject
                }),
                new EnumValue(new EnumValueConfig
                {
                    Description =
                        "Indicates this type is a list. " +
                        "`ofType` is a valid field.",
                    Value = TypeKind.List
                }),
                new EnumValue(new EnumValueConfig
                {
                    Name = "NON_NULL",
                    Description =
                        "Indicates this type is a non-null. " +
                        "`ofType` is a valid field.",
                    Value = TypeKind.NonNull
                }),
            }
        };
    }
}
