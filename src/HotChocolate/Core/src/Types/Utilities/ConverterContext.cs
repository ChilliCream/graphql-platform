using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities;

public class ConverterContext
{
    public string Name { get; set; }

    public object Object { get; set; }

    public Type ClrType { get; set; }

    public IInputType InputType { get; set; }

    public FieldCollection<InputField> InputFields { get; set; }

    public ILookup<string, PropertyInfo> Fields { get; set; }

    public ISyntaxNode Node { get; set; }
}
