namespace Atf.Common.Models;

public sealed record UserRequest(string Name, string Email, string Gender = "male", string Status = "active");

public sealed record UserResponse(int Id, string Name, string Email, string Gender, string Status);
