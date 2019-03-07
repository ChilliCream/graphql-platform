using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator.ClassGenerator
{
    public class ClassBuilder
    {
        private readonly UsingsCollection _usings = new UsingsCollection();
        private readonly string _className;
        private string _namespace;
        private ClassConstructor _constructor = ClassConstructor.Empty;
        private readonly List<ClassMethod> _methods = new List<ClassMethod>();
        private readonly List<Statement> _fields = new List<Statement>();

        private string _content = string.Empty;

        private ClassBuilder(string className)
        {
            _className = className;
            _namespace = className;
        }

        public static ClassBuilder Init(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException(nameof(className));
            }

            return new ClassBuilder(className);
        }

        public ClassBuilder WithUsings(params string[] namespaces)
        {
            _usings.AddRange(namespaces);
            return this;
        }

        public ClassBuilder WithNamespace(string namespaceValue)
        {
            if (string.IsNullOrEmpty(namespaceValue))
            {
                throw new ArgumentException(
                    "Value cannot be null or empty.",
                    nameof(namespaceValue));
            }

            _namespace = $"namespace {namespaceValue}";
            return this;
        }

        public ClassBuilder WithConstructor(params Statement[] statements)
        {
            _constructor = new ClassConstructor(_className, statements);
            return this;
        }

        public ClassBuilder WithFields(params Statement[] statement)
        {
            _fields.AddRange(statement);
            return this;
        }

        public ClassBuilder WithMethods(params ClassMethod[] methods)
        {
            _methods.AddRange(methods);
            return this;
        }

        public Compilation Compile()
        {
            return new Compilation
            {
                Errors = SyntaxFactory
                    .ParseCompilationUnit(Build())
                    .GetDiagnostics()
                    .Select(d => d.GetMessage())
                    .ToList(),
                Source = _content
            };
        }

        public string Build()
        {
            if (string.IsNullOrEmpty(_content))
            {
                StringBuilder builder = new StringBuilder();

                builder.AppendLine(_usings.Generate());
                builder.AppendLine();
                builder.AppendLine(_namespace);
                builder.AppendLine("{");

                builder.AppendLine($"public class {_className}");
                builder.AppendLine("{");

                foreach (Statement field in _fields)
                {
                    builder.AppendLine(field.Generate());
                }

                if (_fields.Any())
                {
                    builder.AppendLine();
                }

                builder.AppendLine(_constructor.Generate());

                foreach (ClassMethod classMethod in _methods)
                {
                    builder.AppendLine(classMethod.Generate());
                }

                builder.AppendLine("}");
                builder.AppendLine("}");

                _content = SyntaxFactory
                    .ParseCompilationUnit(builder.ToString())
                    .NormalizeWhitespace()
                    .ToFullString();
            }

            return _content;
        }
    }
}
