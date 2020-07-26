using System;

namespace HotChocolate
{
    public partial class SchemaBuilder
    {
        internal class LazySchema
        {
            private ISchema _schema;
            private bool _isSet;

            public ISchema Schema
            {
                get
                {
                    if (!_isSet)
                    {
                        throw new InvalidOperationException(
                            "Schema is not ready yet.");
                    }

                    return _schema;
                }
                set
                {
                    if (value is null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }

                    if (_isSet)
                    {
                        throw new InvalidOperationException();
                    }

                    _isSet = true;
                    _schema = value;
                }
            }
        }
    }
}
