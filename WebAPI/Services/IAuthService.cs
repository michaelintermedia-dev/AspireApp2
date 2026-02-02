namespace WebAPI.Services;

public interface IAuthService
{
    Task<(bool success, string? token, string? refreshToken, string? message)> RegisterAsync(
        string email, string password, string? firstName, string? lastName);
    
    Task<(bool success, string? token, string? refreshToken, string? message, int? userId)> LoginAsync(
        string email, string password, string? deviceToken, string? platform);
    
    Task<(bool success, string? token, string? refreshToken, string? message)> RefreshTokenAsync(
        string refreshToken);
    
    Task<bool> LogoutAsync(int userId, string refreshToken);
    
    Task<bool> VerifyEmailAsync(string token);
    
    Task<bool> RequestPasswordResetAsync(string email);
    
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}