using System;
using System.Collections.Generic;
using System.IO;

namespace AMEFManager.Helpers;

/// <summary>
/// Unified cross-platform template resolution for ANAF document templates (F4102, C801, C802, etc.).
/// Probes OS-specific canonical application data directories, application base directory,
/// working directory, project root, and optional custom paths.
/// </summary>
public static class TemplatePathResolver
{
    /// <summary>
    /// Resolves the absolute path to a template file by probing prioritized candidate locations.
    /// </summary>
    /// <param name="templateFileName">The template file name (e.g. "F4102_template.pdf").</param>
    /// <param name="callerContext">Optional caller context name for structured diagnostic logging.</param>
    /// <param name="customDirectory">Optional custom directory to probe with highest priority.</param>
    /// <returns>The absolute path to the existing template file.</returns>
    /// <exception cref="ArgumentException">Thrown when templateFileName is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the template cannot be found at any probed candidate path.</exception>
    public static string ResolveTemplatePath(string templateFileName, string? callerContext = null, string? customDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(templateFileName))
        {
            throw new ArgumentException("Template file name cannot be null or whitespace.", nameof(templateFileName));
        }

        string logContext = callerContext ?? nameof(TemplatePathResolver);
        AppLogger.LogDebug($"[{logContext}] Starting template path resolution for '{templateFileName}'...");

        var candidates = GetCandidatePaths(templateFileName, customDirectory);
        var probedPaths = new List<string>();

        foreach (var path in candidates)
        {
            probedPaths.Add(path);
            bool exists = File.Exists(path);
            AppLogger.LogDebug($"[{logContext}] Probing template path: '{path}' (Exists={exists})");

            if (exists)
            {
                var fileInfo = new FileInfo(path);
                AppLogger.LogInfo($"[{logContext}] Successfully resolved template '{templateFileName}' -> '{path}' (Size: {fileInfo.Length} bytes)");
                return path;
            }
        }

        string probedSummary = string.Join(Environment.NewLine + "  - ", probedPaths);
        var fnfEx = new FileNotFoundException(
            $"Template PDF '{templateFileName}' not found. Probed locations:{Environment.NewLine}  - {probedSummary}",
            templateFileName);

        AppLogger.LogError($"[{logContext}] Template PDF resolution failed for '{templateFileName}'.", fnfEx);
        throw fnfEx;
    }

    /// <summary>
    /// Returns a prioritized, deduplicated list of candidate absolute paths for the specified template.
    /// </summary>
    /// <param name="templateFileName">The template file name.</param>
    /// <param name="customDirectory">Optional custom directory to prioritize.</param>
    /// <returns>List of candidate paths.</returns>
    public static List<string> GetCandidatePaths(string templateFileName, string? customDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(templateFileName))
        {
            return new List<string>();
        }

        var candidateList = new List<string>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                string normalized = Path.GetFullPath(path);
                if (seenPaths.Add(normalized))
                {
                    candidateList.Add(normalized);
                }
            }
            catch
            {
                // Ignore malformed paths
            }
        }

        // 1. Custom directory override (if provided)
        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            AddCandidate(Path.Combine(customDirectory, templateFileName));
            AddCandidate(Path.Combine(customDirectory, "templates", templateFileName));
        }

        // 2. OS Canonical locations
        if (OperatingSystem.IsMacOS())
        {
            // macOS Primary: ~/Library/Application Support/AMEFManager/templates/{templateFileName}
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                AddCandidate(Path.Combine(userProfile, "Library", "Application Support", "AMEFManager", "Templates", templateFileName));
                AddCandidate(Path.Combine(userProfile, "Library", "Application Support", "AMEFManager", "templates", templateFileName));
            }

            string personalDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrWhiteSpace(personalDir))
            {
                AddCandidate(Path.Combine(personalDir, "Library", "Application Support", "AMEFManager", "Templates", templateFileName));
                AddCandidate(Path.Combine(personalDir, "Library", "Application Support", "AMEFManager", "templates", templateFileName));
            }

            // Fallbacks for macOS: %APPDATA%
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                AddCandidate(Path.Combine(appData, "AMEFManager", "templates", templateFileName));
            }
        }
        else
        {
            // Windows / Linux Primary: %APPDATA%\AMEFManager\templates\{templateFileName}
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                AddCandidate(Path.Combine(appData, "AMEFManager", "templates", templateFileName));
            }

            // %LOCALAPPDATA%\AMEFManager\templates\{templateFileName}
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                AddCandidate(Path.Combine(localAppData, "AMEFManager", "templates", templateFileName));
            }

            // Fallback for Windows/Linux: macOS path pattern
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                AddCandidate(Path.Combine(userProfile, "Library", "Application Support", "AMEFManager", "templates", templateFileName));
            }

            string personalDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrWhiteSpace(personalDir))
            {
                AddCandidate(Path.Combine(personalDir, "Library", "Application Support", "AMEFManager", "templates", templateFileName));
            }
        }

        // 3. Application Base Directory: AppContext.BaseDirectory / templates
        string baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(baseDir))
        {
            AddCandidate(Path.Combine(baseDir, "templates", templateFileName));
            AddCandidate(Path.Combine(baseDir, templateFileName));
        }

        // 4. Current Working Directory: Directory.GetCurrentDirectory() / templates
        string currentDir = Directory.GetCurrentDirectory();
        if (!string.IsNullOrWhiteSpace(currentDir))
        {
            AddCandidate(Path.Combine(currentDir, "templates", templateFileName));
            AddCandidate(Path.Combine(currentDir, templateFileName));
        }

        // 5. Development project root traversal
        ProbeProjectRoot(baseDir, templateFileName, AddCandidate);
        ProbeProjectRoot(currentDir, templateFileName, AddCandidate);

        return candidateList;
    }

    private static void ProbeProjectRoot(string startingDir, string templateFileName, Action<string?> addCandidate)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(startingDir) || !Directory.Exists(startingDir))
                return;

            var dir = new DirectoryInfo(startingDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "AMEFManager.csproj")) ||
                    File.Exists(Path.Combine(dir.FullName, "AMEFManager.sln")))
                {
                    addCandidate(Path.Combine(dir.FullName, "templates", templateFileName));
                    addCandidate(Path.Combine(dir.FullName, templateFileName));
                    break;
                }
                dir = dir.Parent;
            }
        }
        catch
        {
            // Ignore filesystem access exceptions when probing root
        }
    }
}
