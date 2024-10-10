using System.Globalization;
using HotChocolate.Properties;

namespace HotChocolate.Types;

public static class DirectiveLocationExtensions
{
    private static readonly Dictionary<DirectiveLocation, Language.DirectiveLocation> _locs =
       new()
       {
           {
               DirectiveLocation.Query,
               Language.DirectiveLocation.Query
           },
           {
               DirectiveLocation.Mutation,
               Language.DirectiveLocation.Mutation
           },
           {
               DirectiveLocation.Subscription,
               Language.DirectiveLocation.Subscription
           },
           {
               DirectiveLocation.Field,
               Language.DirectiveLocation.Field
           },
           {
               DirectiveLocation.FragmentDefinition,
               Language.DirectiveLocation.FragmentDefinition
           },
           {
               DirectiveLocation.FragmentSpread,
               Language.DirectiveLocation.FragmentSpread
           },
           {
               DirectiveLocation.InlineFragment,
               Language.DirectiveLocation.InlineFragment
           },
           {
               DirectiveLocation.VariableDefinition,
               Language.DirectiveLocation.VariableDefinition
           },
           {
               DirectiveLocation.Schema,
               Language.DirectiveLocation.Schema
           },
           {
               DirectiveLocation.Scalar,
               Language.DirectiveLocation.Scalar
           },
           {
               DirectiveLocation.Object,
               Language.DirectiveLocation.Object
           },
           {
               DirectiveLocation.FieldDefinition,
               Language.DirectiveLocation.FieldDefinition
           },
           {
               DirectiveLocation.ArgumentDefinition,
               Language.DirectiveLocation.ArgumentDefinition
           },
           {
               DirectiveLocation.Interface,
               Language.DirectiveLocation.Interface
           },
           {
               DirectiveLocation.Union,
               Language.DirectiveLocation.Union
           },
           {
               DirectiveLocation.Enum,
               Language.DirectiveLocation.Enum
           },
           {
               DirectiveLocation.EnumValue,
               Language.DirectiveLocation.EnumValue
           },
           {
               DirectiveLocation.InputObject,
               Language.DirectiveLocation.InputObject
           },
           {
               DirectiveLocation.InputFieldDefinition,
               Language.DirectiveLocation.InputFieldDefinition
           },
       };

    public static Language.DirectiveLocation MapDirectiveLocation(
        this DirectiveLocation location)
    {
        if (!_locs.TryGetValue(location, out var l))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                TypeResources.DirectiveTypeFactory_LocationNotSupported,
                location));
        }
        return l;
    }

    internal static IEnumerable<DirectiveLocation> AsEnumerable(
        this DirectiveLocation locations)
    {
        if ((locations & DirectiveLocation.Query) == DirectiveLocation.Query)
        {
            yield return DirectiveLocation.Query;
        }

        if ((locations & DirectiveLocation.Mutation) == DirectiveLocation.Mutation)
        {
            yield return DirectiveLocation.Mutation;
        }

        if ((locations & DirectiveLocation.Subscription) == DirectiveLocation.Subscription)
        {
            yield return DirectiveLocation.Subscription;
        }

        if ((locations & DirectiveLocation.Field) == DirectiveLocation.Field)
        {
            yield return DirectiveLocation.Field;
        }

        if ((locations & DirectiveLocation.FragmentDefinition) ==
            DirectiveLocation.FragmentDefinition)
        {
            yield return DirectiveLocation.FragmentDefinition;
        }

        if ((locations & DirectiveLocation.FragmentSpread) == DirectiveLocation.FragmentSpread)
        {
            yield return DirectiveLocation.FragmentSpread;
        }

        if ((locations & DirectiveLocation.InlineFragment) == DirectiveLocation.InlineFragment)
        {
            yield return DirectiveLocation.InlineFragment;
        }

        if ((locations & DirectiveLocation.VariableDefinition) ==
            DirectiveLocation.VariableDefinition)
        {
            yield return DirectiveLocation.VariableDefinition;
        }

        if ((locations & DirectiveLocation.Schema) == DirectiveLocation.Schema)
        {
            yield return DirectiveLocation.Schema;
        }

        if ((locations & DirectiveLocation.Scalar) == DirectiveLocation.Scalar)
        {
            yield return DirectiveLocation.Scalar;
        }

        if ((locations & DirectiveLocation.Object) == DirectiveLocation.Object)
        {
            yield return DirectiveLocation.Object;
        }

        if ((locations & DirectiveLocation.FieldDefinition) ==
            DirectiveLocation.FieldDefinition)
        {
            yield return DirectiveLocation.FieldDefinition;
        }

        if ((locations & DirectiveLocation.ArgumentDefinition) ==
            DirectiveLocation.ArgumentDefinition)
        {
            yield return DirectiveLocation.ArgumentDefinition;
        }

        if ((locations & DirectiveLocation.Interface) == DirectiveLocation.Interface)
        {
            yield return DirectiveLocation.Interface;
        }

        if ((locations & DirectiveLocation.Union) == DirectiveLocation.Union)
        {
            yield return DirectiveLocation.Union;
        }

        if ((locations & DirectiveLocation.Enum) == DirectiveLocation.Enum)
        {
            yield return DirectiveLocation.Enum;
        }

        if ((locations & DirectiveLocation.EnumValue) == DirectiveLocation.EnumValue)
        {
            yield return DirectiveLocation.EnumValue;
        }

        if ((locations & DirectiveLocation.InputObject) == DirectiveLocation.InputObject)
        {
            yield return DirectiveLocation.InputObject;
        }

        if ((locations & DirectiveLocation.InputFieldDefinition) ==
            DirectiveLocation.InputFieldDefinition)
        {
            yield return DirectiveLocation.InputFieldDefinition;
        }
    }
}
