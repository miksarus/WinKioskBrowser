using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WebViewer
{
    /// <summary>
    /// Interaction logic for EnterPasswordWindow.xaml
    /// </summary>
    public partial class EnterPasswordWindow : Window
    {
        private MainWindow m_parent;

        public EnterPasswordWindow(MainWindow owner)
        {
            InitializeComponent();

            this.Owner = owner;
            m_parent = owner;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if(!m_parent.CheckPassword(pbPassword.Password)) {
                text.Text = "Неверный пароль! Попробуйте ещё раз.";
                text.Foreground = Brushes.Red;
                pbPassword.Clear();
                return;
            }

            this.DialogResult = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pbPassword.Focusable = true;
            pbPassword.Focus();
            Keyboard.Focus(pbPassword);
        }

        private void pbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Apply_Click(sender, e);
        }
    }
}
