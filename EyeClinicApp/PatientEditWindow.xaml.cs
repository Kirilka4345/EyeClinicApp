using System;
using System.Data;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    public partial class PatientEditWindow : Window
    {
        private int _patientId = -1; // -1 = новый пациент

        public PatientEditWindow(int patientId = -1)
        {
            InitializeComponent();
            _patientId = patientId;

            if (_patientId > 0)
            {
                txtTitle.Text = "Редактирование пациента";
                Title = "Редактирование пациента";
            }

            LoadComboBoxes();

            if (_patientId > 0)
            {
                LoadPatientData();
            }
        }

        /// <summary>
        /// Загрузка справочников в ComboBox
        /// </summary>
        private void LoadComboBoxes()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Диагнозы
                    var diagAdapter = new SqlDataAdapter("SELECT КодДиагноза, НаименованиеДиагноза FROM Диагнозы ORDER BY НаименованиеДиагноза", conn);
                    var diagTable = new DataTable();
                    diagAdapter.Fill(diagTable);
                    cmbDiagnosis.ItemsSource = diagTable.DefaultView;

                    // Врачи
                    var docAdapter = new SqlDataAdapter("SELECT КодВрача, ФИОВрача FROM Врачи ORDER BY ФИОВрача", conn);
                    var docTable = new DataTable();
                    docAdapter.Fill(docTable);
                    cmbDoctor.ItemsSource = docTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загрузка данных пациента для редактирования
        /// </summary>
        private void LoadPatientData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM Пациенты WHERE РегНомер = @id";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _patientId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFIO.Text = reader["ФИОПациента"].ToString();
                                dpBirthDate.SelectedDate = Convert.ToDateTime(reader["ДатаРождения"]);
                                
                                string gender = reader["Пол"].ToString() ?? "";
                                cmbGender.SelectedIndex = gender == "Мужской" ? 0 : 1;
                                
                                txtAddress.Text = reader["Адрес"].ToString();
                                cmbDiagnosis.SelectedValue = Convert.ToInt32(reader["КодДиагноза"]);
                                cmbDoctor.SelectedValue = Convert.ToInt32(reader["КодВрача"]);
                                txtWard.Text = reader["НомерПалаты"].ToString();
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

        /// <summary>
        /// Сохранение данных пациента
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            // Валидация
            if (string.IsNullOrWhiteSpace(txtFIO.Text))
            {
                txtError.Text = "Ошибка: введите ФИО пациента!";
                txtFIO.Focus();
                return;
            }

            if (dpBirthDate.SelectedDate == null)
            {
                txtError.Text = "Ошибка: выберите дату рождения!";
                return;
            }

            if (dpBirthDate.SelectedDate > DateTime.Now)
            {
                txtError.Text = "Ошибка: дата рождения не может быть в будущем!";
                return;
            }

            if (cmbGender.SelectedItem == null)
            {
                txtError.Text = "Ошибка: выберите пол!";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                txtError.Text = "Ошибка: введите адрес!";
                txtAddress.Focus();
                return;
            }

            if (cmbDiagnosis.SelectedValue == null)
            {
                txtError.Text = "Ошибка: выберите диагноз!";
                return;
            }

            if (cmbDoctor.SelectedValue == null)
            {
                txtError.Text = "Ошибка: выберите лечащего врача!";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtWard.Text) || !int.TryParse(txtWard.Text, out int ward))
            {
                txtError.Text = "Ошибка: введите корректный номер палаты (число)!";
                txtWard.Focus();
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    if (_patientId > 0)
                    {
                        // Обновление
                        string query = @"UPDATE Пациенты SET 
                            ФИОПациента = @fio, ДатаРождения = @birthDate, 
                            Пол = @gender, Адрес = @address, 
                            КодДиагноза = @diagId, КодВрача = @docId, 
                            НомерПалаты = @ward
                            WHERE РегНомер = @id";

                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _patientId);
                            cmd.Parameters.AddWithValue("@fio", txtFIO.Text.Trim());
                            cmd.Parameters.AddWithValue("@birthDate", dpBirthDate.SelectedDate);
                            cmd.Parameters.AddWithValue("@gender", (cmbGender.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString());
                            cmd.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
                            cmd.Parameters.AddWithValue("@diagId", cmbDiagnosis.SelectedValue);
                            cmd.Parameters.AddWithValue("@docId", cmbDoctor.SelectedValue);
                            cmd.Parameters.AddWithValue("@ward", ward);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Данные пациента успешно обновлены!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Добавление
                        string query = @"INSERT INTO Пациенты 
                            (ДатаРегистрации, ФИОПациента, ДатаРождения, Пол, Адрес, КодДиагноза, КодВрача, НомерПалаты)
                            VALUES (@regDate, @fio, @birthDate, @gender, @address, @diagId, @docId, @ward)";

                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@regDate", DateTime.Now.Date);
                            cmd.Parameters.AddWithValue("@fio", txtFIO.Text.Trim());
                            cmd.Parameters.AddWithValue("@birthDate", dpBirthDate.SelectedDate);
                            cmd.Parameters.AddWithValue("@gender", (cmbGender.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString());
                            cmd.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
                            cmd.Parameters.AddWithValue("@diagId", cmbDiagnosis.SelectedValue);
                            cmd.Parameters.AddWithValue("@docId", cmbDoctor.SelectedValue);
                            cmd.Parameters.AddWithValue("@ward", ward);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Пациент успешно добавлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных:\n{ex.Message}", "Ошибка БД",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
