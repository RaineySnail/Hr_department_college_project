using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HrDepartment
{
    public class EventTypeService
    {
        private readonly string _cs;

        public EventTypeService(string connectionString)
        {
            _cs = connectionString;
        }

        public List<EventType> GetAll()
        {
            var list = new List<EventType>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT id_event_type, Название FROM тип_события ORDER BY Название;", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new EventType
                {
                    Id = r.GetInt64("id_event_type"),
                    Name = r.GetString("Название")
                });
            }

            return list;
        }

        public void Add(string name)
        {
            ValidateName(name);

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            using var cmd = new MySqlCommand(
                "INSERT INTO тип_события (Название) VALUES (@name);", conn);

            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.ExecuteNonQuery();
        }

        public void Update(long id, string name)
        {
            ValidateName(name);

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            using var cmd = new MySqlCommand(
                "UPDATE тип_события SET Название=@name WHERE id_event_type=@id;", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            // Защита: нельзя удалить тип, если есть события с таким типом
            using var check = new MySqlCommand(
                "SELECT COUNT(*) FROM событие WHERE id_типа_события=@id;", conn);
            check.Parameters.AddWithValue("@id", id);

            long cnt = Convert.ToInt64(check.ExecuteScalar());
            if (cnt > 0)
                throw new Exception("Нельзя удалить тип события: он используется в журнале событий.");

            using var cmd = new MySqlCommand(
                "DELETE FROM тип_события WHERE id_event_type=@id;", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Название не может быть пустым.");

            if (name.Trim().Length < 2)
                throw new Exception("Название слишком короткое.");
        }
    }
}
