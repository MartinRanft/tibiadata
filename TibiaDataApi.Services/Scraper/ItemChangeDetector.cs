using System.Reflection;
using System.Text.Json;

using TibiaDataApi.Services.Entities.Items;

namespace TibiaDataApi.Services.Scraper
{
    public static class ItemChangeDetector
    {
        private static readonly PropertyInfo[] IncludedProperties = typeof(Item)
                                                                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                                    .Where(property => property.CanRead && property.CanWrite)
                                                                    .Where(property => property.Name is not nameof(Item.Id)
                                                                    and not nameof(Item.Category)
                                                                    and not nameof(Item.ItemAssets)
                                                                    and not nameof(Item.NormalizedName)
                                                                    and not nameof(Item.NormalizedActualName)
                                                                    and not nameof(Item.LastUpdated)
                                                                    and not nameof(Item.LastSeenAt)
                                                                    and not nameof(Item.IsMissingFromSource)
                                                                    and not nameof(Item.MissingSince))
                                                                    .ToArray();

        public static IReadOnlyList<string> GetChangedFields(Item existing, Item incoming)
        {
            List<string> changedFields = new();

            foreach(PropertyInfo property in IncludedProperties)
            {
                object? existingValue = property.GetValue(existing);
                object? incomingValue = property.GetValue(incoming);

                if(!AreEqual(existingValue, incomingValue))
                {
                    changedFields.Add(property.Name);
                }
            }

            return changedFields;
        }

        public static string CreateSnapshotJson(Item item)
        {
            Dictionary<string, object?> snapshot = IncludedProperties.ToDictionary(
                property => property.Name,
                property => CloneValue(property.GetValue(item)));

            return JsonSerializer.Serialize(snapshot);
        }

        private static bool AreEqual(object? left, object? right)
        {
            if(left is List<string> leftList && right is List<string> rightList)
            {
                return leftList.SequenceEqual(rightList);
            }

            return Equals(left, right);
        }

        private static object? CloneValue(object? value)
        {
            if(value is List<string> list)
            {
                return list.ToList();
            }

            return value;
        }
    }
}