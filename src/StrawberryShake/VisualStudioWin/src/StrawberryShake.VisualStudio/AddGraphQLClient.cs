using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using StrawberryShake.VisualStudio.GUI;
using Task = System.Threading.Tasks.Task;

namespace StrawberryShake.VisualStudio
{
    /// <summary>
    /// The AddGraphQL client handler.
    /// </summary>
    internal sealed class AddGraphQLClient
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2170a3df-e63b-4e90-a8e9-54bf12d8a112");


        private readonly AsyncPackage _package;
        private readonly IVsPackageInstallerServices _packageInstallerServices;
        private readonly IVsPackageInstaller2 _packageInstaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddGraphQLClient"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AddGraphQLClient(
            AsyncPackage package,
            OleMenuCommandService commandService,
            IVsPackageInstallerServices packageInstallerServices,
            IVsPackageInstaller2 packageInstaller)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _packageInstallerServices = packageInstallerServices ?? throw new ArgumentNullException(nameof(packageInstallerServices));
            _packageInstaller = packageInstaller ?? throw new ArgumentNullException(nameof(packageInstaller));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandID) { Supported = true };
            commandService.AddCommand(menuItem);
        }
       
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddGraphQLClient Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync<IMenuCommandService, OleMenuCommandService>().ConfigureAwait(false);
            IComponentModel componentModel = await package.GetServiceAsync<SComponentModel, IComponentModel>().ConfigureAwait(false);
            IVsPackageInstaller2 packageInstaller = componentModel.GetService<IVsPackageInstaller2>();
            IVsPackageInstallerServices packageInstallerServices = componentModel.GetService<IVsPackageInstallerServices>();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            Instance = new AddGraphQLClient(package, commandService, packageInstallerServices, packageInstaller);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = (EnvDTE.DTE)ServiceProvider.GetService(typeof(EnvDTE.DTE));
            object activeSolutionProject = dte.SelectedItems.Item(1);
            var project = activeSolutionProject as EnvDTE.SelectedItem;

            if (project != null)
            {
                var dialog = new CreateClient();
                dialog.Project = new VisualStudioProject(project.Project, _packageInstallerServices, _packageInstaller);
                dialog.ShowDialog();
            }
        }
    }
}
