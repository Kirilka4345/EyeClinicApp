using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    public partial class DoctorEditWindow : Window
    {
        private int _doctorId = -1;

        public DoctorEditWindow(int doctorId = -1)
        {
            InitializeComponent();
            _doctorId = doctorId;

            if (_doctorId > 0)
            {
                txtTitle.Text = "Редактирование врача";
                Title = "Редактирование врача";
                LoadDoctorData();
            }
        }

        private void LoadDoctorData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT * FROM Врачи WHERE КодВрача = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _doctorId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFIO.Text = reader["ФИОВрача"].ToString();
                                string spec = reader["Специализация"].ToString() ?? "";
                                cmbSpec.SelectedIndex = spec.IndexOf("хирург", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
                                txtPhone.Text = reader["Телефон"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            if (string.IsNullOrWhiteSpace(txtFIO.Text))
            {
                txtError.Text = "Ошибка: введите ФИО врача!";
                return;
            }

            if (cmbSpec.SelectedItem == null)
            {
                txtError.Text = "Ошибка: выберите специализацию!";
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string spec = (cmbSpec.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                    if (_doctorId > 0)
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE Врачи SET ФИОВрача=@fio, Специализация=@spec, Телефон=@phone WHERE КодВрача=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _doctorId);
                            cmd.Parameters.AddWithValue("@fio", txtFIO.Text.Trim());
                            cmd.Parameters.AddWithValue("@spec", spec);
                            cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Данные врача обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(
                            "INSERT INTO Врачи (ФИОВрача, Специализация, Телефон) VALUES (@fio, @spec, @phone)", conn))
                        {
                            cmd.Parameters.AddWithValue("@fio", txtFIO.Text.Trim());
                            cmd.Parameters.AddWithValue("@spec", spec);
                            cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Врач добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка БД:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
