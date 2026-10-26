namespace ChangeAnalyst.Shared.Auth;

public sealed record UserInfo(string Name, string ObjectId, IReadOnlyList<string> Roles);
