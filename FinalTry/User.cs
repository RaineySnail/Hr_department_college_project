using System.ComponentModel;
using System.Runtime.CompilerServices;

public class User : INotifyPropertyChanged
{
    private int _id;
    private string _username = "";
    private string _password = "";
    private int _roleId;
    private string _roleName = "";
    private string _fullName = "";
    private bool _isActive;
    private long? _employeeId;
    private bool _hasPersonalDataConsent;

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public int RoleId
    {
        get => _roleId;
        set { _roleId = value; OnPropertyChanged(); }
    }

    public string RoleName
    {
        get => _roleName;
        set { _roleName = value; OnPropertyChanged(); }
    }

    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(); }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public long? EmployeeId
    {
        get => _employeeId;
        set { _employeeId = value; OnPropertyChanged(); }
    }

    public bool HasPersonalDataConsent
    {
        get => _hasPersonalDataConsent;
        set { _hasPersonalDataConsent = value; OnPropertyChanged(); }
    }

    public bool IsAdministrator => RoleId == 1;
    public bool IsSelfServiceUser => RoleId == 2;
    public bool IsViewer => RoleId == 3;
    public bool CanEditSelfOnly => IsSelfServiceUser;

    public string StatusDisplay
    {
        get => IsActive ? "Активен" : "Заблокирован";
    }

    public System.Windows.Media.Brush StatusColor
    {
        get => IsActive ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
    }

    // Проверка прав доступа
    public bool CanEditEmployees => IsAdministrator;
    public bool CanDeleteEmployees => IsAdministrator;
    public bool CanManageUsers => IsAdministrator;
    public bool CanViewReports => IsAdministrator || IsViewer;
    public bool CanViewEventsJournal => IsAdministrator || IsViewer;
    public bool CanViewEventTypes => IsAdministrator || IsViewer;
    public bool CanManageEventTypes => IsAdministrator;
    public bool CanManageDepartments => IsAdministrator;
    public bool CanManageVacations => IsAdministrator;
    public bool CanManageCareer => IsAdministrator;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
