using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
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
            if (!_locs.TryGetValue(location, out Language.DirectiveLocation l))
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.DirectiveTypeFactory_LocationNotSupported,
                    location));
            }
            return l;
        }
    }
}
