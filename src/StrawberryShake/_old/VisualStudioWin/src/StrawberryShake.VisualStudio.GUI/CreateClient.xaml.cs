using System.Windows;
using Microsoft.Win32;

namespace StrawberryShake.VisualStudio.GUI
{
    public partial class CreateClient : Window
    {
        private CreateClientViewModel _viewModel;

        public CreateClient()
        {
            _viewModel = new CreateClientViewModel();
            InitializeComponent();
            DataContext = _viewModel;
        }

        public string ProjectFileName
        {
            get => _viewModel.ProjectFileName;
            set => _viewModel.ProjectFileName = value;
        }


        private void HttpDetails_Click(object sender, RoutedEventArgs e)
        {

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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
