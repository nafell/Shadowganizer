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

namespace RecordingsOrganiser
{
    /// <summary>
    /// Interaction logic for DeleteConfirmationDialog.xaml
    /// </summary>
    public partial class DeleteConfirmationDialog : Window
    {
        public DeleteConfirmationDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        private void Delete()
        {
            DialogResult = true;
            Close();
        }

        private void Cancel()
        {
            DialogResult = false;
        }
    }
}
