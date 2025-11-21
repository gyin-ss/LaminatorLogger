using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Data;

namespace SwiftUtil
{
    public class InfluxDbAccess
    {
        private readonly string _influxDbUrl;
        private readonly string _influxDbToken;
        private readonly string _influxDbOrg;
        private readonly string _influxDbBucket;

        public InfluxDbAccess(string url, string token, string org, string bucket)
        {
            _influxDbUrl = url ?? throw new ArgumentNullException(nameof(url));
            _influxDbToken = token ?? throw new ArgumentNullException(nameof(token));
            _influxDbOrg = org ?? throw new ArgumentNullException(nameof(org));
            _influxDbBucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
        }

        /// <summary>
        /// Executes the provided Flux query and returns the raw CSV response produced by the InfluxDB flux endpoint.
        /// This is a simple, robust way to inspect the full query output; caller can parse CSV as needed.
        /// </summary>
        public async Task<string> QueryRawAsync(string fluxQuery)
        {
            if (string.IsNullOrWhiteSpace(fluxQuery))
                return "ERROR: Flux query is empty.";

            try
            {
                using var client = new InfluxDBClient(_influxDbUrl, _influxDbToken);
                var queryApi = client.GetQueryApi();
                // QueryRawAsync returns the CSV formatted result from the server
                var csv = await queryApi.QueryRawAsync(fluxQuery, null, _influxDbOrg).ConfigureAwait(false);
                return csv ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"ERROR: Exception while querying InfluxDB: {ex.Message}";
            }
        }

        /// <summary>
        /// Executes the Flux query and returns the response split into lines (CSV lines).
        /// Useful for simple UI display or incremental processing.
        /// </summary>
        public async Task<string[]> QueryLinesAsync(string fluxQuery)
        {
            var raw = await QueryRawAsync(fluxQuery).ConfigureAwait(false);
            if (raw == null) return Array.Empty<string>();
            // split into lines, preserve order
            var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return lines;
        }

        /// <summary>
        /// Executes a Flux query and returns a list of rows represented as dictionaries (column -> value).
        /// This method uses the CSV returned by QueryRawAsync and performs a simple CSV parse.
        /// It's intentionally conservative — for complex CSV quoting use a dedicated CSV parser.
        /// </summary>
        public async Task<List<Dictionary<string, string>>> QueryToRecordsAsync(string fluxQuery)
        {
            var lines = await QueryLinesAsync(fluxQuery).ConfigureAwait(false);
            var results = new List<Dictionary<string, string>>();

            if (lines == null || lines.Length == 0)
                return results;

            // We'll parse the CSV streaming-lines style:
            // - Skip comment lines that start with '#'
            // - When we encounter a header row (contains known column names like _time or _value),
            //   treat it as the active header for subsequent data rows.
            // - Handle repeated header rows (Flux outputs multiple tables).
            // - For each data row map header->value; tolerate missing columns by using empty string.
            // Note: this is still a simple CSV parser (does not handle quoted commas). For robust parsing
            // use a CSV library.

            string[] header = null;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var trimmed = raw.TrimStart();
                if (trimmed.StartsWith("#"))
                    continue; // skip comment/metadata lines

                // split line by comma (simple)
                var parts = raw.Split(',');

                // Detect header row: contains at least one Flux metadata column like "_time" or "_value" or "_field"
                // and no leading "result" value (headers often start with "result,table,...", but we detect presence of known tokens)
                var lowerParts = parts.Select(p => p.Trim()).ToArray();
                if (lowerParts.Any(p => string.Equals(p, "_time", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(p, "_value", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(p, "_field", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(p, "_measurement", StringComparison.OrdinalIgnoreCase)))
                {
                    // treat this as header row
                    header = parts.Select(p => p.Trim()).ToArray();
                    continue;
                }

                // If we don't have a header yet, skip rows until we find one
                if (header == null)
                    continue;

                // Map fields to header columns
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < header.Length; c++)
                {
                    var key = header[c] ?? string.Empty;
                    string val = c < parts.Length ? parts[c] : string.Empty;
                    // Trim surrounding whitespace
                    val = val?.Trim();
                    dict[key] = val;
                }

                // Only add rows that contain at least one non-empty value (avoids adding header-like blank rows)
                if (dict.Values.Any(v => !string.IsNullOrEmpty(v)))
                {
                    results.Add(dict);
                }
            }

            return results;
        }

        /// <summary>
        /// Convenience: build a simple Flux query to read all data from the configured bucket over the last duration.
        /// Example: pass window like "1h" or "30m".
        /// </summary>
        public string BuildSimpleQuery(string rangeDuration = "1h", string measurement = null)
        {
            var measurementFilter = !string.IsNullOrWhiteSpace(measurement)
                ? $" |> filter(fn: (r) => r._measurement == \"{measurement}\")"
                : string.Empty;

            return

$@"from(bucket: ""{_influxDbBucket}"")
      |> range(start: -{rangeDuration}){measurementFilter}
      |> yield(name: ""results"")";

        }

        // Helper to keep logs concise
        private static string TruncateForLog(string s, int max = 200)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        /// <summary>
        /// Query the specified measurement and pivot the Flux output client-side so each row contains all field keys.
        /// Returns a DataTable ordered with columns: _measurement, _time, [tags...], [fields...].
        /// Returns null when no rows found.
        /// </summary>
        public async Task<DataTable> QueryPivotedAsync(string measurement, TimeSpan? recentWindow = null)
        {
            if (string.IsNullOrWhiteSpace(measurement))
                throw new ArgumentNullException(nameof(measurement));

            var safeMeasurement = measurement.Replace("\"", "\\\"");

            // Use wide fixed time window as requested (start 1971 -> stop 2261)
            var fluxStart = "1971-01-01T00:00:00Z";
            var fluxStop = "2261-01-01T00:00:00Z";

            string flux = $@"from(bucket: ""{_influxDbBucket}"")
  |> range(start: {fluxStart}, stop: {fluxStop})
  |> filter(fn: (r) => r[""_measurement""] == ""{safeMeasurement}"")
  |> group(columns: [""_measurement"", ""_field""])
  |> sort(columns: [""_time""])";

            var records = await QueryToRecordsAsync(flux).ConfigureAwait(false);

            if (records == null || records.Count == 0)
            {
                return null;
            }

            // Identify columns produced by Flux
            var allKeys = records.SelectMany(d => d.Keys)
                                 .Where(k => !string.IsNullOrWhiteSpace(k))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            // Flux metadata columns we treat specially
            var metaCols = new[] { "result", "table", "_time", "_field", "_value", "_measurement", "_start", "_stop" };

            // Tag columns are those keys returned by Flux that are not meta and are not field/value.
            var tagColumns = allKeys.Except(metaCols, StringComparer.OrdinalIgnoreCase)
                                     .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                     .ToList();

            // Field names (distinct values from _field column)
            var fieldNames = records
                .Where(r => r.TryGetValue("_field", out _))
                .Select(r => r.TryGetValue("_field", out var f) ? f : null)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Pivot client-side:
            var rowValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            var rowTimes = new Dictionary<string, DateTimeOffset>();
            var rowTagValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (var rec in records)
            {
                if (!rec.TryGetValue("_time", out var timeStr) || string.IsNullOrEmpty(timeStr))
                    continue;

                if (!DateTimeOffset.TryParse(timeStr, out var dto))
                    dto = DateTimeOffset.MinValue;

                var keyParts = new List<string> { timeStr };
                foreach (var tcol in tagColumns)
                {
                    rec.TryGetValue(tcol, out var tval);
                    keyParts.Add(tval ?? string.Empty);
                }

                var compositeKey = string.Join("|", keyParts);

                if (!rowValues.TryGetValue(compositeKey, out var vals))
                {
                    vals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    rowValues[compositeKey] = vals;
                    rowTimes[compositeKey] = dto;

                    var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var tcol in tagColumns)
                    {
                        rec.TryGetValue(tcol, out var tval);
                        tags[tcol] = tval ?? string.Empty;
                    }
                    rowTagValues[compositeKey] = tags;

                    vals["_time"] = timeStr;
                    if (rec.TryGetValue("_measurement", out var meas))
                        vals["_measurement"] = meas;
                }

                if (rec.TryGetValue("_field", out var field) && !string.IsNullOrEmpty(field))
                {
                    rec.TryGetValue("_value", out var value);
                    vals[field] = value ?? string.Empty;
                }
            }

            // Build DataTable columns in desired order: _measurement, _time, tag keys, field keys
            var dt = new DataTable();

            var hasMeasurement = allKeys.Contains("_measurement", StringComparer.OrdinalIgnoreCase);
            if (hasMeasurement)
                dt.Columns.Add("_measurement", typeof(string));

            dt.Columns.Add("_time", typeof(string));

            foreach (var tcol in tagColumns) dt.Columns.Add(tcol, typeof(string));
            foreach (var f in fieldNames) dt.Columns.Add(f, typeof(string));

            var orderedKeys = rowValues.Keys
                .OrderBy(k => rowTimes.TryGetValue(k, out var t) ? t : DateTimeOffset.MinValue)
                .ThenBy(k => k, StringComparer.Ordinal)
                .ToList();

            foreach (var k in orderedKeys)
            {
                var vals = rowValues[k];
                var row = dt.NewRow();

                if (hasMeasurement)
                    row["_measurement"] = vals.TryGetValue("_measurement", out var mval) ? mval : DBNull.Value;

                row["_time"] = vals.TryGetValue("_time", out var tstr) ? tstr : DBNull.Value;

                if (rowTagValues.TryGetValue(k, out var tags))
                {
                    foreach (var tcol in tagColumns)
                    {
                        if (tags.TryGetValue(tcol, out var tval) && !string.IsNullOrEmpty(tval))
                            row[tcol] = tval;
                        else
                            row[tcol] = DBNull.Value;
                    }
                }
                else
                {
                    foreach (var tcol in tagColumns) row[tcol] = DBNull.Value;
                }

                foreach (var f in fieldNames)
                {
                    if (vals.TryGetValue(f, out var fval) && !string.IsNullOrEmpty(fval))
                        row[f] = fval;
                    else
                        row[f] = DBNull.Value;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }

        /// <summary>
        /// Overload: query a measurement and additionally filter by a single tag key/value, then pivot the result.
        /// If tagKey or tagValue are null/empty this delegates to QueryPivotedAsync(measurement, recentWindow).
        /// </summary>
        /// <summary>
        /// Overload: query a measurement and additionally filter by a single tag key/value, then pivot the result.
        /// If tagKey or tagValue are null/empty this delegates to QueryPivotedAsync(measurement, recentWindow).
        /// Uses a fixed time window: start 1971-01-01 -> stop 2261-01-01.
        /// </summary>
        public async Task<DataTable> QueryPivotedAsync(string measurement, string tagKey, string tagValue, TimeSpan? recentWindow = null)
        {
            if (string.IsNullOrWhiteSpace(measurement))
                throw new ArgumentNullException(nameof(measurement));

            // If no tag filter provided, reuse existing implementation
            if (string.IsNullOrWhiteSpace(tagKey) || string.IsNullOrWhiteSpace(tagValue))
                return await QueryPivotedAsync(measurement, recentWindow).ConfigureAwait(false);

            var safeMeasurement = measurement.Replace("\"", "\\\"");
            var safeTagKey = tagKey.Replace("\"", "\\\"");
            var safeTagVal = tagValue.Replace("\"", "\\\"");

            // Use wide fixed time window as requested (start 1971 -> stop 2261)
            var fluxStart = "1971-01-01T00:00:00Z";
            var fluxStop = "2261-01-01T00:00:00Z";

            string fluxRecent = $@"from(bucket: ""{_influxDbBucket}"")
  |> range(start: {fluxStart}, stop: {fluxStop})
  |> filter(fn: (r) => r[""_measurement""] == ""{safeMeasurement}"" and r[""{safeTagKey}""] == ""{safeTagVal}"")
  |> group(columns: [""_measurement"", ""_field""])
  |> sort(columns: [""_time""])";

            var records = await QueryToRecordsAsync(fluxRecent).ConfigureAwait(false);

            if (records == null || records.Count == 0)
            {
                // fallback uses same fixed window
                string fluxAll = $@"from(bucket: ""{_influxDbBucket}"")
  |> range(start: {fluxStart}, stop: {fluxStop})
  |> filter(fn: (r) => r[""_measurement""] == ""{safeMeasurement}"" and r[""{safeTagKey}""] == ""{safeTagVal}"")
  |> group(columns: [""_measurement"", ""_field""])
  |> sort(columns: [""_time""])";

                records = await QueryToRecordsAsync(fluxAll).ConfigureAwait(false);
            }

            if (records == null || records.Count == 0)
            {
                return null;
            }

            // Identify columns produced by Flux
            var allKeys = records.SelectMany(d => d.Keys)
                                 .Where(k => !string.IsNullOrWhiteSpace(k))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            // Flux metadata columns we treat specially
            var metaCols = new[] { "result", "table", "_time", "_field", "_value", "_measurement", "_start", "_stop" };

            // Tag columns are those keys returned by Flux that are not meta and are not field/value.
            var tagColumns = allKeys.Except(metaCols, StringComparer.OrdinalIgnoreCase)
                                     .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                     .ToList();

            // Field names (distinct values from _field column)
            var fieldNames = records
                .Where(r => r.TryGetValue("_field", out _))
                .Select(r => r.TryGetValue("_field", out var f) ? f : null)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Pivot client-side:
            var rowValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            var rowTimes = new Dictionary<string, DateTimeOffset>();
            var rowTagValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (var rec in records)
            {
                if (!rec.TryGetValue("_time", out var timeStr) || string.IsNullOrEmpty(timeStr))
                    continue;

                if (!DateTimeOffset.TryParse(timeStr, out var dto))
                    dto = DateTimeOffset.MinValue;

                var keyParts = new List<string> { timeStr };
                foreach (var tcol in tagColumns)
                {
                    rec.TryGetValue(tcol, out var tval);
                    keyParts.Add(tval ?? string.Empty);
                }

                var compositeKey = string.Join("|", keyParts);

                if (!rowValues.TryGetValue(compositeKey, out var vals))
                {
                    vals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    rowValues[compositeKey] = vals;
                    rowTimes[compositeKey] = dto;

                    var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var tcol in tagColumns)
                    {
                        rec.TryGetValue(tcol, out var tval);
                        tags[tcol] = tval ?? string.Empty;
                    }
                    rowTagValues[compositeKey] = tags;

                    vals["_time"] = timeStr;
                    if (rec.TryGetValue("_measurement", out var meas))
                        vals["_measurement"] = meas;
                }

                if (rec.TryGetValue("_field", out var field) && !string.IsNullOrEmpty(field))
                {
                    rec.TryGetValue("_value", out var value);
                    vals[field] = value ?? string.Empty;
                }
            }

            // Build DataTable columns in desired order: _measurement, _time, tag keys, field keys
            var dt = new DataTable();

            var hasMeasurement = allKeys.Contains("_measurement", StringComparer.OrdinalIgnoreCase);
            if (hasMeasurement)
                dt.Columns.Add("_measurement", typeof(string));

            dt.Columns.Add("_time", typeof(string));

            foreach (var tcol in tagColumns) dt.Columns.Add(tcol, typeof(string));
            foreach (var f in fieldNames) dt.Columns.Add(f, typeof(string));

            var orderedKeys = rowValues.Keys
                .OrderBy(k => rowTimes.TryGetValue(k, out var t) ? t : DateTimeOffset.MinValue)
                .ThenBy(k => k, StringComparer.Ordinal)
                .ToList();

            foreach (var k in orderedKeys)
            {
                var vals = rowValues[k];
                var row = dt.NewRow();

                if (hasMeasurement)
                    row["_measurement"] = vals.TryGetValue("_measurement", out var mval) ? mval : DBNull.Value;

                row["_time"] = vals.TryGetValue("_time", out var tstr) ? tstr : DBNull.Value;

                if (rowTagValues.TryGetValue(k, out var tags))
                {
                    foreach (var tcol in tagColumns)
                    {
                        if (tags.TryGetValue(tcol, out var tval) && !string.IsNullOrEmpty(tval))
                            row[tcol] = tval;
                        else
                            row[tcol] = DBNull.Value;
                    }
                }
                else
                {
                    foreach (var tcol in tagColumns) row[tcol] = DBNull.Value;
                }

                foreach (var f in fieldNames)
                {
                    if (vals.TryGetValue(f, out var fval) && !string.IsNullOrEmpty(fval))
                        row[f] = fval;
                    else
                        row[f] = DBNull.Value;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }
        /// <summary>
        /// Example: writes a sample point and a sample line-protocol record.
        /// This method intentionally returns void (keeps original signature) and writes example data.
        /// </summary>
        public async Task WriteDataPointAsync()
        {
            using var client = new InfluxDBClient(_influxDbUrl, _influxDbToken);
            var writeApi = client.GetWriteApiAsync();

            // Option 1: Construct a PointData object
            var point = PointData.Measurement("cpu_usage")
                .Tag("host", "server_a")
                .Field("usage_percent", 75.5)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            await writeApi.WritePointAsync(point, _influxDbBucket, _influxDbOrg).ConfigureAwait(false);

            // Option 2: Write raw line protocol string
            var nowNs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L;
            string lineProtocol = $"memory,host=server_b used_bytes=1024i,free_bytes=2048i {nowNs}";

            await writeApi.WriteRecordAsync(lineProtocol, WritePrecision.Ns, _influxDbBucket, _influxDbOrg).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a single line-protocol record to InfluxDB and returns a status message.
        /// </summary>
        /// <param name="lineProtocol">Raw line protocol string to write.</param>
        /// <returns>Success message or error details.</returns>
        public async Task<string> WriteDataPointAsync(string lineProtocol)
        {
            if (string.IsNullOrWhiteSpace(lineProtocol))
                return "Line protocol string is empty; nothing to write.";

            try
            {
                using var client = new InfluxDBClient(_influxDbUrl, _influxDbToken);
                var writeApi = client.GetWriteApiAsync();

                // Use nanosecond precision for better accuracy unless your data already includes timestamp units.
                await writeApi.WriteRecordAsync(lineProtocol, WritePrecision.Ns, _influxDbBucket, _influxDbOrg).ConfigureAwait(false);

                return $"OK: Written ({TruncateForLog(lineProtocol)})";
            }
            catch (Exception ex)
            {
                return $"ERROR: Failed writing ({TruncateForLog(lineProtocol)}): {ex.Message}";
            }
        }

        /// <summary>
        /// Write multiple line-protocol records sequentially. Returns array of result messages (one per input line).
        /// </summary>
        public async Task<string[]> WriteDataPointsAsync(IEnumerable<string> lineProtocols)
        {
            if (lineProtocols == null)
                throw new ArgumentNullException(nameof(lineProtocols));

            var results = new List<string>();

            // Reuse a single client for the batch
            using var client = new InfluxDBClient(_influxDbUrl, _influxDbToken);
            var writeApi = client.GetWriteApiAsync();

            foreach (var raw in lineProtocols)
            {
                var line = raw?.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    results.Add("Skipped: empty line");
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    results.Add("Skipped: comment");
                    continue;
                }

                try
                {
                    await writeApi.WriteRecordAsync(line, WritePrecision.Ns, _influxDbBucket, _influxDbOrg).ConfigureAwait(false);
                    results.Add($"OK: {TruncateForLog(line)}");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: {TruncateForLog(line)} -> {ex.Message}");
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Update an existing row identified by the line protocol header (measurement + tags).
        /// If the row has no timestamp, the current server time is used.
        /// Returns 0 on success, negative error code on failure.
        /// </summary>
        public int UpdateInfluxPoint(string lineProtocol)
        {
            // Return codes:
            //  0 = success (row updated)
            // -1 = no matching row found
            // -2 = multiple matching rows found
            // -3 = parse error (invalid line protocol)
            // -4 = delete failed
            // -5 = write failed
            // -6 = unexpected exception

            if (string.IsNullOrWhiteSpace(lineProtocol))
            {
                //AppendMessage("UpdateInfluxPoint: empty line protocol.");
                return -3;
            }

            try
            {
                // Normalize
                var lp = lineProtocol.Trim();
                if (lp.StartsWith("#"))
                {
                    //AppendMessage("UpdateInfluxPoint: comment line - nothing to do.");
                    return -3;
                }

                // Parse measurement+tags and detect timestamp:
                // Find first space (separates measurement/tags and fields)
                var firstSpace = lp.IndexOf(' ');
                if (firstSpace < 0)
                {
                    //AppendMessage("UpdateInfluxPoint: invalid line-protocol (no fields section).");
                    return -3;
                }

                // Determine if there's a timestamp token at the end by checking last token is integer
                var lastSpace = lp.LastIndexOf(' ');
                bool hasTimestamp = false;
                string timestampToken = null;
                if (lastSpace > firstSpace)
                {
                    var possibleTs = lp.Substring(lastSpace + 1).Trim();
                    if (long.TryParse(possibleTs, out _))
                    {
                        hasTimestamp = true;
                        timestampToken = possibleTs;
                    }
                }

                // measurement+tags is substring before firstSpace
                var measAndTags = lp.Substring(0, firstSpace).Trim();
                if (string.IsNullOrEmpty(measAndTags))
                {
                    //AppendMessage("UpdateInfluxPoint: cannot parse measurement/tags.");
                    return -3;
                }

                // measurement is before first comma (if any)
                var commaIndex = measAndTags.IndexOf(',');
                string measurement = commaIndex >= 0 ? measAndTags.Substring(0, commaIndex) : measAndTags;
                measurement = measurement.Trim();
                if (string.IsNullOrEmpty(measurement))
                {
                    //AppendMessage("UpdateInfluxPoint: measurement name empty.");
                    return -3;
                }

                // parse tags
                var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (commaIndex >= 0)
                {
                    var tagPart = measAndTags.Substring(commaIndex + 1);
                    var tagPairs = tagPart.Split(',');
                    foreach (var t in tagPairs)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        var eq = t.IndexOf('=');
                        if (eq <= 0) continue;
                        var k = t.Substring(0, eq).Trim();
                        var v = t.Substring(eq + 1).Trim();
                        // strip optional surrounding quotes for values (line-protocol tags normally unquoted)
                        if (v.Length >= 2 && ((v.StartsWith("\"") && v.EndsWith("\"")) || (v.StartsWith("'") && v.EndsWith("'"))))
                            v = v.Substring(1, v.Length - 2);
                        tags[k] = v;
                    }
                }

                // Build Flux predicate to find matching rows (measurement + tags)
                string safeMeasurement = measurement.Replace("\"", "\\\"");
                var predicateParts = new List<string> { $@"r[""_measurement""] == ""{safeMeasurement}""" };
                foreach (var kv in tags)
                {
                    var key = kv.Key.Replace("\"", "\\\"");
                    var val = kv.Value.Replace("\"", "\\\"");
                    predicateParts.Add($@"r[""{key}""] == ""{val}""");
                }

                var predicate = string.Join(" and ", predicateParts);

                //var reader = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // If timestamp provided, convert to RFC3339 and query a tiny range around it; otherwise query whole retention (epoch -> now)
                DateTimeOffset matchTime = DateTimeOffset.MinValue;
                string fluxRangeStart;
                string fluxRangeStop;
                if (hasTimestamp)
                {
                    // assume timestampToken is unix nanoseconds
                    if (!long.TryParse(timestampToken, out var ns))
                    {
                        //AppendMessage("UpdateInfluxPoint: invalid timestamp token.");
                        return -3;
                    }
                    // Convert ns -> DateTimeOffset
                    long ticks = ns / 100; // 1 tick = 100ns
                    var dto = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(ticks);
                    matchTime = dto;

                    // Use a tiny window: [dto - 1ms, dto + 1ms] to ensure the point is included
                    fluxRangeStart = dto.AddMilliseconds(-1).ToString("o");
                    fluxRangeStop = dto.AddMilliseconds(1).ToString("o");
                }
                else
                {
                    fluxRangeStart = "1970-01-01T00:00:00Z";
                    fluxRangeStop = DateTime.UtcNow.ToString("o");
                }

                //gyin: cannot use group in the next flux
                var flux = $@"from(bucket: ""{_influxDbBucket}"")
  |> range(start: {fluxRangeStart}, stop: {fluxRangeStop})
  |> filter(fn: (r) => {predicate})
  |> sort(columns: [""_time""])";

                var records = QueryToRecordsAsync(flux).GetAwaiter().GetResult();

                if (records == null || records.Count == 0)
                {
                    //AppendMessage("UpdateInfluxPoint: no matching rows found.");
                    return -1;
                }

                // Identify logical rows by composite key: _time + tag values
                var compositeToRecs = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.Ordinal);
                foreach (var rec in records)
                {
                    if (!rec.TryGetValue("_time", out var timeStr) || string.IsNullOrEmpty(timeStr))
                        continue;

                    var keyParts = new List<string> { timeStr };
                    foreach (var tcol in tags.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                    {
                        rec.TryGetValue(tcol, out var tval);
                        keyParts.Add(tval ?? string.Empty);
                    }

                    var composite = string.Join("|", keyParts);
                    if (!compositeToRecs.TryGetValue(composite, out var list))
                    {
                        list = new List<Dictionary<string, string>>();
                        compositeToRecs[composite] = list;
                    }
                    list.Add(rec);
                }

                if (compositeToRecs.Count == 0)
                {
                    //AppendMessage("UpdateInfluxPoint: no valid time-tagged rows found.");
                    return -1;
                }

                if (compositeToRecs.Count > 1)
                {
                    //AppendMessage($"UpdateInfluxPoint: multiple matching logical rows found ({compositeToRecs.Count}).");
                    return -2;
                }

                // Exactly one logical row
                var targetComposite = compositeToRecs.Keys.First();
                var targetRecs = compositeToRecs[targetComposite];

                // Extract the time to use (either from lineProtocol timestamp or from the found row)
                DateTimeOffset targetDto;
                if (hasTimestamp)
                {
                    // use previously parsed matchTime
                    targetDto = matchTime;
                }
                else
                {
                    // parse time from the found row's _time (use first record)
                    var timeStr = targetRecs[0].TryGetValue("_time", out var tsStr) ? tsStr : null;
                    if (string.IsNullOrEmpty(timeStr) || !DateTimeOffset.TryParse(timeStr, out var parsed))
                    {
                        //AppendMessage("UpdateInfluxPoint: cannot parse existing row _time.");
                        return -3;
                    }
                    targetDto = parsed;
                }


                // Prepare delete: start = targetDto, stop = targetDto + 1 tick (1ns not possible in string, use 1ms window)
                //gyin: without delete, if the timestamp is exactly matching current record, the record will be overwritten
                //gyin: this is not alway true, it is on my local influxdb but not true with swift influxdb
                //gyin: added if(hasTimestamp), in this case, need to delete old record
                //gyin: change to delete in all cases, as long as only one data point found
                //if (hasTimestamp)
                if(true) //gyin: 11-21-2025
                {
                    var deleteStart = targetDto.AddMilliseconds(-1).ToString("o");
                    var deleteStop = targetDto.AddMilliseconds(1).ToString("o");

                    // Build delete predicate (measurement + tags)
                    var deletePredicateParts = new List<string> { $@"_measurement=""{measurement.Replace("\"", "\\\"")}""" };
                    foreach (var kv in tags)
                    {
                        deletePredicateParts.Add($@"{kv.Key}=""{kv.Value.Replace("\"", "\\\"")}""");
                    }
                    var deletePredicate = string.Join(" and ", deletePredicateParts);

                    // Call delete endpoint //gyin, has to use Http?
                    using (var http = new HttpClient())
                    {
                        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _influxDbToken);
                        var baseUri = _influxDbUrl.TrimEnd('/');
                        var deleteUri = $"{baseUri}/api/v2/delete?org={Uri.EscapeDataString(_influxDbOrg)}&bucket={Uri.EscapeDataString(_influxDbBucket)}";

                        var payloadObj = new { start = deleteStart, stop = deleteStop, predicate = deletePredicate };
                        var payload = JsonSerializer.Serialize(payloadObj);
                        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                        HttpResponseMessage resp;
                        try
                        {
                            resp = http.PostAsync(deleteUri, content).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            //AppendMessage($"UpdateInfluxPoint: delete request exception -> {ex.Message}");
                            return -4;
                        }

                        if (!resp.IsSuccessStatusCode)
                        {
                            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            //AppendMessage($"UpdateInfluxPoint: delete failed -> {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");
                            return -4;
                        }
                    }
                }


                // Prepare new line protocol to write:
                string newLine = lp;
                if (!hasTimestamp)
                {
                    // append timestamp in nanoseconds
                    long ns = (targetDto.UtcTicks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks) * 100L;
                    newLine = lp.TrimEnd() + " " + ns.ToString();
                }

                // Write the new line
                var writeResult = WriteDataPointAsync(newLine).GetAwaiter().GetResult();

                if (writeResult != null && writeResult.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                {
                    //AppendMessage($"UpdateInfluxPoint: updated row at {targetDto:o} for measurement '{measurement}'.");
                    return 0;
                }
                else
                {
                    //AppendMessage($"UpdateInfluxPoint: write failed -> {writeResult}");
                    return -5;
                }
            }
            catch (Exception ex)
            {
                //AppendMessage($"UpdateInfluxPoint: unexpected exception -> {ex.Message}");
                return -6;
            }
        }


        public int WriteDataInfluxDB(string measurement, List<Tuple<string, string>> tagList, List<Tuple<string, float>> floatFieldList, List<Tuple<string, int>> intFieldList, DateTime time)
        {
            try
            {
                using var client = new InfluxDBClient(_influxDbUrl, _influxDbToken);
                var writeApi = client.GetWriteApiAsync();

                // Ensure timestamp is interpreted as UTC
                DateTime utcTime;
                if (time.Kind == DateTimeKind.Unspecified)
                    utcTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
                else
                    utcTime = time.ToUniversalTime();

                var point = PointData.Measurement(measurement)
                    .Timestamp(utcTime, WritePrecision.Ns);

                foreach (var tag in tagList ?? Enumerable.Empty<Tuple<string, string>>())
                {
                    point = point.Tag(tag.Item1, tag.Item2);
                }
                foreach (var field in floatFieldList ?? Enumerable.Empty<Tuple<string, float>>())
                {
                    point = point.Field(field.Item1, field.Item2);
                }
                foreach (var field in intFieldList ?? Enumerable.Empty<Tuple<string, int>>())
                {
                    point = point.Field(field.Item1, field.Item2);
                }

                // Write synchronously to preserve existing API contract
                writeApi.WritePointAsync(point, _influxDbBucket, _influxDbOrg).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }
    }


}
