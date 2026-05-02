namespace TibiaDataApi.Services.Entities.Assets
{
    public enum ItemImageSyncState
    {
        Pending = 1,
        Processing = 2,
        Succeeded = 3,
        Missing = 4,
        Failed = 5
    }
}