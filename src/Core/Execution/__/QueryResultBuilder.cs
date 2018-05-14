using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal class QueryResultBuilder
    {
        private Dictionary<Path, object> _items = new Dictionary<Path, object>();

        public void AddValue(in Path path, object value)
        {
            _items[path] = value;
        }

        public Dictionary<string, object> Build()
        {
            Dictionary<string, object> root = new Dictionary<string, object>();

            foreach (KeyValuePair<Path, object> item in _items)
            {

            }
        }

        private Dictionary<string, object> ResolveObject(Dictionary<string, object> root, Path path)
        {
            Dictionary<string, object> current = root;
            foreach (Path pathElement in path.Elements)
            {
                if (pathElement.IsIndexer)
                {

                }
                else
                {
                    if (!current.TryGetValue(pathElement.Name, out object value))
                    {
                        Dictionary<string, object> newValue = new Dictionary<string, object>();
                        current[pathElement.Name] = newValue;
                        current = newValue;
                    }
                    else
                    {
                        current = (Dictionary<string, object>)value;
                    }
                }
            }

        }
    }
}
