using System;
using System.IO;
using Microsoft.Data.SqlClient;

namespace EyeClinicApp
{
    /// <summary>
    /// Класс для работы с подключением к базе данных MSSQL
    /// </summary>
    public static class DatabaseHelper
    {
        // Строка подключения по умолчанию (локальный SQL Server Express)
        private static string _connectionString =
            @"Server=KOMPUTER\SQLEXPRESS;Database=EyeClinicDB;Trusted_Connection=True;TrustServerCertificate=True;";

        private static readonly string _configFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection.txt");

        static DatabaseHelper()
        {
            // Попытка загрузить строку подключения из файла конфигурации
            if (File.Exists(_configFile))
            {
                string saved = File.ReadAllText(_configFile).Trim();
                if (!string.IsNullOrEmpty(saved))
                    _connectionString = saved;
            }
        }

        public static string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                try { File.WriteAllText(_configFile, value); } catch { }
            }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
