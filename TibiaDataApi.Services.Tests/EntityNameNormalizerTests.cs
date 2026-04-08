using TibiaDataApi.Services.Text;

namespace TibiaDataApi.Services.Tests
{
    public sealed class EntityNameNormalizerTests
    {
        [Theory]
        [InlineData("Eldritch Rod", "eldritch rod")]
        [InlineData("  Golden_Rune_Emblem  ", "golden rune emblem")]
        [InlineData("Yellow Skull (Item)", "yellow skull (item)")]
        public void Normalize_ReturnsTrimmedLowercaseValue(string input, string expected)
        {
            string normalized = EntityNameNormalizer.Normalize(input);

            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void NormalizeOptional_ReturnsNull_ForEmptyInput()
        {
            string? normalized = EntityNameNormalizer.NormalizeOptional("   ");

            Assert.Null(normalized);
        }
    }
}