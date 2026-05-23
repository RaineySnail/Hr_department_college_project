using System;
using System.IO;

namespace HrDepartment
{
    public static class Logger
    {
        private static string path = "logs.txt";

        public static void Log(string text)
        {
            try
            {
                File.AppendAllText(path,
                    $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {text}\n");
            }
            catch { }
        }
    }
}
