using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
    {
        internal static class Tools
        {
            internal static bool IsSchemaType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return Helper.IsSchemaType(type);
            }

            internal static bool IsGenericBaseType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return BaseTypes.IsGenericBaseType(type);
            }

            internal static bool IsNonGenericBaseType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return BaseTypes.IsNonGenericBaseType(type);
            }
        }
    }
}
