using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SkiRental.App.Services;

/// <summary>
/// Simple test class to verify JWT functionality
/// </summary>
public static class JwtTest
{
    public static async Task<bool> TestJwtFlow(JwtTokenService jwtService, ILogger logger)
    {
        try
        {
            // Test token generation
            var token = jwtService.GenerateToken("123", "testuser", UserRole.Customer);
            logger.LogInformation("Generated JWT token: {Token}", token);

            // Test token validation
            var principal = jwtService.ValidateToken(token);
            if (principal == null)
            {
                logger.LogError("Token validation failed");
                return false;
            }

            // Test extracting claims
            var userId = jwtService.GetUserIdFromToken(token);
            var username = jwtService.GetUsernameFromToken(token);
            var role = jwtService.GetRoleFromToken(token);

            logger.LogInformation("Extracted claims - UserId: {UserId}, Username: {Username}, Role: {Role}", 
                userId, username, role);

            // Verify claims
            if (userId != "123" || username != "testuser" || role != UserRole.Customer)
            {
                logger.LogError("Claims extraction failed");
                return false;
            }

            logger.LogInformation("JWT test passed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JWT test failed");
            return false;
        }
    }
}
