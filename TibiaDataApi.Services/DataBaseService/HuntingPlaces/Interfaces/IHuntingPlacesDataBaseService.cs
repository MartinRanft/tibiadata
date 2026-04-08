using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.HuntingPlaces;

namespace TibiaDataApi.Services.DataBaseService.HuntingPlaces.Interfaces
{
    public interface IHuntingPlacesDataBaseService
    {
        Task<List<HuntingPlaceListItemResponse>> GetHuntingPlacesAsync(CancellationToken cancellationToken = default);

        Task<HuntingPlaceDetailsResponse?> GetHuntingPlaceDetailsByNameAsync(
            string huntingPlaceName,
            CancellationToken cancellationToken = default);

        Task<HuntingPlaceDetailsResponse?> GetHuntingPlaceDetailsByIdAsync(
            int huntingPlaceId,
            CancellationToken cancellationToken = default);

        Task<HuntingPlaceAreaRecommendationResponse?> GetHuntingPlaceAreaRecommendationAsync(
            string huntingPlaceName,
            CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetHuntingPlaceUpdates(CancellationToken cancellationToken = default);

        Task<List<SyncStateResponse>> GetHuntingPlaceUpdatesByDate(DateTime time, CancellationToken cancellationToken = default);
    }
}