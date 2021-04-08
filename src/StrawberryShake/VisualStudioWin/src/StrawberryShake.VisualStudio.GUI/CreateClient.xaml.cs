using System.Windows;
using Microsoft.Win32;

namespace StrawberryShake.VisualStudio.GUI
{
    public partial class CreateClient : Window
    {
        private readonly HttpDetails _httpDetails = new HttpDetails();
        private readonly CreateClientViewModel _viewModel;

        public CreateClient()
        {
            _viewModel = new CreateClientViewModel();
            InitializeComponent();
            DataContext = _viewModel;

            _viewModel.Canceled += OnCanceled;
            _viewModel.ClientCreated += OnClientCreated;
        }

        public IProject Project
        {
            get => _viewModel.Project;
            set => _viewModel.Project = value;
        }

        private void HttpDetails_Click(object sender, RoutedEventArgs e)
        {
            if(_httpDetails.ShowDialog() ?? false)
            {
                _viewModel.AccessTokenScheme = _httpDetails.Scheme.Text;
                _viewModel.AccessTokenValue= _httpDetails.Token.Text;
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openSchemaFile = new OpenFileDialog();
            openSchemaFile.Title = "Open Schema File";
            openSchemaFile.Filter = "GraphQL Schema File|*.graphql";
            if (openSchemaFile.ShowDialog() ?? false)
            {
                _viewModel.SchemaFile = openSchemaFile.FileName;
            }
        }

        private void OnClientCreated(object sender, System.EventArgs e)
        {
            DialogResult = true;
        }

        private void OnCanceled(object sender, System.EventArgs e)
        {
            DialogResult = false;
        }
    }
}
