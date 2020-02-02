﻿using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Class
        | AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Parameter
        | AttributeTargets.Field)]
    public sealed class GraphQLDescriptionAttribute
        : Attribute
    {
        public GraphQLDescriptionAttribute(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }

            Description = description;
        }

        public string Description { get; }
    }
}
