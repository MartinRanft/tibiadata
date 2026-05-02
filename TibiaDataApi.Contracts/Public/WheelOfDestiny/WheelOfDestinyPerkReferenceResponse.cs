namespace TibiaDataApi.Contracts.Public.WheelOfDestiny
{
    public sealed record WheelOfDestinyPerkReferenceResponse(
        int Id,
        string Key,
        string Slug,
        string Vocation,
        string Type,
        string Name);
}
