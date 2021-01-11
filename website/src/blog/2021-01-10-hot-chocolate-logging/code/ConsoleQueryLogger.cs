using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

namespace logging
{
    public class ConsoleQueryLogger : DiagnosticEventListener
    {
        private static readonly Stopwatch QueryTimer = new();
        private readonly ILogger<ConsoleQueryLogger> _logger;

        public ConsoleQueryLogger(ILogger<ConsoleQueryLogger> logger)
        {
            _logger = logger;
        }

        // this diagnostic event is raised when a request is executed ...
        public override IActivityScope ExecuteRequest(IRequestContext context)
        {
            // ... we will return an activity scope that is used to signal when the request is
            // finished.
            return new RequestScope(_logger, context);
        }

        private class RequestScope : IActivityScope
        {
            private readonly IRequestContext _context;
            private readonly ILogger<ConsoleQueryLogger> _logger;

            public RequestScope(ILogger<ConsoleQueryLogger> logger, IRequestContext context)
            {
                _logger = logger;
                _context = context;
                QueryTimer.Start();
            }

            public void Dispose()
            {
                // when the request is finished it will dispose the activity scope and 
                // this is when we print the parsed query.
                if (_context.Document is not null)
                {
                    // we just need to do a ToString on the Document which represents the parsed
                    // GraphQL request document.
                    StringBuilder stringBuilder = new(_context.Document.ToString(true));
                    stringBuilder.AppendLine();

                    if (_context.Variables != null)
                    {
                        var variablesConcrete = _context.Variables!.ToList();
                        if (variablesConcrete.Count > 0)
                        {
                            stringBuilder.AppendFormat($"Variables {Environment.NewLine}");
                            foreach (var variableValue in _context.Variables!)
                            {
                                stringBuilder.AppendFormat(
                                    $"  {variableValue.Name}{"".PadRight(20 - variableValue.Name.Value.Length)} :  {variableValue.Value}{"".PadRight(20 - variableValue.Value.ToString().Length)}: {variableValue.Type}");
                                stringBuilder.AppendFormat($"{Environment.NewLine}");
                            }
                        }
                    }

                    QueryTimer.Stop();
                    stringBuilder.AppendFormat(
                        $"Ellapsed time for query is {QueryTimer.Elapsed.TotalMilliseconds:0.#} milliseconds.");
                    _logger.LogInformation(stringBuilder.ToString());
                }
            }
        }
    }
}