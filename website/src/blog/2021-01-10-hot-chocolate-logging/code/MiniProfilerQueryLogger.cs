using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using StackExchange.Profiling;

namespace logging
{
    public class MiniProfilerQueryLogger : DiagnosticEventListener
    {
        private static MiniProfiler? _miniProfiler; // per MiniProfiler example, initalizing not needed.

        // this diagnostic event is raised when a request is executed ...
        public override IActivityScope ExecuteRequest(IRequestContext context)
        {
            // ... we will return an activity scope that is used to signal when the request is
            // finished.
            return new RequestScope(context);
        }

        private class RequestScope : IActivityScope
        {
            private readonly IRequestContext _context;
            private readonly Stopwatch _queryTimer;

            public RequestScope(IRequestContext context)
            {
                _context = context;
                _miniProfiler = MiniProfiler.StartNew("Hot Chocolate GraphQL Query");
                _queryTimer = new Stopwatch();
                _queryTimer.Start();
            }

            public void Dispose()
            {
                _queryTimer.Stop();

                // when the request is finished it will dispose the activity scope and 
                // this is when we print the parsed query.
                var variables = _context.Variables;
                var queryString = _context.Document;

                string htmlText;
                using (MiniProfiler.Current.Ignore()) // this does not seem to ignore as documented
                {
                    htmlText = CreateHtmlFromDocument(queryString, variables);
                }

                _miniProfiler?.AddCustomLink(htmlText, "#");
                _miniProfiler?.Stop();
            }

            private string CreateHtmlFromDocument(DocumentNode? queryString, IVariableValueCollection? variables)
            {
                StringBuilder htmlText = new();
                if (queryString is not null)
                {
                    var divWithBorder =
                        "<div style=\"border: 1px solid black;align-items: flex-start;margin-left: 10%;margin-right: 15%; padding: 5px\">";
                    var lineArray = queryString.ToString(true)
                        .Split(
                            new[] {Environment.NewLine},
                            StringSplitOptions.None
                        ).ToList();
                    htmlText.AppendLine(divWithBorder);
                    htmlText.AppendLine("<b>GraphQL Query</b>");
                    foreach (var s in lineArray)
                    {
                        var str = "<p>" + s.Replace(" ", "&nbsp; ") + "</p>";
                        htmlText.AppendLine(str);
                    }

                    htmlText.AppendLine("</div>");

                    if (variables is not null)
                    {
                        var variablesConcrete = _context.Variables!.ToList();
                        if (variablesConcrete.Count > 0)
                        {
                            htmlText.AppendLine(divWithBorder);
                            htmlText.AppendLine("<b>Variables</b><table>");
                            foreach (var variableValue in variablesConcrete!)
                            {
                                htmlText.Append("<tr>");
                                htmlText.AppendFormat(
                                    $"<td>&nbsp;&nbsp;{variableValue.Name}</td><td>:</td><td>{variableValue.Value}</td><td>:</td><td>{variableValue.Type}</td>");
                                htmlText.Append("</tr>");
                            }

                            htmlText.Append("</table></div>");
                        }
                    }

                    htmlText.AppendFormat(
                        $"Execution time inside query is {_queryTimer.Elapsed.TotalMilliseconds:0.#} milliseconds.");
                    htmlText.AppendLine("</div>");
                }
                return htmlText.ToString();
            }
        }
    }
}