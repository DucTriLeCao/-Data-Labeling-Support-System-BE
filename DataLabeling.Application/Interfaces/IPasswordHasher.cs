namespace DataLabeling.Application.Interfaces;

public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password using bcrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a bcrypt hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
