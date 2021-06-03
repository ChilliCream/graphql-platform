using System;

#nullable enable

namespace HotChocolate
{
    public partial class SchemaBuilder
    {
        public sealed class LazySchema
        {
            public event EventHandler? Completed;

            private ISchema? _schema;
            private bool _isSet;

            public ISchema Schema
            {
                get
                {
                    if (!_isSet || _schema is null)
                    {
                        throw new InvalidOperationException(
                            "The schema does not yet exist.");
                    }

                    return _schema;
                }
                set
                {
                    if (_isSet)
                    {
                        throw new InvalidOperationException(
                            "The schema was already created.");
                    }

                    _schema = value ?? throw new ArgumentNullException(nameof(value));
                    _isSet = true;
                    Completed?.Invoke(this, EventArgs.Empty);
                    Completed = null;
                }
            }
        }
    }
}
