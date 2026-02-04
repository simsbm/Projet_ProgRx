using System.Windows;
using System.Windows.Controls;

namespace NetAdmin.Server
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer les contrôles du XAML
            var usernameBox = this.FindName("UsernameTextBox") as TextBox;
            var passwordBox = this.FindName("PasswordBox") as PasswordBox;
            var errorMessage = this.FindName("ErrorMessage") as TextBlock;
            var loginButton = this.FindName("LoginButton") as Button;

            if (usernameBox == null || passwordBox == null || errorMessage == null)
                return;

            var username = usernameBox.Text ?? "";
            var password = passwordBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage.Text = "Veuillez entrer un nom d'utilisateur";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage.Text = "Veuillez entrer un mot de passe";
                return;
            }

            bool isValid = (username, password) switch
            {
                ("admin", "Admin@123!") => true,
                ("supervisor", "Supervisor@123!") => true,
                ("operator", "Operator@123!") => true,
                ("viewer", "Viewer@123!") => true,
                _ => false
            };

            if (isValid)
            {
                errorMessage.Text = "Connexion réussie!";
                try
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    errorMessage.Text = $"Erreur: {ex.Message}";
                }
            }
            else
            {
                errorMessage.Text = "Identifiants invalides";
            }
        }
    }
}


