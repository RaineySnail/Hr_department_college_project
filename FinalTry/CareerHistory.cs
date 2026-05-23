using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class CareerHistory : INotifyPropertyChanged
{
    private int _id;
    private int _employeeId;
    private int _departmentId;
    private string _departmentName = "";
    private int _positionId;
    private string _positionName = "";
    private DateTime _startDate;
    private DateTime? _endDate;
    private decimal _salary;
    private string _note = "";

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public int EmployeeId
    {
        get => _employeeId;
        set { _employeeId = value; OnPropertyChanged(); }
    }

    public int DepartmentId
    {
        get => _departmentId;
        set { _departmentId = value; OnPropertyChanged(); }
    }

    public string DepartmentName
    {
        get => _departmentName;
        set { _departmentName = value; OnPropertyChanged(); }
    }

    public int PositionId
    {
        get => _positionId;
        set { _positionId = value; OnPropertyChanged(); }
    }

    public string PositionName
    {
        get => _positionName;
        set { _positionName = value; OnPropertyChanged(); }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set { _endDate = value; OnPropertyChanged(); }
    }

    public decimal Salary
    {
        get => _salary;
        set { _salary = value; OnPropertyChanged(); }
    }

    public string Note
    {
        get => _note;
        set { _note = value; OnPropertyChanged(); }
    }



    // Свойство для отображения в интерфейсе
    public string PeriodDisplay => EndDate.HasValue
        ? $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}"
        : $"{StartDate:dd.MM.yyyy} - по настоящее время";

    public string CurrentStatus => EndDate.HasValue ? "Завершена" : "Текущая";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
