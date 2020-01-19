using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StrawberryShake.VisualStudio.GUI
{
    /// <summary>
    /// Interaction logic for CreateClient.xaml
    /// </summary>
    public partial class CreateClient : Window
    {
        public CreateClient()
        {
            InitializeComponent();
        }

        public void ShowMe() => ShowDialog();
    }
}
