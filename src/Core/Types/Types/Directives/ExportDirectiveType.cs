
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types
{
    public sealed class ExportDirectiveType
        : DirectiveType<ExportDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<ExportDirective> descriptor)
        {
            descriptor.Name(ExportDirectiveHelper.Name);
            descriptor.Location(DirectiveLocation.Field);

            descriptor.Argument(t => t.As).Type<StringType>();

            descriptor.Use(next => context =>
            {
                if (context.ContextData.TryGetValue(
                    ExportDirectiveHelper.ExportedVariables,
                    out object obj)
                    && obj is ICollection<ExportedVariable> exported)
                {
                    IDirective directive = context.Field.Directives.GetFirst(ExportDirectiveHelper.Name);
                    string name = directive.ToObject<ExportDirective>().As;
                }



            });
        }
    }

    public static class ExportDirectiveHelper
    {
        internal const string Name = "export";
        internal const string ExportedVariables = "HC.ExportedVariables";

        public static void AddExportedVariables(
            this IDictionary<string, object> contextData)
        {
            contextData[ExportedVariables] =
                new ConcurrentBag<ExportedVariable>();
        }

        public static ICollection<ExportedVariable> GetExportedVariables(
            this IDictionary<string, object> contextData)
        {
            return (ICollection<ExportedVariable>)
                contextData[ExportedVariables];
        }
    }

    public sealed class ExportedVariable
    {
        public ExportedVariable(string name, object value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;

        }
        public string Name { get; }
        public object Value { get; }
    }
}
