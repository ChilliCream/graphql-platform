#nullable enable

namespace HotChocolate;

public partial class SchemaBuilder
{
    public sealed class LazySchema
    {
        private readonly List<Action<ISchema>> _callbacks = new();
        private ISchema? _schema;

        public ISchema Schema
        {
            get
            {
                if (_schema is null)
                {
                    throw new InvalidOperationException(
                        "The schema does not yet exist.");
                }

                return _schema;
            }
            set
            {
                if (_schema is not null)
                {
                    throw new InvalidOperationException(
                        "The schema was already created.");
                }

                _schema = value ?? throw new ArgumentNullException(nameof(value));

                Action<ISchema>[] callbacks;
                lock (_callbacks)
                {
                    callbacks = _callbacks.ToArray();
                    _callbacks.Clear();
                }

                foreach (var callback in callbacks)
                {
                    callback(_schema);
                }
            }
        }

        public void OnSchemaCreated(Action<ISchema> callback)
        {
            if (_schema is not null)
            {
                callback(_schema);
            }

            lock (_callbacks)
            {
                if (_schema is not null)
                {
                    callback(_schema);
                    return;
                }

                _callbacks.Add(callback);
            }
        }
    }
}
