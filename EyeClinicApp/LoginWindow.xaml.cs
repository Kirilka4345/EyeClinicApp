using System;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtLogin.Focus();
        }

        /// <summary>
        /// Обработчик кнопки "Войти"
        /// </summary>
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            string login = txtLogin.Text.Trim();
            string password = chkShowPassword.IsChecked == true 
                ? txtPasswordVisible.Text 
                : txtPassword.Password;

            // Валидация
            if (string.IsNullOrEmpty(login))
            {
                txtError.Text = "Ошибка: введите логин!";
                txtLogin.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                txtError.Text = "Ошибка: введите пароль!";
                txtPassword.Focus();
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT КодСотрудника, ФИО, Должность FROM Сотрудники " +
                                   "WHERE Логин = @login AND Пароль = @password";
                    
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                App.CurrentUserId = reader.GetInt32(0);
                                App.CurrentUserName = reader.GetString(1);
                                App.CurrentUserRole = reader.GetString(2);

                                MessageBox.Show(
                                    $"Добро пожаловать, {App.CurrentUserName}!\nРоль: {App.CurrentUserRole}",
                                    "Успешная авторизация",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                                var mainWindow = new MainWindow();
                                mainWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                txtError.Text = "Ошибка: неверный логин или пароль!\nПроверьте правильность введённых данных.";
                                txtPassword.Clear();
                                txtPasswordVisible.Clear();
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    $"Ошибка подключения к базе данных:\n{ex.Message}\n\nПроверьте строку подключения в DatabaseHelper.cs",
                    "Ошибка БД",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Непредвиденная ошибка:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Регистрация"
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var regWindow = new RegisterWindow();
            regWindow.Owner = this;
            regWindow.ShowDialog();
        }

        /// <summary>
        /// Обработчик кнопки "Выход"
        /// </summary>
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти из приложения?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Вход как гость
        /// </summary>
        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUserName = "Гость";
            App.CurrentUserRole = "Гость";
            App.CurrentUserId = 0;

            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        /// <summary>
        /// Переключение видимости пароля
        /// </summary>
        private void ChkShowPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (chkShowPassword.IsChecked == true)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Focus();
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Focus();
            }
        }
    }
}
