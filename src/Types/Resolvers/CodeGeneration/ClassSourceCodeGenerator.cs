using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class ClassSourceCodeGenerator
    {
        internal const string Namespace = "HotChocolate.Resolvers.CodeGeneration";
        private const string ClassNameTemplate = "___CompiledResolvers__";

        private static readonly ISourceCodeGenerator[] _generators =
        {
            new AsyncResolverMethodGenerator(),
            new SyncResolverMethodGenerator(),
            new ResolverPropertyGenerator(),

            new AsyncSourceMethodGenerator(),
            new SyncSourceMethodGenerator(),
            new SourcePropertyGenerator(),

            new AsyncOnInvokeMethodGenerator(),
            new SyncOnInvokeMethodGenerator(),

            new SyncOnAfterInvokeMethodGenerator(),
        };

        public GeneratedClass Generate(
            IEnumerable<IDelegateDescriptor> resolverDescriptors)
        {
            if (resolverDescriptors == null)
            {
                throw new ArgumentNullException(nameof(resolverDescriptors));
            }

            string className = ClassNameTemplate + Guid.NewGuid().ToString("N");
            string sourceText = GenerateClass(resolverDescriptors, className);

            return new GeneratedClass(Namespace, className, sourceText);
        }

        private static string GenerateClass(
            IEnumerable<IDelegateDescriptor> resolverDescriptors,
            string className)
        {
            var source = new StringBuilder();

            source.AppendLine("using System;");
            source.AppendLine("using System.Collections.Generic;");
            source.AppendLine("using System.Diagnostics;");
            source.AppendLine("using System.IO;");
            source.AppendLine("using System.Linq;");
            source.AppendLine("using System.Threading;");
            source.AppendLine("using System.Threading.Tasks;");
            source.AppendLine("using HotChocolate;");
            source.AppendLine("using HotChocolate.Resolvers;");
            source.AppendLine("using HotChocolate.Types;");

            source.AppendLine($"namespace {Namespace}");
            source.AppendLine("{");
            source.AppendLine($"public static class {className}");
            source.AppendLine("{");

            GenerateResolvers(resolverDescriptors, source);

            source.AppendLine("}");
            source.AppendLine("}");
            return source.ToString();
        }

        private static void GenerateResolvers(
            IEnumerable<IDelegateDescriptor> resolverDescriptors,
            StringBuilder source)
        {
            var i = 0;
            foreach (IDelegateDescriptor resolverDescriptor in
                resolverDescriptors)
            {
                ISourceCodeGenerator generator = _generators.First(
                    t => t.CanHandle(resolverDescriptor));
                source.AppendLine(generator.Generate(
                    GetDelegateName(i++), resolverDescriptor));
            }
        }

        public static string GetDelegateName(int index)
        {
            return "_" + index;
        }
    }
}
