using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HrDepartment
{
    public class EventService
    {
        private readonly string _cs;

        public EventService(string connectionString)
        {
            _cs = connectionString;
        }

        public List<EventType> GetEventTypes()
        {
            var list = new List<EventType>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT id_event_type, Название FROM тип_события ORDER BY Название", conn);

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

        public void AddEvent(long employeeId, DateTime start, DateTime end, long eventTypeId, string? reason)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            // В таблице есть колонка с пробелом: `Дата окончания`
            using var cmd = new MySqlCommand(@"
                INSERT INTO событие (id_сотрудника, `Дата_начала`, `Дата окончания`, `Обоснование`, id_типа_события)
                VALUES (@emp, @start, @end, @reason, @type);", conn);

            cmd.Parameters.AddWithValue("@emp", employeeId);
            cmd.Parameters.AddWithValue("@start", start.Date);
            cmd.Parameters.AddWithValue("@end", end.Date);
            cmd.Parameters.AddWithValue("@reason", (object?)reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@type", eventTypeId);

            cmd.ExecuteNonQuery();

            // Пересчёт флага "В_отпуске"
            UpdateVacationFlag(conn, employeeId);
        }

        public List<EmployeeEvent> GetEvents(DateTime? from, DateTime? to, long? typeId, string? employeeSearch)
        {
            var list = new List<EmployeeEvent>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            var sql = @"
                SELECT 
                    ev.id_event,
                    ev.id_сотрудника,
                    emp.ФИО AS EmployeeName,
                    ev.`Дата_начала`,
                    ev.`Дата окончания`,
                    ev.Обоснование,
                    ev.id_типа_события,
                    t.Название AS EventTypeName
                FROM событие ev
                JOIN сотрудник emp ON emp.id_employee = ev.id_сотрудника
                JOIN тип_события t ON t.id_event_type = ev.id_типа_события
                WHERE 1=1
            ";

            var cmd = new MySqlCommand();
            cmd.Connection = conn;

            if (from.HasValue)
            {
                sql += " AND ev.`Дата_начала` >= @from";
                cmd.Parameters.AddWithValue("@from", from.Value.Date);
            }

            if (to.HasValue)
            {
                sql += " AND ev.`Дата_начала` <= @to";
                cmd.Parameters.AddWithValue("@to", to.Value.Date);
            }

            if (typeId.HasValue)
            {
                sql += " AND ev.id_типа_события = @type";
                cmd.Parameters.AddWithValue("@type", typeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(employeeSearch))
            {
                sql += " AND emp.ФИО LIKE @q";
                cmd.Parameters.AddWithValue("@q", "%" + employeeSearch.Trim() + "%");
            }

            sql += " ORDER BY ev.`Дата_начала` DESC";

            cmd.CommandText = sql;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new EmployeeEvent
                {
                    Id = r.GetInt64("id_event"),
                    EmployeeId = r.GetInt64("id_сотрудника"),
                    EmployeeName = r.GetString("EmployeeName"),
                    StartDate = r.GetDateTime("Дата_начала"),
                    EndDate = r.GetDateTime("Дата окончания"),
                    Reason = r.IsDBNull(r.GetOrdinal("Обоснование")) ? null : r.GetString("Обоснование"),
                    EventTypeId = r.GetInt64("id_типа_события"),
                    EventTypeName = r.GetString("EventTypeName")
                });
            }

            return list;
        }

        /// <summary>
        /// Автоматический пересчёт флага "В_отпуске" для ВСЕХ сотрудников на текущую дату.
        /// Используется при запуске программы, чтобы статусы всегда были актуальными.
        /// </summary>
        /// 
        public void RecalculateVacationFlagForEmployee(long employeeId)
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            using var cmd = new MySqlCommand(@"
        UPDATE сотрудник s
        SET s.В_отпуске = EXISTS (
            SELECT 1
            FROM событие ev
            WHERE ev.id_сотрудника = s.id_employee
              AND ev.id_типа_события = 3
              AND CURDATE() BETWEEN ev.`Дата_начала` AND ev.`Дата окончания`
        )
        WHERE s.id_employee = @employeeId;", conn);

            cmd.Parameters.AddWithValue("@employeeId", employeeId);
            cmd.ExecuteNonQuery();
        }
        public void RecalculateAllVacationFlags()
        {
            using var conn = new MySqlConnection(_cs);
            conn.Open();

            // 3 = "Отпускной" (как в твоём дампе). Если поменяешь типы — можно заменить на поиск по названию.
            using var tx = conn.BeginTransaction();

            try
            {
                // Сначала сбрасываем всем
                using (var reset = new MySqlCommand("UPDATE сотрудник SET В_отпуске = 0;", conn, tx))
                {
                    reset.ExecuteNonQuery();
                }

                // Затем ставим 1 тем, у кого есть активное отпускное событие
                using (var set = new MySqlCommand(@"
                    UPDATE сотрудник s
                    SET В_отпуске = 1
                    WHERE EXISTS (
                        SELECT 1
                        FROM событие ev
                        WHERE ev.id_сотрудника = s.id_employee
                          AND ev.id_типа_события = 3
                          AND CURDATE() BETWEEN ev.`Дата_начала` AND ev.`Дата окончания`
                    );", conn, tx))
                {
                    set.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private void UpdateVacationFlag(MySqlConnection conn, long employeeId)
        {
            // 3 = "Отпускной" (у тебя так в дампе)
            using var check = new MySqlCommand(@"
                SELECT COUNT(*) 
                FROM событие 
                WHERE id_сотрудника=@id 
                  AND id_типа_события=3
                  AND CURDATE() BETWEEN `Дата_начала` AND `Дата окончания`;", conn);
            check.Parameters.AddWithValue("@id", employeeId);

            long cnt = Convert.ToInt64(check.ExecuteScalar());
            bool isOnVacation = cnt > 0;

            using var upd = new MySqlCommand(
                "UPDATE сотрудник SET В_отпуске=@v WHERE id_employee=@id", conn);
            upd.Parameters.AddWithValue("@v", isOnVacation);
            upd.Parameters.AddWithValue("@id", employeeId);
            upd.ExecuteNonQuery();
        }

        public List<EmployeeEvent> GetEventsByEmployee(long employeeId)
        {
            var list = new List<EmployeeEvent>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            using var cmd = new MySqlCommand(@"
        SELECT 
            ev.id_event,
            ev.id_сотрудника,
            emp.ФИО AS EmployeeName,
            ev.`Дата_начала`,
            ev.`Дата окончания`,
            ev.Обоснование,
            ev.id_типа_события,
            t.Название AS EventTypeName
        FROM событие ev
        JOIN сотрудник emp ON emp.id_employee = ev.id_сотрудника
        JOIN тип_события t ON t.id_event_type = ev.id_типа_события
        WHERE ev.id_сотрудника = @employeeId
        ORDER BY ev.`Дата_начала` DESC, ev.id_event DESC;", conn);

            cmd.Parameters.AddWithValue("@employeeId", employeeId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new EmployeeEvent
                {
                    Id = r.GetInt64("id_event"),
                    EmployeeId = r.GetInt64("id_сотрудника"),
                    EmployeeName = r.GetString("EmployeeName"),
                    StartDate = r.GetDateTime("Дата_начала"),
                    EndDate = r.GetDateTime("Дата окончания"),
                    Reason = r.IsDBNull(r.GetOrdinal("Обоснование")) ? null : r.GetString("Обоснование"),
                    EventTypeId = r.GetInt64("id_типа_события"),
                    EventTypeName = r.GetString("EventTypeName")
                });
            }

            return list;
        }
    }
}
