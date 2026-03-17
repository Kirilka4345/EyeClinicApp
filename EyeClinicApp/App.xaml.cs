using System.Windows;

namespace EyeClinicApp
{
    public partial class App : Application
    {
        // Текущий авторизованный пользователь
        public static string? CurrentUserName { get; set; }
        public static string? CurrentUserRole { get; set; }
        public static int CurrentUserId { get; set; }
    }
}
