using System.Text;

namespace HotChocolate.CostAnalysis;

internal sealed class CsvTable
{
    private readonly string _path;
    private readonly string[] _columns;

    private CsvTable(string path, string[] columns, IReadOnlyList<CsvRow> rows)
    {
        _path = path;
        _columns = columns;
        Rows = rows;
    }

    public IReadOnlyList<CsvRow> Rows { get; }

    public static CsvTable Read(string path)
    {
        using var lines = File.ReadLines(path).GetEnumerator();
        if (!lines.MoveNext())
        {
            throw GateThrowHelper.EmptyCsv(path);
        }

        var columns = ParseLine(lines.Current, path, 1);
        var rows = new List<CsvRow>();
        var lineNumber = 1;

        while (lines.MoveNext())
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(lines.Current))
            {
                continue;
            }

            var values = ParseLine(lines.Current, path, lineNumber);
            if (values.Length != columns.Length)
            {
                throw GateThrowHelper.InvalidCsv(path, lineNumber);
            }

            rows.Add(new CsvRow(path, columns, values));
        }

        return new CsvTable(path, columns, rows);
    }

    public void RequireColumn(string column)
    {
        if (!_columns.Contains(column, StringComparer.Ordinal))
        {
            throw GateThrowHelper.MissingColumn(_path, column);
        }
    }

    private static string[] ParseLine(string line, string path, int lineNumber)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var state = CsvState.FieldStart;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            switch (state)
            {
                case CsvState.FieldStart when character == '"':
                    state = CsvState.Quoted;
                    break;

                case CsvState.FieldStart when character == ',':
                    values.Add(string.Empty);
                    break;

                case CsvState.FieldStart:
                    value.Append(character);
                    state = CsvState.Unquoted;
                    break;

                case CsvState.Unquoted when character == ',':
                    values.Add(value.ToString());
                    value.Clear();
                    state = CsvState.FieldStart;
                    break;

                case CsvState.Unquoted when character == '"':
                    throw GateThrowHelper.InvalidCsv(path, lineNumber);

                case CsvState.Unquoted:
                    value.Append(character);
                    break;

                case CsvState.Quoted when character == '"'
                    && index + 1 < line.Length
                    && line[index + 1] == '"':
                    value.Append('"');
                    index++;
                    break;

                case CsvState.Quoted when character == '"':
                    state = CsvState.AfterQuote;
                    break;

                case CsvState.Quoted:
                    value.Append(character);
                    break;

                case CsvState.AfterQuote when character == ',':
                    values.Add(value.ToString());
                    value.Clear();
                    state = CsvState.FieldStart;
                    break;

                case CsvState.AfterQuote:
                    throw GateThrowHelper.InvalidCsv(path, lineNumber);
            }
        }

        if (state == CsvState.Quoted)
        {
            throw GateThrowHelper.InvalidCsv(path, lineNumber);
        }

        values.Add(value.ToString());
        return values.ToArray();
    }

    private enum CsvState
    {
        FieldStart,
        Unquoted,
        Quoted,
        AfterQuote
    }
}

internal sealed class CsvRow
{
    private readonly string _path;
    private readonly Dictionary<string, string> _values;

    public CsvRow(string path, IReadOnlyList<string> columns, IReadOnlyList<string> values)
    {
        _path = path;
        _values = new Dictionary<string, string>(columns.Count, StringComparer.Ordinal);

        for (var index = 0; index < columns.Count; index++)
        {
            _values.Add(columns[index], values[index]);
        }
    }

    public string this[string column]
        => _values.TryGetValue(column, out var value)
            ? value
            : throw GateThrowHelper.MissingColumn(_path, column);
}
