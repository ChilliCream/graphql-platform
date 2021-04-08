namespace StrawberryShake.VisualStudio.GUI
{
    public interface IProject
    {
        string FileName { get; }

        string DirectoryName { get; }

        void EnsurePackageIsInstalled(string packageId);

        void SaveFile(string fileName, string content);
    }
}
