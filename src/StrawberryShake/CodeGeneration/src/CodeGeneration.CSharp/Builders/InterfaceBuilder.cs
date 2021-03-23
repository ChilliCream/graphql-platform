using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceBuilder : AbstractTypeBuilder
    {
        private readonly List<MethodBuilder> _methods = new();

        private XmlCommentBuilder? _xmlComment;

        public static InterfaceBuilder New() => new();

        public new InterfaceBuilder SetName(NameString name)
        {
            base.SetName(name);
            return this;
        }

        public new InterfaceBuilder AddImplements(string value)
        {
            base.AddImplements(value);
            return this;
        }

        public new InterfaceBuilder AddProperty(PropertyBuilder property)
        {
            base.AddProperty(property);
            return this;
        }

        public InterfaceBuilder SetComment(string? xmlComment)
        {
            if (xmlComment is not null)
            {
                _xmlComment = XmlCommentBuilder.New().SetSummary(xmlComment);
            }

            return this;
        }

        public InterfaceBuilder SetComment(XmlCommentBuilder? xmlComment)
        {
            if (xmlComment is not null)
            {
                _xmlComment = xmlComment;
            }

            return this;
        }

        public InterfaceBuilder AddMethod(MethodBuilder method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            _methods.Add(method);
            return this;
        }

        public override void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _xmlComment?.Build(writer);

            writer.WriteGeneratedAttribute();

            writer.WriteIndent();

            writer.Write("public interface ");
            writer.WriteLine(Name);

            if (Implements.Count > 0)
            {
                using (writer.IncreaseIndent())
                {
                    for (var i = 0; i < Implements.Count; i++)
                    {
                        writer.WriteIndentedLine(
                            i == 0
                                ? $": {Implements[i]}"
                                : $", {Implements[i]}");
                    }
                }
            }

            writer.WriteIndentedLine("{");

            var writeLine = false;

            using (writer.IncreaseIndent())
            {
                if (Properties.Count > 0)
                {
                    for (var i = 0; i < Properties.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            writer.WriteLine();
                        }

                        Properties[i].Build(writer);
                    }

                    writeLine = true;
                }

                if (_methods.Count > 0)
                {
                    for (var i = 0; i < _methods.Count; i++)
                    {
                        if (writeLine || i > 0)
                        {
                            writer.WriteLine();
                        }

                        _methods[i].Build(writer);
                    }
                }
            }

            writer.WriteIndentedLine("}");
        }
    }
}
