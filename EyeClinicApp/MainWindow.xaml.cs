using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    public partial class MainWindow : Window
    {
        private string _currentView = "Пациенты";
        private DataTable _currentData = new DataTable();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Отображение информации о пользователе
            txtUserInfo.Text = $"{App.CurrentUserName} ({App.CurrentUserRole})";

            // Ограничение функционала для гостя
            if (App.CurrentUserRole == "Гость")
            {
                btnAdd.IsEnabled = false;
                btnEdit.IsEnabled = false;
                btnDelete.IsEnabled = false;
            }

            // Загрузка данных пациентов
            LoadPatients();
        }

        #region Загрузка данных

        /// <summary>
        /// Загрузка списка пациентов
        /// </summary>
        public void LoadPatients()
        {
            _currentView = "Пациенты";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT p.РегНомер, p.ДатаРегистрации, p.ФИОПациента, 
                               p.ДатаРождения, p.Пол, p.Адрес, 
                               d.НаименованиеДиагноза AS Диагноз, 
                               v.ФИОВрача AS [Лечащий врач], 
                               p.НомерПалаты
                        FROM Пациенты p
                        INNER JOIN Диагнозы d ON p.КодДиагноза = d.КодДиагноза
                        INNER JOIN Врачи v ON p.КодВрача = v.КодВрача
                        ORDER BY p.ФИОПациента";

                    var adapter = new SqlDataAdapter(query, conn);
                    _currentData = new DataTable();
                    adapter.Fill(_currentData);

                    dataGrid.Columns.Clear();
                    dataGrid.ItemsSource = _currentData.DefaultView;
                    dataGrid.AutoGenerateColumns = true;

                    // Настройка полей поиска
                    cmbSearchField.Items.Clear();
                    cmbSearchField.Items.Add("ФИО пациента");
                    cmbSearchField.Items.Add("Диагноз");
                    cmbSearchField.Items.Add("Номер палаты");
                    cmbSearchField.Items.Add("ФИО врача");
                    cmbSearchField.SelectedIndex = 0;

                    txtStatus.Text = $"Записей: {_currentData.Rows.Count} | Раздел: Пациенты | Пользователь: {App.CurrentUserName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загрузка списка врачей
        /// </summary>
        private void LoadDoctors()
        {
            _currentView = "Врачи";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT КодВрача, ФИОВрача, Специализация, Телефон FROM Врачи ORDER BY ФИОВрача";
                    var adapter = new SqlDataAdapter(query, conn);
                    _currentData = new DataTable();
                    adapter.Fill(_currentData);

                    dataGrid.Columns.Clear();
                    dataGrid.ItemsSource = _currentData.DefaultView;
                    dataGrid.AutoGenerateColumns = true;

                    cmbSearchField.Items.Clear();
                    cmbSearchField.Items.Add("ФИО врача");
                    cmbSearchField.Items.Add("Специализация");
                    cmbSearchField.SelectedIndex = 0;

                    txtStatus.Text = $"Записей: {_currentData.Rows.Count} | Раздел: Врачи | Пользователь: {App.CurrentUserName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загрузка списка диагнозов
        /// </summary>
        private void LoadDiagnoses()
        {
            _currentView = "Диагнозы";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT КодДиагноза, НаименованиеДиагноза FROM Диагнозы ORDER BY НаименованиеДиагноза";
                    var adapter = new SqlDataAdapter(query, conn);
                    _currentData = new DataTable();
                    adapter.Fill(_currentData);

                    dataGrid.Columns.Clear();
                    dataGrid.ItemsSource = _currentData.DefaultView;
                    dataGrid.AutoGenerateColumns = true;

                    cmbSearchField.Items.Clear();
                    cmbSearchField.Items.Add("Наименование");
                    cmbSearchField.SelectedIndex = 0;

                    txtStatus.Text = $"Записей: {_currentData.Rows.Count} | Раздел: Диагнозы | Пользователь: {App.CurrentUserName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Навигация

        private void BtnPatients_Click(object sender, RoutedEventArgs e)
        {
            LoadPatients();
            HighlightNavButton(btnPatients);
        }

        private void BtnDoctors_Click(object sender, RoutedEventArgs e)
        {
            LoadDoctors();
            HighlightNavButton(btnDoctors);
        }

        private void BtnDiagnoses_Click(object sender, RoutedEventArgs e)
        {
            LoadDiagnoses();
            HighlightNavButton(btnDiagnoses);
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            var reportsWindow = new ReportsWindow();
            reportsWindow.Owner = this;
            reportsWindow.ShowDialog();
        }

        private void HighlightNavButton(Button activeBtn)
        {
            var accentBrush = FindResource("AccentBrush") as System.Windows.Media.SolidColorBrush;
            var primaryBrush = FindResource("PrimaryBrush") as System.Windows.Media.SolidColorBrush;
            
            btnPatients.Background = accentBrush;
            btnDoctors.Background = accentBrush;
            btnDiagnoses.Background = accentBrush;
            btnReports.Background = accentBrush;
            activeBtn.Background = primaryBrush;
        }

        #endregion

        #region CRUD операции

        /// <summary>
        /// Добавление записи
        /// </summary>
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == "Пациенты")
            {
                var patientWindow = new PatientEditWindow();
                patientWindow.Owner = this;
                if (patientWindow.ShowDialog() == true)
                {
                    LoadPatients();
                }
            }
            else if (_currentView == "Врачи")
            {
                var doctorWindow = new DoctorEditWindow();
                doctorWindow.Owner = this;
                if (doctorWindow.ShowDialog() == true)
                {
                    LoadDoctors();
                }
            }
            else if (_currentView == "Диагнозы")
            {
                var diagWindow = new DiagnosisEditWindow();
                diagWindow.Owner = this;
                if (diagWindow.ShowDialog() == true)
                {
                    LoadDiagnoses();
                }
            }
        }

        /// <summary>
        /// Редактирование записи
        /// </summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (dataGrid.SelectedItem as DataRowView)?.Row;
            if (row == null) return;

            if (_currentView == "Пациенты")
            {
                int regNum = Convert.ToInt32(row["РегНомер"]);
                var patientWindow = new PatientEditWindow(regNum);
                patientWindow.Owner = this;
                if (patientWindow.ShowDialog() == true)
                {
                    LoadPatients();
                }
            }
            else if (_currentView == "Врачи")
            {
                int doctorId = Convert.ToInt32(row["КодВрача"]);
                var doctorWindow = new DoctorEditWindow(doctorId);
                doctorWindow.Owner = this;
                if (doctorWindow.ShowDialog() == true)
                {
                    LoadDoctors();
                }
            }
            else if (_currentView == "Диагнозы")
            {
                int diagId = Convert.ToInt32(row["КодДиагноза"]);
                var diagWindow = new DiagnosisEditWindow(diagId);
                diagWindow.Owner = this;
                if (diagWindow.ShowDialog() == true)
                {
                    LoadDiagnoses();
                }
            }
        }

        /// <summary>
        /// Удаление записи
        /// </summary>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Вы действительно хотите удалить выбранную запись?\nЭто действие нельзя отменить!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var row = (dataGrid.SelectedItem as DataRowView)?.Row;
            if (row == null) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "";
                    SqlCommand cmd;

                    if (_currentView == "Пациенты")
                    {
                        query = "DELETE FROM Пациенты WHERE РегНомер = @id";
                        cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", Convert.ToInt32(row["РегНомер"]));
                    }
                    else if (_currentView == "Врачи")
                    {
                        query = "DELETE FROM Врачи WHERE КодВрача = @id";
                        cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", Convert.ToInt32(row["КодВрача"]));
                    }
                    else
                    {
                        query = "DELETE FROM Диагнозы WHERE КодДиагноза = @id";
                        cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", Convert.ToInt32(row["КодДиагноза"]));
                    }

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Запись успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Обновление данных
                if (_currentView == "Пациенты") LoadPatients();
                else if (_currentView == "Врачи") LoadDoctors();
                else LoadDiagnoses();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547) // FK violation
                {
                    MessageBox.Show(
                        "Невозможно удалить запись, так как на неё ссылаются другие записи!\n" +
                        "Сначала удалите связанные записи.",
                        "Ошибка удаления",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка базы данных:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Поиск и фильтрация

        /// <summary>
        /// Поиск по текстовому полю
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentData == null || _currentData.Rows.Count == 0) return;

            string searchText = txtSearch.Text.Trim();
            string searchField = cmbSearchField.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                _currentData.DefaultView.RowFilter = "";
                txtStatus.Text = $"Записей: {_currentData.Rows.Count} | Раздел: {_currentView}";
                return;
            }

            string columnName = "";
            if (_currentView == "Пациенты")
            {
                switch (searchField)
                {
                    case "ФИО пациента": columnName = "ФИОПациента"; break;
                    case "Диагноз": columnName = "Диагноз"; break;
                    case "Номер палаты": columnName = "НомерПалаты"; break;
                    case "ФИО врача": columnName = "[Лечащий врач]"; break;
                }
            }
            else if (_currentView == "Врачи")
            {
                switch (searchField)
                {
                    case "ФИО врача": columnName = "ФИОВрача"; break;
                    case "Специализация": columnName = "Специализация"; break;
                }
            }
            else
            {
                columnName = "НаименованиеДиагноза";
            }

            try
            {
                // Экранирование спецсимволов в строке поиска
                string safeText = searchText.Replace("'", "''").Replace("[", "[[").Replace("]", "]]").Replace("%", "[%]").Replace("*", "[*]");

                if (columnName == "НомерПалаты")
                {
                    _currentData.DefaultView.RowFilter = $"CONVERT({columnName}, 'System.String') LIKE '%{safeText}%'";
                }
                else
                {
                    _currentData.DefaultView.RowFilter = $"{columnName} LIKE '%{safeText}%'";
                }
                txtStatus.Text = $"Найдено: {_currentData.DefaultView.Count} из {_currentData.Rows.Count} | Раздел: {_currentView}";
            }
            catch
            {
                _currentData.DefaultView.RowFilter = "";
            }
        }

        #endregion

        /// <summary>
        /// Обновление данных
        /// </summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            if (_currentView == "Пациенты") LoadPatients();
            else if (_currentView == "Врачи") LoadDoctors();
            else LoadDiagnoses();
        }

        /// <summary>
        /// Выход из учётной записи
        /// </summary>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти из учётной записи?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUserName = null;
                App.CurrentUserRole = null;
                App.CurrentUserId = 0;

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
