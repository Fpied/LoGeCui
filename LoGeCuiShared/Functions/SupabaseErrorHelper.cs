using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LoGeCuiShared.Functions
{
    internal static class SupabaseErrorHelper
    {
        internal static string ExtractSupabaseError(string content, System.Net.HttpStatusCode code)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    var json = JsonDocument.Parse(content);
                    if (json.RootElement.TryGetProperty("msg", out var msg)) return msg.GetString() ?? $"Erreur {code}";
                    if (json.RootElement.TryGetProperty("error_description", out var ed)) return ed.GetString() ?? $"Erreur {code}";
                    if (json.RootElement.TryGetProperty("message", out var m)) return m.GetString() ?? $"Erreur {code}";
                    if (json.RootElement.TryGetProperty("error", out var e)) return e.GetString() ?? $"Erreur {code}";
                }
                catch
                {
                    if (content.Length < 400) return content;
                }
            }
            return $"Erreur {code}";
        }
    }
}
