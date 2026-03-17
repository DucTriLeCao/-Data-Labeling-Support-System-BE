namespace DataLabeling.Domain.DTOs.Auth;

public static class UserRole
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Annotator = "Annotator";
    public const string Reviewer = "Reviewer";

    public static readonly string[] AllRoles = { Admin, Manager, Annotator, Reviewer };
}
