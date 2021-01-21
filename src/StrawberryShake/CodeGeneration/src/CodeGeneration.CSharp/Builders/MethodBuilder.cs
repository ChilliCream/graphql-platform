using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class MethodBuilder : ICodeContainer<MethodBuilder>
    {
        private AccessModifier _accessModifier = AccessModifier.Private;
        private Inheritance _inheritance = Inheritance.None;
        private bool _isStatic;
        private bool _is;
        private TypeReferenceBuilder _returnType = TypeReferenceBuilder.New().SetName("void");
        private string? _name;
        private readonly List<ParameterBuilder> _parameters = new();
        private readonly List<ICode> _lines = new();

        public static MethodBuilder New() => new();

        public MethodBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public MethodBuilder SetStatic()
        {
            _isStatic = true;
            return this;
        }

        public MethodBuilder Set()
        {
            _is = true;
            return this;
        }

        public MethodBuilder SetInheritance(Inheritance value)
        {
            _inheritance = value;
            return this;
        }

        public MethodBuilder SetReturnType(string value, bool condition = true)
        {
            if (condition)
            {
                _returnType = TypeReferenceBuilder.New().SetName(value);
            }
            return this;
        }

        public MethodBuilder SetReturnType(TypeReferenceBuilder value, bool condition = true)
        {
            if (condition)
            {
                _returnType = value;
            }
            return this;
        }

        public MethodBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public MethodBuilder AddParameter(ParameterBuilder value)
        {
            _parameters.Add(value);
            return this;
        }

        public MethodBuilder AddCode(string code, bool addIf = true)
        {
            if (addIf)
            {
                _lines.Add(CodeLineBuilder.New().SetLine(code));
            }
            return this;
        }

        public MethodBuilder AddCode(ICode code, bool addIf = true)
        {
            if (addIf)
            {
                _lines.Add(code);
            }
            return this;
        }

        public MethodBuilder AddEmptyLine()
        {
            _lines.Add(CodeLineBuilder.New());
            return this;
        }

        public MethodBuilder AddInlineCode(string code)
        {
            _lines.Add(CodeInlineBuilder.New().SetText(code));
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            writer.WriteIndent();

            writer.Write($"{modifier} ");

            if (_isStatic)
            {
                writer.Write("static ");
            }

            if (_is)
            {
                writer.Write(" ");
            }

            writer.Write($"{CreateInheritance()}");
            _returnType.Build(writer);
            writer.Write($"{_name}(");

            if (_parameters.Count == 0)
            {
                writer.Write(")");
            }
            else if (_parameters.Count == 1)
            {
                _parameters[0].Build(writer);
                writer.Write(")");
            }
            else
            {
                writer.WriteLine();

                using (writer.IncreaseIndent())
                {
                    for (var i = 0; i < _parameters.Count; i++)
                    {
                        writer.WriteIndent();
                        _parameters[i].Build(writer);
                        if (i == _parameters.Count - 1)
                        {
                            writer.Write(")");
                        }
                        else
                        {
                            writer.Write(",");
                            writer.WriteLine();
                        }
                    }
                }
            }

            writer.WriteLine();
            writer.WriteIndentedLine("{");

            using (writer.IncreaseIndent())
            {
                foreach (ICode code in _lines)
                {
                    code.Build(writer);
                }
            }

            writer.WriteIndentedLine("}");
        }

        private string CreateInheritance()
        {
            switch (_inheritance)
            {
                case Inheritance.Override:
                    return "override ";

                case Inheritance.Sealed:
                    return "sealed override ";

                case Inheritance.Virtual:
                    return "virtual ";

                default:
                    return string.Empty;
            }
        }
    }
}
