using System;

namespace HrDepartment
{
    public class EmployeeEvent
    {
        public long Id { get; set; }
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public long EventTypeId { get; set; }
        public string EventTypeName { get; set; } = "";

        public string? Reason { get; set; }
    }
}
