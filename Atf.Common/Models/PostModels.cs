namespace Atf.Common.Models;

public sealed record PostRequest(string Title, string Body);

public sealed record PostResponse(int Id, int UserId, string Title, string Body);
