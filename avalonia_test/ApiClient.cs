using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AvaloniaTest;

/// <summary>
/// Thin wrapper around HttpClient for calling the gateway-api.
/// </summary>
public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public ApiClient(string baseUrl = "http://localhost:8001")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // ---------- Generic helpers ----------

    public async Task<string> GetAsync(string path)
    {
        var resp = await _http.GetAsync(path);
        return await FormatResponse(resp);
    }

    public async Task<T?> GetJsonAsync<T>(string path)
    {
        var resp = await _http.GetAsync(path);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOpts);
    }

    public async Task<string> PostAsync(string path, object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync(path, content);
        return await FormatResponse(resp);
    }

    public async Task<string> PutAsync(string path, object body)
    {
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PutAsync(path, content);
        return await FormatResponse(resp);
    }

    public async Task<string> DeleteAsync(string path)
    {
        var resp = await _http.DeleteAsync(path);
        return await FormatResponse(resp);
    }

    // ---------- Private ----------

    private static async Task<string> FormatResponse(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        var code = (int)resp.StatusCode;
        var header = $"HTTP {code} {resp.StatusCode}";

        // For success — pretty-print JSON as before
        if (resp.IsSuccessStatusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                body = JsonSerializer.Serialize(doc, JsonOpts);
            }
            catch { }
            return $"✅ {header}\n{body}";
        }

        // For errors — produce a human-readable message
        var friendly = ParseFriendlyError(code, body);
        return $"❌ {header}\n{friendly}";
    }

    /// <summary>
    /// Turns raw API error JSON into a short, readable message.
    /// </summary>
    private static string ParseFriendlyError(int statusCode, string rawBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            // --- 422 Validation Error (FastAPI/Pydantic) ---
            if (statusCode == 422 && root.TryGetProperty("detail", out var detailArr) && detailArr.ValueKind == JsonValueKind.Array)
            {
                var msgs = new List<string>();
                foreach (var item in detailArr.EnumerateArray())
                {
                    var field = "?";
                    if (item.TryGetProperty("loc", out var loc) && loc.ValueKind == JsonValueKind.Array)
                    {
                        var parts = loc.EnumerateArray().Select(l => l.ToString()).ToList();
                        // skip "body" prefix
                        field = string.Join(" → ", parts.Where(p => p != "body"));
                    }
                    var msg = item.TryGetProperty("msg", out var m) ? m.GetString() : "invalid";
                    msgs.Add($"  • {field}: {msg}");
                }
                return $"กรุณาตรวจสอบข้อมูลที่กรอก:\n{string.Join("\n", msgs)}";
            }

            // --- ERPNext error with detail object ---
            if (root.TryGetProperty("detail", out var detail))
            {
                // detail can be a string or an object
                if (detail.ValueKind == JsonValueKind.String)
                    return FriendlyByStatus(statusCode, detail.GetString()!);

                if (detail.ValueKind == JsonValueKind.Object)
                {
                    // Try _server_messages first (ERPNext's format)
                    if (detail.TryGetProperty("_server_messages", out var srvMsgs))
                    {
                        var parsed = ExtractServerMessages(srvMsgs.GetString() ?? "");
                        if (parsed.Count > 0)
                            return FriendlyByStatus(statusCode, string.Join("\n", parsed.Select(m => $"  • {m}")));
                    }

                    // Try exc_type
                    var excType = detail.TryGetProperty("exc_type", out var et) ? et.GetString() : null;
                    return FriendlyByStatus(statusCode, excType ?? detail.ToString());
                }
            }
        }
        catch
        {
            // JSON parsing failed — fall through
        }

        return FriendlyByStatus(statusCode, rawBody.Length > 200 ? rawBody[..200] + "…" : rawBody);
    }

    private static string FriendlyByStatus(int code, string detail) => code switch
    {
        401 => $"🔑 ไม่ได้รับอนุญาต (ตรวจ API Key/Secret)\n{detail}",
        403 => $"🚫 ถูกปฏิเสธ (ไม่มีสิทธิ์เข้าถึง)\n{detail}",
        404 => $"🔍 ไม่พบข้อมูล:\n{detail}",
        409 => $"⚠️ ข้อมูลซ้ำหรือขัดแย้ง:\n{detail}",
        417 => $"⚠️ ERPNext ปฏิเสธ (ข้อมูลไม่ครบ/ไม่ถูก):\n{detail}",
        500 => $"💥 เซิร์ฟเวอร์ ERPNext Error:\n{detail}",
        _ => $"ข้อผิดพลาด ({code}):\n{detail}",
    };

    /// <summary>
    /// ERPNext encodes server messages as a JSON-string-in-a-JSON-string.
    /// </summary>
    private static List<string> ExtractServerMessages(string raw)
    {
        var results = new List<string>();
        try
        {
            // outer layer: JSON array of strings
            var outer = JsonSerializer.Deserialize<List<string>>(raw);
            if (outer == null) return results;

            foreach (var item in outer)
            {
                try
                {
                    using var doc = JsonDocument.Parse(item);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                        results.Add(Regex.Unescape(msg.GetString() ?? item));
                    else
                        results.Add(item);
                }
                catch
                {
                    results.Add(item);
                }
            }
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(raw))
                results.Add(raw);
        }
        return results;
    }

    public void Dispose() => _http.Dispose();
}
