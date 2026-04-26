namespace SchoolERP.Services;

/// <summary>
/// Scoped service holding current user session state for Blazor Server
/// </summary>
public class AppState
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CurrentSchoolId { get; set; }
    public string CurrentSchoolName { get; set; } = string.Empty;
    public List<int> AccessibleSchoolIds { get; set; } = new();
    public bool IsAuthenticated => UserId > 0;
    public bool IsSuperAdmin => Role == "SuperAdmin";
    public bool IsSchoolAdmin => Role == "SchoolAdmin" || Role == "SuperAdmin";
    public bool IsAccountant => Role == "Accountant" || IsSchoolAdmin;
    public bool CanCollectFee => Role is "Accountant" or "SchoolAdmin" or "SuperAdmin";
    public bool CanManageStudents => Role is "SchoolAdmin" or "SuperAdmin";
    public bool CanManageUsers => Role == "SuperAdmin";
    public bool CanAccessSettings => Role is "SchoolAdmin" or "SuperAdmin";
    public bool CanAccessReports => Role is "Accountant" or "SchoolAdmin" or "SuperAdmin";

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}
