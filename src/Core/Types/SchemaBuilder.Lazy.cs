using System.Net;
using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Types.Factories;

namespace HotChocolate
{
    public partial class SchemaBuilder
    {
        public class LazySchema
        {
            private ISchema _schema;
            private bool _isSet;

            public ISchema Schema
            {
                get
                {
                    if (!_isSet)
                    {
                        throw new InvalidOperationException();
                    }
                    return _schema;
                }
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }
                    _schema = value;
                }
            }
        }
    }
}
