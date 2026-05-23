using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HrDepartment
{
    public class EventOnDateReportRow
    {
        public string EmployeeName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string PositionName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
    }

    public class DepartmentCountReportRow
    {
        public string DepartmentName { get; set; } = "";
        public int EmployeesCount { get; set; }
    }

    public class BirthdayReportRow
    {
        public string EmployeeName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string PositionName { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public DateTime UpcomingBirthday { get; set; }
        public int DaysLeft { get; set; }
    }

    public class ReportsService
    {
        private readonly string _cs;

        public ReportsService(string connectionString)
        {
            _cs = connectionString;
        }

        public List<DepartmentCountReportRow> GetEmployeesCountByDepartments()
        {
            var list = new List<DepartmentCountReportRow>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            const string sql = @"
SELECT 
    dep.`Название_подразделения` AS DepartmentName,
    COUNT(emp.id_employee) AS EmployeesCount
FROM структурное_подразделение dep
LEFT JOIN сотрудник emp ON emp.id_подразделения = dep.id_department
GROUP BY dep.id_department, dep.`Название_подразделения`
ORDER BY dep.`Название_подразделения`;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new DepartmentCountReportRow
                {
                    DepartmentName = reader.GetString("DepartmentName"),
                    EmployeesCount = reader.GetInt32("EmployeesCount")
                });
            }

            return list;
        }

        public List<EventOnDateReportRow> GetEmployeesByEventOnDate(DateTime date, long eventTypeId)
        {
            var list = new List<EventOnDateReportRow>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            const string sql = @"
SELECT 
    emp.ФИО AS EmployeeName,
    dep.`Название_подразделения` AS DepartmentName,
    pos.`Название_должности` AS PositionName,
    ev.`Дата_начала` AS StartDate,
    ev.`Дата окончания` AS EndDate,
    ev.Обоснование AS Reason
FROM событие ev
JOIN сотрудник emp ON emp.id_employee = ev.id_сотрудника
JOIN структурное_подразделение dep ON dep.id_department = emp.id_подразделения
JOIN должность pos ON pos.id_position = emp.id_должности
WHERE ev.id_типа_события = @typeId
  AND @d BETWEEN ev.`Дата_начала` AND ev.`Дата окончания`
ORDER BY dep.`Название_подразделения`, emp.ФИО;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@typeId", eventTypeId);
            cmd.Parameters.AddWithValue("@d", date.Date);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EventOnDateReportRow
                {
                    EmployeeName = reader.GetString("EmployeeName"),
                    DepartmentName = reader.GetString("DepartmentName"),
                    PositionName = reader.GetString("PositionName"),
                    StartDate = reader.GetDateTime("StartDate"),
                    EndDate = reader.GetDateTime("EndDate"),
                    Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString("Reason")
                });
            }

            return list;
        }

        public List<BirthdayReportRow> GetUpcomingBirthdays(int daysAhead)
        {
            var list = new List<BirthdayReportRow>();

            using var conn = new MySqlConnection(_cs);
            conn.Open();

            const string sql = @"
SELECT 
    emp.ФИО AS EmployeeName,
    dep.`Название_подразделения` AS DepartmentName,
    pos.`Название_должности` AS PositionName,
    emp.Дата_рождения AS BirthDate
FROM сотрудник emp
LEFT JOIN структурное_подразделение dep ON dep.id_department = emp.id_подразделения
LEFT JOIN должность pos ON pos.id_position = emp.id_должности
WHERE emp.Дата_рождения IS NOT NULL
ORDER BY emp.ФИО;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            DateTime today = DateTime.Today;
            DateTime endDate = today.AddDays(daysAhead);

            while (reader.Read())
            {
                DateTime birthDate = reader.GetDateTime("BirthDate");
                DateTime nextBirthday = new DateTime(today.Year, birthDate.Month, birthDate.Day);

                if (nextBirthday < today)
                    nextBirthday = nextBirthday.AddYears(1);

                if (nextBirthday > endDate)
                    continue;

                list.Add(new BirthdayReportRow
                {
                    EmployeeName = reader.GetStringSafe("EmployeeName"),
                    DepartmentName = reader.GetStringSafe("DepartmentName"),
                    PositionName = reader.GetStringSafe("PositionName"),
                    BirthDate = birthDate,
                    UpcomingBirthday = nextBirthday,
                    DaysLeft = (nextBirthday - today).Days
                });
            }

            return list;
        }
    }
}
