using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Models.DbData;

namespace WebAPI.Services;

public class AuthService : IAuthService
{
    private readonly Recordings2Context _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        Recordings2Context context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool success, string? token, string? refreshToken, string? message)> RegisterAsync(
        string email, string password, string? firstName, string? lastName)
    {
        try
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return (false, null, null, "User with this email already exists");
            }

            // Create password hash
            var (hash, salt) = HashPassword(password);

            // Create user
            var user = new User
            {
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                FirstName = firstName,
                LastName = lastName,
                EmailVerificationToken = GenerateToken(),
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered: {Email}", email);

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            return (true, token, refreshToken, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", email);
            return (false, null, null, "Registration failed");
        }
    }

    public async Task<(bool success, string? token, string? refreshToken, string? message, int? userId)> LoginAsync(
        string email, string password, string? deviceToken, string? platform)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return (false, null, null, "Invalid email or password", null);
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return (false, null, null, "Invalid email or password", null);
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Register device if provided
            if (!string.IsNullOrEmpty(deviceToken) && !string.IsNullOrEmpty(platform))
            {
                await RegisterDeviceAsync(user.Id, deviceToken, platform);
            }

            // Generate tokens
            var token = GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            _logger.LogInformation("User logged in: {Email}", email);

            return (true, token, refreshToken, "Login successful", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", email);
            return (false, null, null, "Login failed", null);
        }
    }

    public async Task<(bool success, string? token, string? refreshToken, string? message)> RefreshTokenAsync(
        string refreshToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.RevokedAt == null);

        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return (false, null, null, "Invalid or expired refresh token");
        }

        // Revoke old token
        session.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var newToken = GenerateJwtToken(session.User);
        var newRefreshToken = await CreateRefreshTokenAsync(session.UserId);

        await _context.SaveChangesAsync();

        return (true, newToken, newRefreshToken, "Token refreshed");
    }

    public async Task<bool> LogoutAsync(int userId, string refreshToken)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.RefreshToken == refreshToken);

        if (session != null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
            return false;

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return false;

        user.PasswordResetToken = GenerateToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        // TODO: Send email with reset link
        _logger.LogInformation("Password reset requested for {Email}", email);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token 
                && u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return false;

        var (hash, salt) = HashPassword(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task RegisterDeviceAsync(int userId, string deviceToken, string platform)
    {
        var existingDevice = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

        if (existingDevice != null)
        {
            existingDevice.LastActiveAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserDevices.Add(new UserDevice
            {
                UserId = userId,
                DeviceToken = deviceToken,
                Platform = platform
            });
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("isEmailVerified", user.IsEmailVerified.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        var refreshToken = GenerateToken();

        _context.UserSessions.Add(new UserSession
        {
            UserId = userId,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private (string hash, string salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var hmac = new HMACSHA512(saltBytes);
        var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return computedHash == storedHash;
    }

    private string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}