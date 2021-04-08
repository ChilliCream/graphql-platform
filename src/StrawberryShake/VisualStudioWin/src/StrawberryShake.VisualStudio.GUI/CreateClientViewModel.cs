using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using HotChocolate.Language;
using HotChocolate.Utilities.Introspection;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.VisualStudio.GUI
{
    internal class CreateClientViewModel : INotifyPropertyChanged
    {
        private static readonly string[] _packages = new string[]
        {
            "StrawberryShake.Transport.Http",
            "StrawberryShake.CodeGeneration.CSharp.Analyzers"
        };

        private bool _useServerUrl = true;
        private string _serverUrl = "http://localhost:5000/graphql";
        private string _accessTokenScheme;
        private string _accessTokenValue;
        private string _schemaFile;
        private string _clientName = "StarWarsClient";
        private bool _createClientFolder = true;
        private string _accessModifier;
        private bool _useDependencyInjection = true;
        private string _dependencyInjection;
        private bool _useCustomNamespace;
        private string _customNamespace;
        private Visibility _progressVisibility = Visibility.Visible;
        private int _progress;
        private int _progressMax = 100;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ClientCreated;
        public event EventHandler Canceled;
        public event EventHandler<Exception> ErrorOccured;

        public CreateClientViewModel()
        {
            CreateClientCommand = new DelegateCommand(CreateClient);
            CancelCommand= new DelegateCommand(() => Canceled(this, EventArgs.Empty));
        }

        public bool UseServerUrl
        {
            get => _useServerUrl;
            set
            {
                _useServerUrl = value;
                OnPropertyChanged(nameof(UseServerUrl));
                OnPropertyChanged(nameof(UseSchemaFile));
                ValidateSchema();
            }
        }

        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                _serverUrl = value;
                OnPropertyChanged(nameof(ServerUrl));
                ValidateSchema();
            }
        }

        public string AccessTokenScheme
        {
            get => _accessTokenScheme;
            set
            {
                _accessTokenScheme = value;
                OnPropertyChanged(nameof(AccessTokenScheme));
            }
        }

        public string AccessTokenValue
        {
            get => _accessTokenValue;
            set
            {
                _accessTokenValue = value;
                OnPropertyChanged(nameof(AccessTokenValue));
            }
        }

        public bool UseSchemaFile
        {
            get => !_useServerUrl;
            set
            {
                _useServerUrl = !value;
                OnPropertyChanged(nameof(UseServerUrl));
                OnPropertyChanged(nameof(UseSchemaFile));
                ValidateSchema();
            }
        }

        public string SchemaFile
        {
            get => _schemaFile;
            set
            {
                _schemaFile = value;
                OnPropertyChanged(nameof(SchemaFile));
                ValidateSchema();
            }
        }

        public string ClientName
        {
            get => _clientName;
            set
            {
                _clientName = value;
                OnPropertyChanged(nameof(ClientName));
            }
        }

        public bool CreateClientFolder
        {
            get => _createClientFolder;
            set
            {
                _createClientFolder = value;
                OnPropertyChanged(nameof(CreateClientFolder));
            }
        }
   
        public string AccessModifier
        {
            get => _accessModifier;
            set
            {
                _accessModifier = value;
                OnPropertyChanged(nameof(AccessModifier));
            }
        }

        public bool UseDependencyInjection
        {
            get => _useDependencyInjection;
            set
            {
                _useDependencyInjection = value;
                OnPropertyChanged(nameof(UseDependencyInjection));
            }
        }

        public string DependencyInjection
        {
            get => _dependencyInjection;
            set
            {
                _dependencyInjection = value;
                OnPropertyChanged(nameof(DependencyInjection));
            }
        }

        public bool UseCustomNamespace
        {
            get => _useCustomNamespace;
            set
            {
                _useCustomNamespace = value;
                OnPropertyChanged(nameof(UseCustomNamespace));
            }
        }

        public string CustomNamespace
        {
            get => _customNamespace;
            set
            {
                _customNamespace = value;
                OnPropertyChanged(nameof(CustomNamespace));
            }
        }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set
            {
                _progressVisibility = value;
                OnPropertyChanged(nameof(ProgressVisibility));
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public int ProgressMax
        {
            get => _progressMax;
            set
            {
                _progressMax = value;
                OnPropertyChanged(nameof(ProgressMax));
            }
        }

        public IProject Project{ get; set; }

        public DelegateCommand CreateClientCommand { get; }

        public DelegateCommand CancelCommand { get; }

        private void CreateClient()
        {
            CancelCommand.Disable();
            CreateClientCommand.Disable();

            ProgressMax = _packages.Length + 8;
            Progress = 1;
            ProgressVisibility = Visibility.Visible;
            UI.AllowUIToUpdate();

            try
            {
                EnsurePackagesAreInstalled();
                Progress++;

                string configuration = CreateConfiguration();
                Progress++;

                if (!TryLoadSchema(out string schema))
                {
                    CancelCommand.Enable();
                    CreateClientCommand.Enable();
                    return;
                }

                Project.SaveFile(CreateFileName(Defaults.GraphQLConfigFile), configuration);
                Progress++;

                Project.SaveFile(CreateFileName(Defaults.SchemaExtensionFile), Defaults.SchemaExtensionFileContent);
                Progress++;

                Project.SaveFile(CreateFileName(Defaults.SchemaFile), schema);
                Progress = ProgressMax;

                ClientCreated(this, EventArgs.Empty);
            }
            finally
            {
                ProgressVisibility = Visibility.Hidden;
            }
        }

        private void EnsurePackagesAreInstalled()
        {
            foreach (string packageId in _packages)
            {                
                Project.EnsurePackageIsInstalled(packageId);
                Progress++;
            }
        }

        private string CreateConfiguration()
        {
            var configuration = new GraphQLConfig();

            configuration.Extensions.StrawberryShake.Name = ClientName;

            if (UseCustomNamespace)
            {
                configuration.Extensions.StrawberryShake.Namespace = CustomNamespace;
            }

            if (UseServerUrl)
            {
                configuration.Extensions.StrawberryShake.Url = ServerUrl;
            }
            else
            {
                configuration.Extensions.StrawberryShake.Url = new Uri("file://" + SchemaFile).ToString();
            }

            return configuration.ToString();
        }

        private bool TryLoadSchema(out string schema)
        {
            try
            {
                if (UseServerUrl)
                {
                    using var client = new HttpClient { BaseAddress = new Uri(ServerUrl) };
                    schema = IntrospectionClient.Default.DownloadSchemaAsync(client).Result.ToString(true);
                    return true;
                }
                else
                {
                    schema = Utf8GraphQLParser.Parse(File.ReadAllText(SchemaFile)).ToString(true);
                    return true;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                ErrorOccured(this, ex);
                schema = null;
                return false;
            }
        }

        private string CreateFileName(string fileName)
        {
            if (CreateClientFolder)
            {
                return Path.Combine(ClientName, fileName);
            }

            return fileName;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ValidateSchema()
        {
            if (UseSchemaFile)
            {
                if(File.Exists(SchemaFile))
                {
                    CreateClientCommand.Enable();
                }
                else
                {
                    CreateClientCommand.Disable();
                }
            }

            if(UseServerUrl)
            {
                if(Uri.TryCreate(ServerUrl, UriKind.Absolute, out Uri uri) &&
                    (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    CreateClientCommand.Enable();
                }
                else
                {
                    CreateClientCommand.Disable();
                }
            }
        }

        internal class DelegateCommand : ICommand
        {
            private Action _action;
            private bool _enabled = true;

            public DelegateCommand(Action action)
            {
                _action = action;
            }

            public event EventHandler CanExecuteChanged;

            public void Enable()
            {
                _enabled = true;
                CanExecuteChanged(this, EventArgs.Empty);
            }

            public void Disable()
            {
                _enabled = false;
                CanExecuteChanged(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                return _enabled;
            }

            public void Execute(object parameter)
            {
                _action();
            }
        }
    }

    public static class Defaults
    {
        public const string SchemaFile = "schema.graphql";

        public const string SchemaExtensionFile = "schema.extensions.graphql";

        public const string SchemaExtensionFileContent = @"scalar _KeyFieldSet

directive @key(fields: _KeyFieldSet!) on SCHEMA | OBJECT

extend schema @key(fields: ""id"")";
        public const string GraphQLConfigFile = ".graphqlrc.json";
    }
}
