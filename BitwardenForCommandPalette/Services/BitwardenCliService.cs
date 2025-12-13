// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Models;

namespace BitwardenForCommandPalette.Services;

/// <summary>
/// Service class for interacting with the Bitwarden CLI (bw)
/// </summary>
public class BitwardenCliService
{
    private static BitwardenCliService? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// The session key used for authenticated operations
    /// </summary>
    public string? SessionKey { get; private set; }

    /// <summary>
    /// Path to the bw CLI executable. Default is "bw" assuming it's in PATH
    /// </summary>
    public string BwPath { get; set; } = "bw";

    /// <summary>
    /// Singleton instance of the BitwardenCliService
    /// </summary>
    public static BitwardenCliService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new BitwardenCliService();
                }
            }
            return _instance;
        }
    }

    private BitwardenCliService() { }

    /// <summary>
    /// Executes a bw CLI command and returns the output
    /// </summary>
    private async Task<(string output, string error, int exitCode)> ExecuteCommandAsync(string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = BwPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = processInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return (outputBuilder.ToString().Trim(), errorBuilder.ToString().Trim(), process.ExitCode);
    }

    /// <summary>
    /// Gets the current status of the Bitwarden vault
    /// </summary>
    public async Task<BitwardenStatus?> GetStatusAsync()
    {
        var (output, error, exitCode) = await ExecuteCommandAsync("status");

        if (exitCode != 0)
        {
            Debug.WriteLine($"bw status failed: {error}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BitwardenStatus>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Failed to parse status: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Unlocks the vault with the master password
    /// </summary>
    /// <param name="masterPassword">The master password</param>
    /// <returns>True if unlock was successful</returns>
    public async Task<(bool success, string message)> UnlockAsync(string masterPassword)
    {
        // Escape quotes in password for command line
        var escapedPassword = masterPassword.Replace("\"", "\\\"");
        var (output, error, exitCode) = await ExecuteCommandAsync($"unlock \"{escapedPassword}\" --raw");

        if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            // The --raw flag returns just the session key
            SessionKey = output.Trim();
            return (true, "Vault unlocked successfully");
        }

        // Try to extract session key from regular output if --raw didn't work
        if (!string.IsNullOrWhiteSpace(output))
        {
            var sessionMatch = Regex.Match(output, @"BW_SESSION=""([^""]+)""");
            if (sessionMatch.Success)
            {
                SessionKey = sessionMatch.Groups[1].Value;
                return (true, "Vault unlocked successfully");
            }
        }

        return (false, string.IsNullOrWhiteSpace(error) ? "Failed to unlock vault" : error);
    }

    /// <summary>
    /// Locks the vault
    /// </summary>
    public async Task<bool> LockAsync()
    {
        var (_, _, exitCode) = await ExecuteCommandAsync("lock");
        if (exitCode == 0)
        {
            SessionKey = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Syncs the vault with the server
    /// </summary>
    public async Task<bool> SyncAsync()
    {
        if (string.IsNullOrEmpty(SessionKey))
            return false;

        var (_, _, exitCode) = await ExecuteCommandAsync($"sync --session \"{SessionKey}\"");
        return exitCode == 0;
    }

    /// <summary>
    /// Gets all items from the vault
    /// </summary>
    public async Task<BitwardenItem[]?> GetItemsAsync()
    {
        if (string.IsNullOrEmpty(SessionKey))
            return null;

        var (output, error, exitCode) = await ExecuteCommandAsync($"list items --session \"{SessionKey}\"");

        if (exitCode != 0)
        {
            Debug.WriteLine($"bw list items failed: {error}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BitwardenItem[]>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Failed to parse items: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Searches for items matching the query
    /// </summary>
    public async Task<BitwardenItem[]?> SearchItemsAsync(string query)
    {
        if (string.IsNullOrEmpty(SessionKey))
            return null;

        var escapedQuery = query.Replace("\"", "\\\"");
        var (output, error, exitCode) = await ExecuteCommandAsync($"list items --search \"{escapedQuery}\" --session \"{SessionKey}\"");

        if (exitCode != 0)
        {
            Debug.WriteLine($"bw search failed: {error}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BitwardenItem[]>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Failed to parse search results: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if the vault is currently unlocked
    /// </summary>
    public bool IsUnlocked => !string.IsNullOrEmpty(SessionKey);

    /// <summary>
    /// Clears the current session
    /// </summary>
    public void ClearSession()
    {
        SessionKey = null;
    }
}
