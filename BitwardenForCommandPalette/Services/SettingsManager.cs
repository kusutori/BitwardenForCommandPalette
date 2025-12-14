using System;
using System.Collections.Generic;

namespace BitwardenForCommandPalette.Services;

/// <summary>
/// Manages application settings for Bitwarden CLI integration
/// </summary>
public sealed class SettingsManager
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static SettingsManager Instance { get; } = new();

    private SettingsManager() { }

    /// <summary>
    /// Gets or sets the path to the Bitwarden CLI executable (bw.exe)
    /// Default is "bw" (assumes bw is in PATH)
    /// </summary>
    public string BwPath { get; set; } = "bw";

    /// <summary>
    /// Gets or sets custom environment variables for Bitwarden CLI
    /// Format: KEY1=VALUE1;KEY2=VALUE2
    /// </summary>
    public string CustomEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Bitwarden API Client ID for API Key authentication
    /// </summary>
    public string BwClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Bitwarden API Client Secret for API Key authentication
    /// </summary>
    public string BwClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Parses custom environment variables into a dictionary
    /// Automatically includes BW_CLIENTID and BW_CLIENTSECRET if configured
    /// </summary>
    public Dictionary<string, string> GetEnvironmentVariables()
    {
        var result = new Dictionary<string, string>();

        // Add Client ID and Secret if configured
        if (!string.IsNullOrWhiteSpace(BwClientId))
        {
            result["BW_CLIENTID"] = BwClientId;
        }
        if (!string.IsNullOrWhiteSpace(BwClientSecret))
        {
            result["BW_CLIENTSECRET"] = BwClientSecret;
        }

        // Parse custom environment variables
        var envString = CustomEnvironment;
        if (string.IsNullOrWhiteSpace(envString))
            return result;

        // Parse format: KEY1=VALUE1;KEY2=VALUE2
        var pairs = envString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }
}
