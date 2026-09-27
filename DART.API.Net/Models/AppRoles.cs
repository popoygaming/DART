namespace DART.API.Net.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";

    public static bool IsValid(string role)
    {
        return role == Admin || role == Staff;
    }
}
