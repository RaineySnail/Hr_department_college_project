using System;
using System.IO;
using System.Text.Json;

namespace HrDepartment
{
    public static class DbConfig
    {
        public static string ConnectionString { get; } = LoadConnectionString();

        private static string LoadConnectionString()
        {
            // Ищем appsettings.json рядом с exe
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "appsettings.json");

            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Не найден файл конфигурации: {configPath}");

            string json = File.ReadAllText(configPath);

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) ||
                !cs.TryGetProperty("Default", out var def))
            {
                throw new Exception("В appsettings.json нет ConnectionStrings:Default");
            }

            string? conn = def.GetString();
            if (string.IsNullOrWhiteSpace(conn))
                throw new Exception("ConnectionStrings:Default пустой");

            return conn;
        }
    }
}
