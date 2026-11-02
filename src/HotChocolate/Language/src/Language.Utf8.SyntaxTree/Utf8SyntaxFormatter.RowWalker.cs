using System.Diagnostics;
using DbRow = HotChocolate.Language.Utf8OperationDocument.DbRow;

namespace HotChocolate.Language;

internal static partial class Utf8SyntaxFormatter
{
    /// <summary>
    /// Walks the packed metadata rows of a <see cref="Utf8OperationDocument"/> in document order
    /// and writes the GraphQL source text the rows describe. Every token that carries content is
    /// written from its own row's source range, and every punctuation and keyword is written from
    /// the row kinds, so the output never depends on the formatting of the source document.
    /// </summary>
    private ref struct RowWalker
    {
        private readonly Utf8OperationDocument _document;
        private readonly Utf8SyntaxWriter _writer;
        private readonly Utf8VariableNameMap _variables;
        private readonly ReadOnlySpan<byte> _prefix;
        private readonly SharedOrdinalSet _shared;
        private readonly int _siteCount;
        private int _siteIndex;

        internal RowWalker(
            Utf8OperationDocument document,
            Utf8SyntaxWriter writer,
            Utf8VariableNameMap variables,
            ReadOnlySpan<byte> prefix,
            SharedOrdinalSet shared)
        {
            _document = document;
            _writer = writer;
            _variables = variables;
            _prefix = prefix;
            _shared = shared;
            _siteCount = variables.IsEmpty && prefix.IsEmpty ? 0 : document.VariableSiteCount;
            _siteIndex = 0;
        }

        /// <summary>
        /// Writes every definition of the document, separated by a single space.
        /// </summary>
        internal void WriteDocument()
        {
            var rowCount = _document.RowCount;
            var cursor = 0;

            while (cursor < rowCount)
            {
                if (cursor > 0)
                {
                    _writer.Write(" "u8);
                }

                cursor = WriteNode(cursor);
            }
        }

        /// <summary>
        /// Writes the node at <paramref name="cursor"/> and the rows it spans.
        /// </summary>
        internal void Write(int cursor)
        {
            SeekVariableSites(_document.GetRow(cursor).Location);
            WriteNode(cursor);
        }

        /// <summary>
        /// Writes the field at <paramref name="cursor"/> and the rows it spans, leaving out the
        /// selection set it declares.
        /// </summary>
        internal void WriteFieldWithoutSelectionSet(int cursor)
        {
            var row = _document.GetRow(cursor);
            Debug.Assert(row.Kind is Utf8SyntaxKind.Field);

            SeekVariableSites(row.Location);
            WriteField(cursor, row, writeSelectionSet: false);
        }

        private void SeekVariableSites(int position)
        {
            if (_siteCount > 0)
            {
                _siteIndex = _document.FindFirstVariableSite(position);
            }
        }

        private int WriteNode(int cursor)
        {
            var row = _document.GetRow(cursor);

            switch (row.Kind)
            {
                case Utf8SyntaxKind.OperationQuery:
                    WriteOperation(cursor, row, "query"u8);
                    break;

                case Utf8SyntaxKind.OperationMutation:
                    WriteOperation(cursor, row, "mutation"u8);
                    break;

                case Utf8SyntaxKind.OperationSubscription:
                    WriteOperation(cursor, row, "subscription"u8);
                    break;

                case Utf8SyntaxKind.FragmentDefinition:
                    WriteFragmentDefinition(cursor, row);
                    break;

                case Utf8SyntaxKind.VariableDefinition:
                    WriteVariableDefinition(cursor, row);
                    break;

                case Utf8SyntaxKind.SelectionSet:
                    WriteSelectionSet(cursor, row);
                    break;

                case Utf8SyntaxKind.Field:
                    WriteField(cursor, row, writeSelectionSet: true);
                    break;

                case Utf8SyntaxKind.FragmentSpread:
                    WriteFragmentSpread(cursor, row);
                    break;

                case Utf8SyntaxKind.InlineFragment:
                    WriteInlineFragment(cursor, row);
                    break;

                case Utf8SyntaxKind.Argument:
                case Utf8SyntaxKind.ObjectField:
                    WriteNamedValue(cursor);
                    break;

                case Utf8SyntaxKind.Directive:
                    WriteDirective(cursor, row);
                    break;

                case Utf8SyntaxKind.ListValue:
                    WriteListValue(cursor, row);
                    break;

                case Utf8SyntaxKind.ObjectValue:
                    WriteObjectValue(cursor, row);
                    break;

                case Utf8SyntaxKind.Variable:
                    _writer.Write("$"u8);
                    WriteVariableName(row);
                    break;

                case Utf8SyntaxKind.ListType:
                    _writer.Write("["u8);
                    WriteNode(cursor + 1);
                    _writer.Write("]"u8);
                    break;

                case Utf8SyntaxKind.NonNullType:
                    WriteNode(cursor + 1);
                    _writer.Write("!"u8);
                    break;

                default:
                    WriteToken(row);
                    break;
            }

            return cursor + row.NumberOfRows;
        }

        private void WriteOperation(int cursor, DbRow row, ReadOnlySpan<byte> keyword)
        {
            _writer.Write(keyword);

            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;
            var nameRow = _document.GetRow(index);

            if (nameRow.Kind is Utf8SyntaxKind.Name)
            {
                _writer.Write(" "u8);
                WriteToken(nameRow);
                index++;
            }

            index = WriteVariableDefinitions(index, end);
            index = WriteDirectives(index, end);
            WriteNode(index);
        }

        private void WriteFragmentDefinition(int cursor, DbRow row)
        {
            _writer.Write("fragment "u8);

            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;
            WriteToken(_document.GetRow(index++));

            var afterVariables = WriteVariableDefinitions(index, end);

            if (afterVariables == index)
            {
                _writer.Write(" on "u8);
            }
            else
            {
                _writer.Write("on "u8);
            }

            index = afterVariables;
            WriteToken(_document.GetRow(index++));
            index = WriteDirectives(index, end);
            WriteNode(index);
        }

        private int WriteVariableDefinitions(int cursor, int end)
        {
            if (cursor >= end
                || _document.GetRow(cursor).Kind is not Utf8SyntaxKind.VariableDefinition)
            {
                return cursor;
            }

            _writer.Write("("u8);
            var index = cursor;

            while (index < end)
            {
                var row = _document.GetRow(index);

                if (row.Kind is not Utf8SyntaxKind.VariableDefinition)
                {
                    break;
                }

                if (index > cursor)
                {
                    _writer.Write(","u8);
                }

                WriteVariableDefinition(index, row);
                index += row.NumberOfRows;
            }

            _writer.Write(")"u8);
            return index;
        }

        private void WriteVariableDefinition(int cursor, DbRow row)
        {
            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;

            _writer.Write("$"u8);
            WriteVariableName(_document.GetRow(index++));
            _writer.Write(":"u8);
            index = WriteNode(index);

            if (index < end && IsValue(_document.GetRow(index).Kind))
            {
                _writer.Write("="u8);
                index = WriteNode(index);
            }

            WriteDirectives(index, end);
        }

        private void WriteSelectionSet(int cursor, DbRow row)
        {
            _writer.Write("{"u8);

            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;

            while (index < end)
            {
                if (index > cursor + 1)
                {
                    _writer.Write(" "u8);
                }

                index = WriteNode(index);
            }

            _writer.Write("}"u8);
        }

        private void WriteField(int cursor, DbRow row, bool writeSelectionSet)
        {
            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;
            var aliasRow = _document.GetRow(index);

            if (aliasRow.Kind is Utf8SyntaxKind.Alias)
            {
                WriteToken(aliasRow);
                _writer.Write(":"u8);
                index++;
            }

            WriteToken(_document.GetRow(index++));
            index = WriteArguments(index, end);
            index = WriteDirectives(index, end);

            if (writeSelectionSet && index < end)
            {
                WriteNode(index);
            }
        }

        private void WriteFragmentSpread(int cursor, DbRow row)
        {
            _writer.Write("..."u8);

            var index = cursor + 1;
            WriteToken(_document.GetRow(index++));
            WriteDirectives(index, cursor + row.NumberOfRows);
        }

        private void WriteInlineFragment(int cursor, DbRow row)
        {
            _writer.Write("..."u8);

            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;
            var typeRow = _document.GetRow(index);

            if (typeRow.Kind is Utf8SyntaxKind.TypeCondition)
            {
                _writer.Write("on "u8);
                WriteToken(typeRow);
                index++;
            }

            index = WriteDirectives(index, end);
            WriteNode(index);
        }

        private int WriteDirectives(int cursor, int end)
        {
            var index = cursor;

            while (index < end)
            {
                var row = _document.GetRow(index);

                if (row.Kind is not Utf8SyntaxKind.Directive)
                {
                    break;
                }

                WriteDirective(index, row);
                index += row.NumberOfRows;
            }

            return index;
        }

        private void WriteDirective(int cursor, DbRow row)
        {
            _writer.Write("@"u8);

            var index = cursor + 1;
            WriteToken(_document.GetRow(index++));
            WriteArguments(index, cursor + row.NumberOfRows);
        }

        private int WriteArguments(int cursor, int end)
        {
            if (cursor >= end || _document.GetRow(cursor).Kind is not Utf8SyntaxKind.Argument)
            {
                return cursor;
            }

            _writer.Write("("u8);
            var index = cursor;

            while (index < end)
            {
                var row = _document.GetRow(index);

                if (row.Kind is not Utf8SyntaxKind.Argument)
                {
                    break;
                }

                if (index > cursor)
                {
                    _writer.Write(","u8);
                }

                WriteNamedValue(index);
                index += row.NumberOfRows;
            }

            _writer.Write(")"u8);
            return index;
        }

        private void WriteNamedValue(int cursor)
        {
            var index = cursor + 1;
            WriteToken(_document.GetRow(index++));
            _writer.Write(":"u8);
            WriteNode(index);
        }

        private void WriteListValue(int cursor, DbRow row)
        {
            _writer.Write("["u8);

            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;

            while (index < end)
            {
                if (index > cursor + 1)
                {
                    _writer.Write(","u8);
                }

                index = WriteNode(index);
            }

            _writer.Write("]"u8);
        }

        private void WriteObjectValue(int cursor, DbRow row)
        {
            _writer.Write("{"u8);

            var index = cursor + 1;
            var end = cursor + row.NumberOfRows;

            while (index < end)
            {
                if (index > cursor + 1)
                {
                    _writer.Write(","u8);
                }

                index = WriteNode(index);
            }

            _writer.Write("}"u8);
        }

        private void WriteVariableName(DbRow row)
        {
            if (_siteCount == 0)
            {
                WriteToken(row);
                return;
            }

            var ordinal = ReadOrdinal(row.Location);

            if (!_prefix.IsEmpty)
            {
                if (!_shared.Contains(ordinal))
                {
                    _writer.Write(_prefix);
                }

                WriteToken(row);
                return;
            }

            if (_variables.TryGetReplacement(ordinal, out var name))
            {
                _writer.Write(name.Span);
                return;
            }

            WriteToken(row);
        }

        private int ReadOrdinal(int position)
        {
            while (_document.GetVariableSitePosition(_siteIndex) < position)
            {
                _siteIndex++;
            }

            Debug.Assert(_document.GetVariableSitePosition(_siteIndex) == position);
            return _document.GetVariableSiteOrdinal(_siteIndex++);
        }

        private void WriteToken(DbRow row)
            => _writer.Write(_document.GetSource(row.Location, row.SizeOrLength));

        private static bool IsValue(Utf8SyntaxKind kind)
            => kind is Utf8SyntaxKind.ListValue
                or Utf8SyntaxKind.ObjectValue
                or Utf8SyntaxKind.Variable
                or Utf8SyntaxKind.IntValue
                or Utf8SyntaxKind.FloatValue
                or Utf8SyntaxKind.StringValue
                or Utf8SyntaxKind.EnumValue;
    }
}
