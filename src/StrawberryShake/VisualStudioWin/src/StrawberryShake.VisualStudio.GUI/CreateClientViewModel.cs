using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using StrawberryShake.Tools;

namespace StrawberryShake.VisualStudio.GUI
{
    internal class CreateClientViewModel : INotifyPropertyChanged
    {
        private bool _useServerUrl = true;
        private string _serverUrl = "http://localhost:5000";
        private string _schemaFile;
        private string _clientName = "StarWars";
        private bool _useHttpTransport = true;
        private bool _useGrpcTransport = false;
        private string _accessModifier;
        private bool _useDependencyInjection = true;
        private string _dependencyInjection;
        private bool _useCustomNamespace;
        private string _customNamespace;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public bool UseHttpTransport
        {
            get => _useHttpTransport;
            set
            {
                _useHttpTransport = value;
                OnPropertyChanged(nameof(UseHttpTransport));
            }
        }

        public bool UseGrpcTransport
        {
            get => _useGrpcTransport;
            set
            {
                _useGrpcTransport = value;
                OnPropertyChanged(nameof(UseGrpcTransport));
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

        public async Task CreateClient()
        {
            string path = Path.Combine(
                Path.GetDirectoryName(ProjectFileName),
                ClientName);

            Directory.CreateDirectory(path);

            InitCommandContext context = UseServerUrl
                ? new InitCommandContext(ClientName, path, new Uri(ServerUrl), null, null)
                : new InitCommandContext(ClientName, path, null, null);

            InitCommandHandler initCommandHandler =
                CommandTools.CreateHandler<InitCommandHandler>(false);
            // await initCommandHandler.ExecuteAsync(context);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
