using System;
using System.Linq;
using HrDepartment;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

public class Employee : INotifyPropertyChanged
{
    private int _id;
    private string _fullName = "";
    private string _address = "";
    private DateTime _birthDate;
    private string _personalPhone = "";
    private string _workPhone = "";
    private string _office = "";
    private int _departmentId;
    private int _positionId;
    private string _email = "";
    private string _birthDateString = "";
    private bool _isOnVacation;
    private string _departmentName = "";
    private string _positionName = "";

    private string _gender = "";
    private string _placeOfBirth = "";
    private string _passportData = "";
    private string _snils = "";
    private string _inn = "";
    private string _maritalStatus = "";
    private string _citizenship = "";
    private string _education = "";
    private DateTime? _employmentDate;
    private string _note = "";
    private decimal _currentSalary;

    private ObservableCollection<CareerHistory> _careerHistory = new ObservableCollection<CareerHistory>();

    private ObservableCollection<EmployeeEvent> _events = new ObservableCollection<EmployeeEvent>();

    public string VacationStatusDisplay => IsOnVacation ? "В отпуске" : "Не в отпуске";
    public ObservableCollection<EmployeeEvent> Events
    {
        get => _events;
        set
        {
            _events = value;
            OnPropertyChanged();
        }
    }

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(); }
    }

    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }

    public DateTime BirthDate
    {
        get => _birthDate;
        set
        {
            _birthDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BirthDateDisplay));
        }
    }

    public string BirthDateString
    {
        get => string.IsNullOrWhiteSpace(_birthDateString)
            ? (BirthDate == default ? "" : BirthDate.ToString("yyyy-MM-dd"))
            : _birthDateString;
        set
        {
            _birthDateString = value;
            OnPropertyChanged();

            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                BirthDate = date;
            }
        }
    }

    public string BirthDateDisplay => BirthDate == default ? "—" : BirthDate.ToString("dd.MM.yyyy");

    public string PersonalPhone
    {
        get => _personalPhone;
        set { _personalPhone = value; OnPropertyChanged(); }
    }

    public string WorkPhone
    {
        get => _workPhone;
        set { _workPhone = value; OnPropertyChanged(); }
    }

    public string Office
    {
        get => _office;
        set { _office = value; OnPropertyChanged(); }
    }

    public int DepartmentId
    {
        get => _departmentId;
        set { _departmentId = value; OnPropertyChanged(); }
    }

    public int PositionId
    {
        get => _positionId;
        set { _positionId = value; OnPropertyChanged(); }
    }

    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    public bool IsOnVacation
    {
        get => _isOnVacation;
        set
        {
            _isOnVacation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VacationStatusDisplay));
        }
    }

    public string DepartmentName
    {
        get => _departmentName;
        set { _departmentName = value; OnPropertyChanged(); }
    }

    public string PositionName
    {
        get => _positionName;
        set { _positionName = value; OnPropertyChanged(); }
    }

    public string Gender
    {
        get => _gender;
        set { _gender = value; OnPropertyChanged(); }
    }

    public string PlaceOfBirth
    {
        get => _placeOfBirth;
        set { _placeOfBirth = value; OnPropertyChanged(); }
    }

    public string PassportData
    {
        get => _passportData;
        set { _passportData = value; OnPropertyChanged(); }
    }

    public string SNILS
    {
        get => _snils;
        set { _snils = value; OnPropertyChanged(); }
    }

    public string INN
    {
        get => _inn;
        set { _inn = value; OnPropertyChanged(); }
    }

    public string MaritalStatus
    {
        get => _maritalStatus;
        set { _maritalStatus = value; OnPropertyChanged(); }
    }

    public string Citizenship
    {
        get => _citizenship;
        set { _citizenship = value; OnPropertyChanged(); }
    }

    public string Education
    {
        get => _education;
        set { _education = value; OnPropertyChanged(); }
    }

    public DateTime? EmploymentDate
    {
        get => _employmentDate;
        set
        {
            _employmentDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EmploymentDateDisplay));
        }
    }

    public string EmploymentDateDisplay => EmploymentDate?.ToString("dd.MM.yyyy") ?? "—";

    public string Note
    {
        get => _note;
        set { _note = value; OnPropertyChanged(); }
    }

    public decimal CurrentSalary
    {
        get => _currentSalary;
        set
        {
            _currentSalary = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentSalaryDisplay));
        }
    }

    public string CurrentSalaryDisplay => CurrentSalary <= 0 ? "—" : $"{CurrentSalary:N2} ₽";

    public string CurrentPosition
    {
        get
        {
            var currentJob = CareerHistory?.FirstOrDefault(ch => ch.EndDate == null);
            return currentJob?.PositionName ?? PositionName;
        }
    }

    public string CurrentDepartment
    {
        get
        {
            var currentJob = CareerHistory?.FirstOrDefault(ch => ch.EndDate == null);
            return currentJob?.DepartmentName ?? DepartmentName;
        }
    }

    public ObservableCollection<CareerHistory> CareerHistory
    {
        get => _careerHistory;
        set
        {
            _careerHistory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentPosition));
            OnPropertyChanged(nameof(CurrentDepartment));
        }
    }

    public void LoadEvents(string connectionString)
    {
        try
        {
            var service = new HrDepartment.EventService(connectionString);
            var items = service.GetEventsByEmployee(Id);

            Events = new ObservableCollection<EmployeeEvent>(items);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки событий сотрудника: {ex.Message}");
        }
    }
    public void LoadCareerHistory(string connectionString)
    {
        CareerHistory = new ObservableCollection<CareerHistory>();

        try
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"SELECT 
                                h.id_записи,
                                h.id_сотрудника,
                                h.id_отдела,
                                h.id_должности,
                                h.дата_начала,
                                h.дата_окончания,
                                h.зарплата,
                                h.примечание,
                                d.Название_подразделения AS DepartmentName,
                                p.Название_должности AS PositionName
                             FROM история_карьеры h
                             LEFT JOIN структурное_подразделение d ON h.id_отдела = d.id_department
                             LEFT JOIN должность p ON h.id_должности = p.id_position
                             WHERE h.id_сотрудника = @employeeId
                             ORDER BY h.дата_начала DESC, h.id_записи DESC;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@employeeId", Id);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                CareerHistory.Add(new CareerHistory
                {
                    Id = reader.GetInt32("id_записи"),
                    EmployeeId = reader.GetInt32("id_сотрудника"),
                    DepartmentId = reader.GetInt32("id_отдела"),
                    PositionId = reader.GetInt32("id_должности"),
                    DepartmentName = reader.GetStringSafe("DepartmentName"),
                    PositionName = reader.GetStringSafe("PositionName"),
                    StartDate = reader.GetDateTime("дата_начала"),
                    EndDate = reader.IsDBNull(reader.GetOrdinal("дата_окончания"))
                        ? null
                        : reader.GetDateTime("дата_окончания"),
                    Salary = reader.IsDBNull(reader.GetOrdinal("зарплата"))
                        ? 0
                        : reader.GetDecimal("зарплата"),
                    Note = reader.GetStringSafe("примечание")
                });
            }

            var currentJob = CareerHistory.FirstOrDefault(ch => ch.EndDate == null)
                             ?? CareerHistory.OrderByDescending(ch => ch.StartDate).FirstOrDefault();

            if (currentJob != null)
            {
                CurrentSalary = currentJob.Salary;
                DepartmentName = string.IsNullOrWhiteSpace(currentJob.DepartmentName) ? DepartmentName : currentJob.DepartmentName;
                PositionName = string.IsNullOrWhiteSpace(currentJob.PositionName) ? PositionName : currentJob.PositionName;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки истории карьеры: {ex.Message}");
        }
    }

    public override string ToString() => $"{FullName} | {Email} | Тел.: {PersonalPhone}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}