using System;
using System.Windows;
using Microsoft.Win32;

namespace StrawberryShake.VisualStudio.GUI
{
    public partial class CreateClient : Window
    {
        private readonly CreateClientViewModel _viewModel;

        public CreateClient()
        {
            _viewModel = new CreateClientViewModel();
            InitializeComponent();
            DataContext = _viewModel;

            _viewModel.Canceled += OnCanceled;
            _viewModel.ClientCreated += OnClientCreated;
            _viewModel.ErrorOccured += OnError;
        }

        public IProject Project
        {
            get => _viewModel.Project;
            set => _viewModel.Project = value;
        }

        private void HttpDetails_Click(object sender, RoutedEventArgs e)
        {
            HttpDetails httpDetails = new HttpDetails();
            httpDetails.Scheme.Text = _viewModel.AccessTokenScheme ?? "bearer";
            httpDetails.Token.Text = _viewModel.AccessTokenValue;

            if (httpDetails.ShowDialog() ?? false)
            {
                _viewModel.AccessTokenScheme = httpDetails.Scheme.Text;
                _viewModel.AccessTokenValue = httpDetails.Token.Text;
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

        private void OnCanceled(object sender, System.EventArgs e) => DialogResult = false;

        private void OnError(object sender, Exception e)
        {            
            Progress.Visibility = Visibility.Hidden;
            UI.AllowUIToUpdate();

            if(e is AggregateException a)
            {
                e = a.InnerException;
            }

            MessageBox.Show(e.Message);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Progress.Visibility = Visibility.Visible;
            UI.AllowUIToUpdate();
        }
    }
}
