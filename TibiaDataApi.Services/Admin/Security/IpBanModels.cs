namespace TibiaDataApi.Services.Admin.Security
{
    public sealed record IpBanPage(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<IpBanEntry> Items);

    public sealed record IpBanEntry(
        string IpAddress,
        string Reason,
        bool IsActive,
        DateTime StartedAt,
        DateTime? EndsAt,
        int? DurationMinutes,
        string? CreatedBy);

    public sealed record IpBanMutationRequest(
        string IpAddress,
        string Reason,
        DateTime? ExpiresAt,
        int? DurationMinutes,
        string? CreatedBy);

    public sealed record IpBanRemovalRequest(
        string IpAddress,
        string? Reason,
        string? RequestedBy);

    public sealed record IpBanMutationResult(
        IpBanMutationOutcome Outcome,
        string Message,
        string? IpAddress);

    public enum IpBanMutationOutcome
    {
        Success,
        InvalidIp,
        InvalidBanWindow,
        ProtectedIp,
        AlreadyExists,
        NotFound
    }
}
