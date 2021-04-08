using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using StrawberryShake.VisualStudio.GUI;

namespace StrawberryShake.VisualStudio
{
    public class VisualStudioProject : IProject
    {
        private readonly Project _project;
        private readonly IVsPackageInstallerServices _packageInstallerServices;
        private readonly IVsPackageInstaller2 _packageInstaller;

        public VisualStudioProject(
            Project project,
            IVsPackageInstallerServices packageInstallerServices,
            IVsPackageInstaller2 packageInstaller)
        {
            _project = project ?? throw new System.ArgumentNullException(nameof(project));
            _packageInstallerServices = packageInstallerServices ?? throw new System.ArgumentNullException(nameof(packageInstallerServices));
            _packageInstaller = packageInstaller ?? throw new System.ArgumentNullException(nameof(packageInstaller));

            ThreadHelper.ThrowIfNotOnUIThread();
            FileName = _project.FullName;
        }

        public string FileName { get; }

        public string DirectoryName => Path.GetDirectoryName(FileName);

        public void EnsurePackageIsInstalled(string packageId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_packageInstallerServices.IsPackageInstalled(_project, packageId))
            {
                try
                {
                    _packageInstaller.InstallLatestPackage(
                        "https://api.nuget.org/v3/index.json",
                        _project,
                        packageId,
                        includePrerelease: false,
                        ignoreDependencies: false);
                }
                catch
                {
                    // we ignore any error here.
                }
            }
        }

        public void SaveFile(string fileName, string content)
        {
            string fullFileName = Path.Combine(DirectoryName, fileName);
            string fileDirectoryName = Path.GetDirectoryName(fullFileName);

            if(!Directory.Exists(fileDirectoryName))
            {
                Directory.CreateDirectory(fileDirectoryName);
            }

            if(File.Exists(fullFileName))
            {
                File.Delete(fullFileName);
            }

            File.WriteAllText(fullFileName, content);
        }
    }
}
