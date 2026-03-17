using System;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    public partial class DiagnosisEditWindow : Window
    {
        private int _diagId = -1;

        public DiagnosisEditWindow(int diagId = -1)
        {
            InitializeComponent();
            _diagId = diagId;

            if (_diagId > 0)
            {
                txtTitle.Text = "Редактирование диагноза";
                Title = "Редактирование диагноза";
                LoadData();
            }
        }

        private void LoadData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT НаименованиеДиагноза FROM Диагнозы WHERE КодДиагноза = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _diagId);
                        var result = cmd.ExecuteScalar();
                        if (result != null) txtName.Text = result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                txtError.Text = "Ошибка: введите наименование диагноза!";
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    if (_diagId > 0)
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE Диагнозы SET НаименованиеДиагноза=@name WHERE КодДиагноза=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _diagId);
                            cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Диагноз обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(
                            "INSERT INTO Диагнозы (НаименованиеДиагноза) VALUES (@name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Диагноз добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
