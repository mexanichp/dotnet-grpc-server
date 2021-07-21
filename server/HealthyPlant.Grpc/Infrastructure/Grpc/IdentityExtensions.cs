using System;
using System.Security.Claims;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class IdentityExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user) => user.FindFirstValue(UserDefaults.UserId) ?? throw new ArgumentNullException(nameof(user));

        public static string? GetUserEmail(this ClaimsPrincipal user) =>  user.FindFirstValue(ClaimTypes.Email);

        public static bool IsEmailVerified(this ClaimsPrincipal user) => bool.TryParse(user.FindFirstValue(UserDefaults.EmailVerified), out var isVerified) && isVerified;
    }

    public static class UserDefaults
    {
        public static string UserId => "user_id";
        public static string EmailVerified => "email_verified";
        public static string TimezoneOffset => "timezone_offset";
    }
}