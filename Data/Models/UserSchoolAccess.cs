namespace SchoolERP.Data.Models;

public class UserSchoolAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;
}
