using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using StrawberryShake.VisualStudio.Utilities;
using StreamJsonRpc;

namespace StrawberryShake.VisualStudio
{
    [ContentType("graphql")]
    [Export(typeof(ILanguageClient))]
    public partial class GraphQLLanguageClient : ILanguageClient, ILanguageClientCustomMessage2
    {
        private readonly MessageHandler _messageHandler = new MessageHandler();
        private readonly string _rootDirectory;
        private readonly string _languageServer;
        private JsonRpc _rpc;
        
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public GraphQLLanguageClient()
        {
            _rootDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            _languageServer = Path.Combine(_rootDirectory, "Resources", "language-server-win.exe");
        }

        public string Name => "GraphQL Language Server";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public object MiddleLayer { get; }

        public object CustomMessageTarget => _messageHandler;

        public bool ShowNotificationOnInitializeFailed => true;

        public async Task<Connection> ActivateAsync(CancellationToken cancellationToken)
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE));

            if(dte.Solution is { FullName: { Length: > 0 } })
            {
                string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                await BuildServerConfigAsync(solutionDir);
                _messageHandler.RootDirectory = solutionDir;
            }

            await Task.Yield();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _languageServer,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = !Debugger.IsAttached
                }
            };

            if (process.Start())
            {
                process.Exited += Process_Exited;
                return process.CreateConnection(_rootDirectory, Debugger.IsAttached);
            }

            return null;
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            _rpc = rpc;
            return Task.CompletedTask;
        }

        public async Task SendConfigurationHasChangedAsync()
        {
            await _rpc.InvokeWithParameterObjectAsync("workspace/didChangeConfiguration");
        }

        private void Process_Exited(object sender, EventArgs e) => BeginStop();

#pragma warning disable VSTHRD106 // Use InvokeAsync to raise async events
        private void BeginStop() =>
            Task.Run(async () => await StopAsync(this, EventArgs.Empty).ConfigureAwait(false));

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            throw new NotImplementedException();
        }
#pragma warning restore VSTHRD106 // Use InvokeAsync to raise async events
    }
}
