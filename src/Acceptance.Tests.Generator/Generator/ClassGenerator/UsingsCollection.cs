using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Generator.ClassGenerator
{
    public class UsingsCollection : IEnumerable<string>, IClassPart
    {
        public List<string> _usings = new List<string>();

        public void AddRange(IEnumerable<string> namespaces)
        {
            foreach (var namespaceValue in namespaces)
            {
                _usings.Add($"using {namespaceValue};");
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _usings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_usings).GetEnumerator();
        }

        public string Generate()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var usingValue in _usings)
            {
                builder.AppendLine(usingValue);
            }

            return builder.ToString();
        }
    }
}
