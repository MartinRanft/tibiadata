using TibiaDataApi.Contracts.Public.Achievements;

namespace TibiaDataApi.Services.DataBaseService.Achievements.Interfaces
{
    public interface IAchievementsDataBaseService
    {
        Task<List<AchievementListItemResponse>> GetAchievementsAsync(CancellationToken cancellationToken = default);
        Task<AchievementDetailsResponse?> GetAchievementDetailsByNameAsync(string achievementName, CancellationToken cancellationToken = default);
        Task<AchievementDetailsResponse?> GetAchievementDetailsByIdAsync(int achievementId, CancellationToken cancellationToken = default);
    }
}