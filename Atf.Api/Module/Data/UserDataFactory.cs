using Atf.Common.Models;

namespace Atf.Common.Data;

public static class UserDataFactory
{
    public static UserRequest Valid(string? seed = null)
    {
        var suffix = string.IsNullOrWhiteSpace(seed)
            ? Guid.NewGuid().ToString("N")[..8]
            : string.Concat(seed.Where(char.IsLetterOrDigit)).ToLowerInvariant()[..Math.Min(12, string.Concat(seed.Where(char.IsLetterOrDigit)).Length)];
        return new UserRequest(
            $"ATF User {suffix}",
            $"atf.{suffix}.{Guid.NewGuid():N}"[..Math.Min(45, $"atf.{suffix}.{Guid.NewGuid():N}".Length)] + "@example.com");
    }

    public static UserRequest WithEmail(string email, string? seed = null)
    {
        var valid = Valid(seed);
        return valid with { Email = email };
    }
}
