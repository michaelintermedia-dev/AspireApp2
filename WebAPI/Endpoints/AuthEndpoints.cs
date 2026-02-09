using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Endpoints.AuthEndpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", async (RegisterRequest request, [FromServices] IAuthService authService) =>
        {
            var (success, token, refreshToken, message) = await authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName
            );

            if (!success)
                return Results.BadRequest(new { message });

            return Results.Ok(new
            {
                message,
                token,
                refreshToken
            });
        });

        auth.MapPost("/login", async (LoginRequest request, [FromServices] IAuthService authService) =>
        {
            var (success, token, refreshToken, message, userId) = await authService.LoginAsync(
                request.Email,
                request.Password,
                request.DeviceToken,
                request.Platform
            );

            if (!success)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                message,
                token,
                refreshToken,
                userId
            });
        });

        auth.MapPost("/refresh", async (RefreshTokenRequest request, [FromServices] IAuthService authService) =>
        {
            var (success, token, refreshToken, message) = await authService.RefreshTokenAsync(
                request.RefreshToken
            );

            if (!success)
                return Results.Unauthorized();

            return Results.Ok(new { token, refreshToken });
        });

        auth.MapPost("/logout", async (LogoutRequest request, [FromServices] IAuthService authService) =>
        {
            await authService.LogoutAsync(request.UserId, request.RefreshToken);
            return Results.Ok(new { message = "Logged out successfully" });
        });

        auth.MapPost("/verify-email", async (string token, [FromServices] IAuthService authService) =>
        {
            var success = await authService.VerifyEmailAsync(token);
            return success ? Results.Ok() : Results.BadRequest();
        });

        auth.MapPost("/forgot-password", async (ForgotPasswordRequest request, [FromServices] IAuthService authService) =>
        {
            await authService.RequestPasswordResetAsync(request.Email);
            return Results.Ok(new { message = "Password reset email sent" });
        });

        auth.MapPost("/reset-password", async (ResetPasswordRequest request, [FromServices] IAuthService authService) =>
        {
            var success = await authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return success ? Results.Ok() : Results.BadRequest();
        });
    }
}

public record RegisterRequest(string Email, string Password, string? FirstName, string? LastName);
public record LoginRequest(string Email, string Password, string? DeviceToken, string? Platform);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(int UserId, string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);