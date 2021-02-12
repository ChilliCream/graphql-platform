using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class MethodCallBuilder : ICode
    {
        private string? _methodName;
        private bool _determineStatement = true;
        private string? _prefix;
        private readonly List<ICode> _arguments = new List<ICode>();
        private readonly List<ICode> _generics = new List<ICode>();
        private readonly List<ICode> _chainedCode = new List<ICode>();

        public static MethodCallBuilder New() => new MethodCallBuilder();

        public MethodCallBuilder SetPrefix(string prefix)
        {
            _prefix = prefix;
            return this;
        }

        public MethodCallBuilder SetMethodName(string methodName)
        {
            _methodName = methodName;
            return this;
        }

        public MethodCallBuilder AddChainedCode(ICode value)
        {
            _chainedCode.Add(value);
            return this;
        }

        public MethodCallBuilder AddArgument(ICode value)
        {
            _arguments.Add(value);
            return this;
        }

        public MethodCallBuilder AddGeneric(ICode value)
        {
            _generics.Add(value);
            return this;
        }

        public MethodCallBuilder AddGeneric(string value)
        {
            _generics.Add(CodeInlineBuilder.New().SetText(value));
            return this;
        }

        public MethodCallBuilder AddArgument(string value)
        {
            _arguments.Add(CodeInlineBuilder.New().SetText(value));
            return this;
        }

        public MethodCallBuilder SetDetermineStatement(bool value)
        {
            _determineStatement = value;
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_determineStatement)
            {
                writer.WriteIndent();
            }

            writer.Write(_prefix);

            if (_methodName != null)
            {
                writer.Write(_methodName);

                if (_generics.Count > 0)
                {
                    writer.Write("<");
                    for (int i = 0; i < _generics.Count; i++)
                    {
                        _generics[i].Build(writer);
                        if (i == _generics.Count - 1)
                        {
                            writer.Write(">");
                        }
                        else
                        {
                            writer.Write(",");
                        }
                    }

                    writer.Write(">");
                }

                writer.Write("(");

                if (_arguments.Count == 0)
                {
                    writer.Write(")");
                }
                else if (_arguments.Count == 1)
                {
                    _arguments[0].Build(writer);
                    writer.Write(")");
                }
                else
                {
                    writer.WriteLine();

                    using (writer.IncreaseIndent())
                    {
                        for (int i = 0; i < _arguments.Count; i++)
                        {
                            writer.WriteIndent();
                            _arguments[i].Build(writer);
                            if (i == _arguments.Count - 1)
                            {
                                writer.WriteLine();
                                writer.DecreaseIndent();
                                writer.WriteIndent();
                                writer.Write(")");
                                writer.IncreaseIndent();
                            }
                            else
                            {
                                writer.Write(",");
                                writer.WriteLine();
                            }
                        }
                    }
                }
            }

            using (writer.IncreaseIndent())
            {
                foreach (ICode code in _chainedCode)
                {
                    writer.WriteLine();
                    writer.WriteIndent();
                    writer.Write('.');
                    code.Build(writer);
                }
            }

            if (_determineStatement)
            {
                writer.Write(";");
                writer.WriteLine();
            }
        }
    }
}
