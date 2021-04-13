using System.Windows;

namespace StrawberryShake.VisualStudio.GUI
{
    /// <summary>
    /// Interaction logic for HttpDetails.xaml
    /// </summary>
    public partial class HttpDetails : Window
    {
        public HttpDetails()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = !string.IsNullOrEmpty(Token.Text) && !string.IsNullOrEmpty(Scheme.Text);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
