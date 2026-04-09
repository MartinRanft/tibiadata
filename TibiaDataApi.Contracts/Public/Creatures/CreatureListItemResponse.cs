namespace TibiaDataApi.Contracts.Public.Creatures
{
    public sealed record CreatureListItemResponse(
        int Id,
        string Name,
        int Hitpoints,
        long Experience,
        CreatureImageResponse? PrimaryImage,
        DateTime LastUpdated);
}