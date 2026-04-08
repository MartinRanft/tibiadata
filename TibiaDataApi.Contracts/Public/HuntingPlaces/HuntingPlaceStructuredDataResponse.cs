namespace TibiaDataApi.Contracts.Public.HuntingPlaces
{
    public sealed record HuntingPlaceStructuredDataResponse(
        string? Template,
        HuntingPlaceInfobox? Infobox,
        HuntingPlaceAdditionalAttributes? AdditionalAttributes);
}