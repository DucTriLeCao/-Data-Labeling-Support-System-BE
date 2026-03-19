namespace DataLabeling.Domain.DTOs.Auth;

public static class UserStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";

    public static readonly string[] AllStatuses = { Active, Inactive };
}
