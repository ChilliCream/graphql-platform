namespace HotChocolate;

public partial class SchemaBuilder
{
    public sealed class LazySchema
    {
        private readonly List<Action<Schema>> _callbacks = [];
        private Schema? _schema;

        public Schema Schema
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

                Action<Schema>[] callbacks;
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

        public void OnSchemaCreated(Action<Schema> callback)
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
