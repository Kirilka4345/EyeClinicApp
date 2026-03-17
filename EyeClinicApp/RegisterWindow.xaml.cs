using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            cmbRole.SelectedIndex = 0;
            txtFIO.Focus();
        }

        /// <summary>
        /// Обработчик кнопки "Зарегистрировать"
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            string fio = txtFIO.Text.Trim();
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;
            string passwordConfirm = txtPasswordConfirm.Password;
            string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            // Валидация
            if (string.IsNullOrEmpty(fio))
            {
                txtError.Text = "Ошибка: введите ФИО!";
                txtFIO.Focus();
                return;
            }

            if (string.IsNullOrEmpty(login))
            {
                txtError.Text = "Ошибка: введите логин!";
                txtLogin.Focus();
                return;
            }

            if (login.Length < 3)
            {
                txtError.Text = "Ошибка: логин должен содержать минимум 3 символа!";
                txtLogin.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                txtError.Text = "Ошибка: введите пароль!";
                txtPassword.Focus();
                return;
            }

            if (password.Length < 6)
            {
                txtError.Text = "Ошибка: пароль должен содержать минимум 6 символов!";
                txtPassword.Focus();
                return;
            }

            if (password != passwordConfirm)
            {
                txtError.Text = "Ошибка: пароли не совпадают!";
                txtPasswordConfirm.Focus();
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Проверка уникальности логина
                    string checkQuery = "SELECT COUNT(*) FROM Сотрудники WHERE Логин = @login";
                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            txtError.Text = "Ошибка: пользователь с таким логином уже существует!\nВыберите другой логин.";
                            txtLogin.Focus();
                            return;
                        }
                    }

                    // Регистрация
                    string insertQuery = "INSERT INTO Сотрудники (Логин, Пароль, ФИО, Должность) " +
                                         "VALUES (@login, @password, @fio, @role)";
                    using (var insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@login", login);
                        insertCmd.Parameters.AddWithValue("@password", password);
                        insertCmd.Parameters.AddWithValue("@fio", fio);
                        insertCmd.Parameters.AddWithValue("@role", role);
                        insertCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show(
                        $"Пользователь {fio} успешно зарегистрирован!\nТеперь вы можете войти в систему.",
                        "Регистрация завершена",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    this.Close();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    $"Ошибка базы данных:\n{ex.Message}",
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
