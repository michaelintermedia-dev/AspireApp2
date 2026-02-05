using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAPI.Endpoints.AuthEndpoints;
using WebAPI.Models.DbData;
using Xunit;

namespace WebAPI.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove all Entity Framework related services
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.Namespace != null && 
                           (d.ServiceType.Namespace.Contains("EntityFramework") ||
                            d.ServiceType == typeof(RecordingsDbContext) ||
                            d.ServiceType == typeof(DbContextOptions<RecordingsDbContext>)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }
        });
    }
}

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private string? _testDatabaseName;

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _testDatabaseName = $"TestDb_{Guid.NewGuid()}";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Aspire:UseServiceDefaults"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all Entity Framework and database provider related services
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType.Namespace != null && 
                               (d.ServiceType.Namespace.Contains("EntityFramework") ||
                                d.ServiceType.Namespace.Contains("Npgsql") ||
                                d.ServiceType == typeof(RecordingsDbContext) ||
                                d.ServiceType == typeof(DbContextOptions<RecordingsDbContext>)))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<RecordingsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDatabaseName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up the in-memory database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task CompleteAuthFlow_ShouldWorkEndToEnd()
    {
        // 1. Register a new user
        var registerRequest = new RegisterRequest(
            Email: "test@example.com",
            Password: "SecurePassword123!",
            FirstName: "John",
            LastName: "Doe"
        );

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerResult);
        Assert.NotNull(registerResult.Token);
        Assert.NotNull(registerResult.RefreshToken);
        Assert.Equal("Registration successful", registerResult.Message);

        var originalToken = registerResult.Token;
        var originalRefreshToken = registerResult.RefreshToken;

        // 2. Try to register with the same email (should fail)
        var duplicateRegisterResponse = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateRegisterResponse.StatusCode);

        // 3. Login with the registered user
        var loginRequest = new LoginRequest(
            Email: "test@example.com",
            Password: "SecurePassword123!",
            DeviceToken: "test-device-token",
            Platform: "iOS"
        );

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.NotNull(loginResult.Token);
        Assert.NotNull(loginResult.RefreshToken);
        Assert.True(loginResult.UserId > 0);

        // 4. Try to login with wrong password (should fail)
        var wrongPasswordRequest = new LoginRequest(
            Email: "test@example.com",
            Password: "WrongPassword123!",
            DeviceToken: null,
            Platform: null
        );

        var wrongPasswordResponse = await _client.PostAsJsonAsync("/auth/login", wrongPasswordRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);

        // 5. Refresh the token
        var refreshRequest = new RefreshTokenRequest(RefreshToken: loginResult.RefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>();
        Assert.NotNull(refreshResult);
        Assert.NotNull(refreshResult.Token);
        Assert.NotNull(refreshResult.RefreshToken);
        Assert.NotEqual(loginResult.Token, refreshResult.Token); // New token should be different

        // 6. Try to refresh with invalid token (should fail)
        var invalidRefreshRequest = new RefreshTokenRequest(RefreshToken: "invalid-token");
        var invalidRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", invalidRefreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidRefreshResponse.StatusCode);

        // 7. Logout
        var logoutRequest = new LogoutRequest(
            UserId: loginResult.UserId.Value,
            RefreshToken: refreshResult.RefreshToken
        );

        var logoutResponse = await _client.PostAsJsonAsync("/auth/logout", logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 8. Try to use the logged out refresh token (should fail)
        var loggedOutRefreshRequest = new RefreshTokenRequest(RefreshToken: refreshResult.RefreshToken);
        var loggedOutRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", loggedOutRefreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOutRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task EmailVerification_ShouldWorkCorrectly()
    {
        // 1. Register a user
        var registerRequest = new RegisterRequest(
            Email: "verify@example.com",
            Password: "SecurePassword123!",
            FirstName: "Jane",
            LastName: "Smith"
        );

        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        // 2. Get the verification token from the database
        string? verificationToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "verify@example.com");
            Assert.NotNull(user);
            Assert.False(user.Isemailverified ?? false);
            verificationToken = user.Emailverificationtoken;
            Assert.NotNull(verificationToken);
        }

        // 3. Verify the email
        var encodedToken = Uri.EscapeDataString(verificationToken);
        var verifyResponse = await _client.PostAsync($"/auth/verify-email?token={encodedToken}", null);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        // 4. Check that the email is now verified
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "verify@example.com");
            Assert.NotNull(user);
            Assert.True(user.Isemailverified ?? false);
            Assert.Null(user.Emailverificationtoken);
        }

        // 5. Try to verify with invalid token (should fail)
        var invalidVerifyResponse = await _client.PostAsync("/auth/verify-email?token=invalid-token", null);
        Assert.Equal(HttpStatusCode.BadRequest, invalidVerifyResponse.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_ShouldWorkCorrectly()
    {
        // 1. Register a user
        var registerRequest = new RegisterRequest(
            Email: "reset@example.com",
            Password: "OldPassword123!",
            FirstName: "Bob",
            LastName: "Johnson"
        );

        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        // 2. Request password reset
        var forgotPasswordRequest = new ForgotPasswordRequest(Email: "reset@example.com");
        var forgotPasswordResponse = await _client.PostAsJsonAsync("/auth/forgot-password", forgotPasswordRequest);
        Assert.Equal(HttpStatusCode.OK, forgotPasswordResponse.StatusCode);

        // 3. Get the reset token from the database
        string? resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "reset@example.com");
            Assert.NotNull(user);
            resetToken = user.Passwordresettoken;
            Assert.NotNull(resetToken);
            Assert.NotNull(user.Passwordresettokenexpiry);
            Assert.True(user.Passwordresettokenexpiry > DateTime.UtcNow);
        }

        // 4. Reset the password
        var resetPasswordRequest = new ResetPasswordRequest(
            Token: resetToken,
            NewPassword: "NewPassword123!"
        );

        var resetPasswordResponse = await _client.PostAsJsonAsync("/auth/reset-password", resetPasswordRequest);
        Assert.Equal(HttpStatusCode.OK, resetPasswordResponse.StatusCode);

        // 5. Check that the reset token is cleared
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "reset@example.com");
            Assert.NotNull(user);
            Assert.Null(user.Passwordresettoken);
            Assert.Null(user.Passwordresettokenexpiry);
        }

        // 6. Try to login with old password (should fail)
        var oldPasswordLogin = new LoginRequest(
            Email: "reset@example.com",
            Password: "OldPassword123!",
            DeviceToken: null,
            Platform: null
        );

        var oldPasswordResponse = await _client.PostAsJsonAsync("/auth/login", oldPasswordLogin);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);

        // 7. Login with new password (should succeed)
        var newPasswordLogin = new LoginRequest(
            Email: "reset@example.com",
            Password: "NewPassword123!",
            DeviceToken: null,
            Platform: null
        );

        var newPasswordResponse = await _client.PostAsJsonAsync("/auth/login", newPasswordLogin);
        Assert.Equal(HttpStatusCode.OK, newPasswordResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldSucceed()
    {
        var registerRequest = new RegisterRequest(
            Email: "invalid-email",
            Password: "SecurePassword123!",
            FirstName: "Test",
            LastName: "User"
        );

        // Note: This test shows that the endpoint currently doesn't validate email format
        // You may want to add email validation in the future
        var response = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldFail()
    {
        var loginRequest = new LoginRequest(
            Email: "nonexistent@example.com",
            Password: "SomePassword123!",
            DeviceToken: null,
            Platform: null
        );

        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ShouldReturnOk()
    {
        // For security reasons, forgot password should return OK even for non-existent emails
        var forgotPasswordRequest = new ForgotPasswordRequest(Email: "nonexistent@example.com");
        var response = await _client.PostAsJsonAsync("/auth/forgot-password", forgotPasswordRequest);
        
        // Should return OK to avoid email enumeration attacks
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MultipleDeviceLogin_ShouldTrackDevices()
    {
        // 1. Register a user
        var registerRequest = new RegisterRequest(
            Email: "multidevice@example.com",
            Password: "SecurePassword123!",
            FirstName: "Multi",
            LastName: "Device"
        );

        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        // 2. Login from iOS device
        var iOSLoginRequest = new LoginRequest(
            Email: "multidevice@example.com",
            Password: "SecurePassword123!",
            DeviceToken: "ios-device-token",
            Platform: "iOS"
        );

        var iOSLoginResponse = await _client.PostAsJsonAsync("/auth/login", iOSLoginRequest);
        Assert.Equal(HttpStatusCode.OK, iOSLoginResponse.StatusCode);

        // 3. Login from Android device
        var androidLoginRequest = new LoginRequest(
            Email: "multidevice@example.com",
            Password: "SecurePassword123!",
            DeviceToken: "android-device-token",
            Platform: "Android"
        );

        var androidLoginResponse = await _client.PostAsJsonAsync("/auth/login", androidLoginRequest);
        Assert.Equal(HttpStatusCode.OK, androidLoginResponse.StatusCode);

        // 4. Verify both devices are tracked in the database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users
                .Include(u => u.Userdevices)
                .FirstOrDefaultAsync(u => u.Email == "multidevice@example.com");
            
            Assert.NotNull(user);
            Assert.Equal(2, user.Userdevices.Count);
            Assert.Contains(user.Userdevices, d => d.Devicetoken == "ios-device-token" && d.Platform == "iOS");
            Assert.Contains(user.Userdevices, d => d.Devicetoken == "android-device-token" && d.Platform == "Android");
        }
    }

    [Fact]
    public async Task RefreshToken_AfterExpiration_ShouldFail()
    {
        // 1. Register and login a user
        var registerRequest = new RegisterRequest(
            Email: "expiry@example.com",
            Password: "SecurePassword123!",
            FirstName: "Test",
            LastName: "Expiry"
        );

        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            Email: "expiry@example.com",
            Password: "SecurePassword123!",
            DeviceToken: null,
            Platform: null
        );

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        // 2. Manually expire the refresh token in the database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var session = await dbContext.Usersessions
                .FirstOrDefaultAsync(s => s.Refreshtoken == loginResult.RefreshToken);
            
            Assert.NotNull(session);
            session.Expiresat = DateTime.UtcNow.AddDays(-1); // Expire it
            await dbContext.SaveChangesAsync();
        }

        // 3. Try to refresh with expired token (should fail)
        var refreshRequest = new RefreshTokenRequest(RefreshToken: loginResult.RefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldUpdateLastLoginTime()
    {
        // 1. Register a user
        var registerRequest = new RegisterRequest(
            Email: "lastlogin@example.com",
            Password: "SecurePassword123!",
            FirstName: "Last",
            LastName: "Login"
        );

        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        DateTime? initialLoginTime;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "lastlogin@example.com");
            Assert.NotNull(user);
            initialLoginTime = user.Lastloginat;
        }

        // 2. Wait a moment to ensure timestamp will be different
        await Task.Delay(100);

        // 3. Login
        var loginRequest = new LoginRequest(
            Email: "lastlogin@example.com",
            Password: "SecurePassword123!",
            DeviceToken: null,
            Platform: null
        );

        await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // 4. Verify LastLoginAt was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RecordingsDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "lastlogin@example.com");
            Assert.NotNull(user);
            Assert.NotNull(user.Lastloginat);
            Assert.NotEqual(initialLoginTime, user.Lastloginat);
        }
    }

    // Response DTOs for deserialization
    private record RegisterResponse(string Message, string Token, string RefreshToken);
    private record LoginResponse(string Message, string Token, string RefreshToken, int? UserId);
    private record RefreshResponse(string Token, string RefreshToken);
}

