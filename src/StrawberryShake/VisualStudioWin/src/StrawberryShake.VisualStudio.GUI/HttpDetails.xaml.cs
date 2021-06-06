using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace StrawberryShake.VisualStudio.GUI
{
    /// <summary>
    /// Interaction logic for HttpDetails.xaml
    /// </summary>
    public partial class HttpDetails : Window
    {
        private readonly ObservableCollection<HttpHeader> _headers = new();

        public HttpDetails()
        {
            InitializeComponent();
            _headersGrid.ItemsSource = _headers;
        }

        internal IReadOnlyList<HttpHeader> Headers
        {
            get => _headers.ToArray();
            set
            {
                _headers.Clear();

                foreach (HttpHeader header in value)
                {
                    _headers.Add(header);
                }
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
