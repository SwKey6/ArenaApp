using System.Windows;

namespace ArenaApp
{
    public partial class ContentTypeDialog : Window
    {
        public enum ContentTypeResult
        {
            Media,
            Text,
            Cancel
        }

        public ContentTypeResult Result { get; private set; } = ContentTypeResult.Cancel;

        public ContentTypeDialog()
        {
            InitializeComponent();
        }

        private void MediaButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentTypeResult.Media;
            DialogResult = true;
            Close();
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentTypeResult.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentTypeResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
