using System.Globalization;
using System.Text;

using Prometheus;

using TibiaDataApi.Contracts.Admin;

namespace TibiaDataApi.AdminAccess
{
    public interface IAdminMetricsService
    {
        Task<AdminMetricsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);
    }

    internal sealed class AdminMetricsService : IAdminMetricsService
    {
        private const int MaxSamplesPerSection = 80;

        public async Task<AdminMetricsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            await using MemoryStream stream = new();
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, cancellationToken);

            string rawMetricsText = Encoding.UTF8.GetString(stream.ToArray());
            ParsedMetrics parsedMetrics = Parse(rawMetricsText);

            List<AdminMetricSampleResponse> httpSamples = parsedMetrics.Samples
                                                                      .Where(sample => IsHttpMetric(sample.Name))
                                                                      .Take(MaxSamplesPerSection)
                                                                      .Select(MapSample)
                                                                      .ToList();

            List<AdminMetricSampleResponse> runtimeSamples = parsedMetrics.Samples
                                                                         .Where(sample => IsRuntimeMetric(sample.Name))
                                                                         .Take(MaxSamplesPerSection)
                                                                         .Select(MapSample)
                                                                         .ToList();

            List<AdminMetricSampleResponse> otherSamples = parsedMetrics.Samples
                                                                       .Where(sample => !IsHttpMetric(sample.Name) && !IsRuntimeMetric(sample.Name))
                                                                       .Take(MaxSamplesPerSection)
                                                                       .Select(MapSample)
                                                                       .ToList();

            return new AdminMetricsOverviewResponse(
                DateTime.UtcNow,
                parsedMetrics.MetricFamilyCount,
                parsedMetrics.Samples.Count,
                BuildSummary(parsedMetrics.Samples),
                httpSamples,
                runtimeSamples,
                otherSamples,
                rawMetricsText);
        }

        private static AdminMetricsSummaryResponse BuildSummary(IReadOnlyList<ParsedMetricSample> samples)
        {
            double? totalHttpRequests = SumMetric(samples, "http_requests_received_total");
            double? httpRequestsInProgress = SumMetric(samples, "http_requests_in_progress");
            double? requestDurationCount = SumMetric(samples, "http_request_duration_seconds_count");
            double? requestDurationSum = SumMetric(samples, "http_request_duration_seconds_sum");

            double? averageHttpRequestDurationMs =
            requestDurationCount is > 0 && requestDurationSum is not null
            ? (requestDurationSum.Value / requestDurationCount.Value) * 1000d
            : null;

            return new AdminMetricsSummaryResponse(
                totalHttpRequests,
                httpRequestsInProgress,
                averageHttpRequestDurationMs,
                ConvertBytesToMegabytes(SumMetric(samples, "process_working_set_bytes")),
                ConvertBytesToMegabytes(SumMetric(samples, "dotnet_total_memory_bytes")),
                SumMetric(samples, "process_cpu_seconds_total"),
                SumMetric(samples, "dotnet_collection_count_total"),
                SumMetric(samples, "dotnet_exceptions_total"));
        }

        private static double? SumMetric(IReadOnlyList<ParsedMetricSample> samples, string name)
        {
            List<double> values = samples.Where(sample => string.Equals(sample.Name, name, StringComparison.Ordinal))
                                         .Select(sample => sample.Value)
                                         .Where(value => !double.IsNaN(value) && !double.IsInfinity(value))
                                         .ToList();

            return values.Count == 0 ? null : values.Sum();
        }

        private static double? ConvertBytesToMegabytes(double? valueInBytes)
        {
            return valueInBytes is null ? null : valueInBytes.Value / 1024d / 1024d;
        }

        private static AdminMetricSampleResponse MapSample(ParsedMetricSample sample)
        {
            return new AdminMetricSampleResponse(
                sample.Name,
                sample.Labels,
                sample.Help,
                sample.Value);
        }

        private static bool IsHttpMetric(string name)
        {
            return name.StartsWith("http_", StringComparison.Ordinal) ||
                   name.StartsWith("kestrel_", StringComparison.Ordinal) ||
                   name.StartsWith("aspnetcore_", StringComparison.Ordinal);
        }

        private static bool IsRuntimeMetric(string name)
        {
            return name.StartsWith("process_", StringComparison.Ordinal) ||
                   name.StartsWith("dotnet_", StringComparison.Ordinal);
        }

        private static ParsedMetrics Parse(string rawMetricsText)
        {
            Dictionary<string, string> helpByMetric = new(StringComparer.Ordinal);
            List<ParsedMetricSample> samples = [];

            foreach(string rawLine in rawMetricsText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();

                if(string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if(line.StartsWith("# HELP ", StringComparison.Ordinal))
                {
                    string[] helpParts = line.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);

                    if(helpParts.Length >= 4)
                    {
                        helpByMetric[helpParts[2]] = helpParts[3];
                    }

                    continue;
                }

                if(line.StartsWith('#'))
                {
                    continue;
                }

                int valueSeparatorIndex = line.LastIndexOf(' ');

                if(valueSeparatorIndex <= 0 || valueSeparatorIndex == line.Length - 1)
                {
                    continue;
                }

                string left = line[..valueSeparatorIndex];
                string valueText = line[(valueSeparatorIndex + 1)..];

                if(!TryParseMetricValue(valueText, out double value))
                {
                    continue;
                }

                string name;
                string? labels = null;
                int labelsStartIndex = left.IndexOf('{');

                if(labelsStartIndex >= 0 && left.EndsWith('}'))
                {
                    name = left[..labelsStartIndex];
                    labels = left[(labelsStartIndex + 1)..^1];
                }
                else
                {
                    name = left;
                }

                helpByMetric.TryGetValue(name, out string? help);
                samples.Add(new ParsedMetricSample(name, labels, help, value));
            }

            int metricFamilyCount = samples.Select(sample => sample.Name)
                                           .Distinct(StringComparer.Ordinal)
                                           .Count();

            return new ParsedMetrics(metricFamilyCount, samples);
        }

        private static bool TryParseMetricValue(string valueText, out double value)
        {
            return valueText switch
            {
                "+Inf" => ReturnSpecialValue(double.PositiveInfinity, out value),
                "-Inf" => ReturnSpecialValue(double.NegativeInfinity, out value),
                "NaN" => ReturnSpecialValue(double.NaN, out value),
                _ => double.TryParse(
                    valueText,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out value)
            };
        }

        private static bool ReturnSpecialValue(double specialValue, out double value)
        {
            value = specialValue;
            return true;
        }

        private sealed record ParsedMetrics(int MetricFamilyCount, List<ParsedMetricSample> Samples);

        private sealed record ParsedMetricSample(string Name, string? Labels, string? Help, double Value);
    }
}
