using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using HotChocolate.Language;
using HotChocolate.Utilities.Introspection;
using StrawberryShake.Tools.Config;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.VisualStudio.GUI
{
    internal class CreateClientViewModel : INotifyPropertyChanged
    {
        private bool _useServerUrl = true;
        private string _serverUrl = "http://localhost:5000/graphql";
        private string _schemaFile;
        private string _clientName = "StarWarsClient";
        private bool _createClientFolder = true;
        private string _accessModifier;
        private bool _useDependencyInjection = true;
        private string _dependencyInjection;
        private bool _useCustomNamespace;
        private string _customNamespace;

        public event PropertyChangedEventHandler PropertyChanged;

        public CreateClientViewModel()
        {
            CreateClientCommand = new DelegateCommand(CreateClient);
        }

        public bool UseServerUrl
        {
            get => _useServerUrl;
            set
            {
                _useServerUrl = value;
                OnPropertyChanged(nameof(UseServerUrl));
                OnPropertyChanged(nameof(UseSchemaFile));
            }
        }

        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                _serverUrl = value;
                OnPropertyChanged(nameof(ServerUrl));
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
            }
        }

        public string SchemaFile
        {
            get => _schemaFile;
            set
            {
                _schemaFile = value;
                OnPropertyChanged(nameof(SchemaFile));
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

        public string ProjectFileName { get; set; }

        public ICommand CreateClientCommand { get; }

        private void CreateClient()
        {
            if(UseSchemaFile && !File.Exists(SchemaFile))
            {
                // todo : signal that schema file is invalid
            }

            string directory = Path.GetDirectoryName(ProjectFileName);

            if(CreateClientFolder)
            {
                directory = Path.Combine(directory, ClientName);
            }

            if(!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var configuration = new GraphQLConfig();

            configuration.Extensions.StrawberryShake.Name = ClientName;

            if (UseCustomNamespace)
            {
                configuration.Extensions.StrawberryShake.Namespace = CustomNamespace;
            }

            // todo : async
            // todo : handle errors
            // todo : auth
            if (UseServerUrl)
            {
                using var client = new HttpClient{ BaseAddress = new Uri(ServerUrl) };
                DocumentNode result = IntrospectionClient.Default.DownloadSchemaAsync(client).Result;
                File.WriteAllText(Path.Combine(directory, Defaults.SchemaFile), result.ToString(true));
                File.WriteAllText(Path.Combine(directory, Defaults.SchemaExtensionFile), Defaults.SchemaExtensionFileContent);
                configuration.Extensions.StrawberryShake.Url = ServerUrl;
            }
            else
            {
                File.Copy(SchemaFile, Path.Combine(directory, Defaults.SchemaFile));
                File.WriteAllText(Path.Combine(directory, Defaults.SchemaExtensionFile), Defaults.SchemaExtensionFileContent);
                configuration.Extensions.StrawberryShake.Url = new Uri("file://" + SchemaFile).ToString();
            }

            configuration.Save(Path.Combine(directory, Defaults.GraphQLConfigFile));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class DelegateCommand : ICommand
        {
            private Action _action;

            public DelegateCommand(Action action)
            {
                _action = action;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
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
