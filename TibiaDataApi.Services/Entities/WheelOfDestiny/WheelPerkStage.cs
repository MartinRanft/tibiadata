namespace TibiaDataApi.Services.Entities.WheelOfDestiny
{
    public sealed class WheelPerkStage
    {
        public int Id { get; set; }

        public int WheelPerkId { get; set; }

        public WheelPerk WheelPerk { get; set; } = null!;

        public byte Stage { get; set; }

        public WheelStageUnlockKind UnlockKind { get; set; }

        public short UnlockValue { get; set; }

        public string? EffectSummary { get; set; }

        public string? EffectDetailsJson { get; set; }

        public short SortOrder { get; set; }
    }
}
