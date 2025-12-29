using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Data;

namespace ArenaApp
{
    public partial class TextInputDialog : Window
    {
        public string LabelText
        {
            get => TextLabel.Text;
            set => TextLabel.Text = value;
        }

        public string TextValue
        {
            get => TextInputBox.Text;
            set => TextInputBox.Text = value;
        }

        public TextInputDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
