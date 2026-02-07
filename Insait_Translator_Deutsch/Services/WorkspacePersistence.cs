using System;
using System.Collections.Generic;
using System.Text.Json;
using Insait_Translator_Deutsch.ViewModels;

namespace Insait_Translator_Deutsch.Services;

/// <summary>
/// Persists workspace tabs using a dedicated LiteDB database with 100MB size limit.
/// Falls back gracefully if database is unavailable or corrupted.
/// </summary>
public static class WorkspacePersistence
{
    private const string Prefix = "Workspaces.";
    private const string SelectedIndexKey = "Workspaces.SelectedIndex";

    private sealed record WorkspaceDto(int Id, string Title, MainTab SelectedTab, string UkrainianText, string GermanText);

    public static (List<WorkspaceTab> tabs, int selectedIndex) Load()
    {
        var workspaceDb = WorkspaceDatabaseService.Instance;
        
        var selectedIndex = 0;
        if (int.TryParse(workspaceDb.GetEntry(SelectedIndexKey, "0"), out var idx))
            selectedIndex = Math.Clamp(idx, 0, 2);

        var result = new List<WorkspaceTab>();
        for (var i = 0; i < 3; i++)
        {
            var json = workspaceDb.GetEntry(Prefix + i, "");
            if (string.IsNullOrWhiteSpace(json))
                continue;

            try
            {
                var dto = JsonSerializer.Deserialize<WorkspaceDto>(json);
                if (dto == null) continue;

                result.Add(new WorkspaceTab
                {
                    Id = dto.Id,
                    Title = dto.Title,
                    SelectedTab = dto.SelectedTab,
                    UkrainianText = dto.UkrainianText,
                    GermanText = dto.GermanText
                });
            }
            catch
            {
                // ignore corrupted entry
            }
        }

        return (result, selectedIndex);
    }

    public static void Save(IReadOnlyList<WorkspaceTab> tabs, int selectedIndex)
    {
        var workspaceDb = WorkspaceDatabaseService.Instance;
        
        workspaceDb.SetEntry(SelectedIndexKey, selectedIndex.ToString());

        for (var i = 0; i < 3; i++)
        {
            if (i >= tabs.Count)
            {
                workspaceDb.RemoveEntry(Prefix + i);
                continue;
            }

            var t = tabs[i];
            var dto = new WorkspaceDto(t.Id, t.Title, t.SelectedTab, t.UkrainianText, t.GermanText);
            var json = JsonSerializer.Serialize(dto);
            workspaceDb.SetEntry(Prefix + i, json);
        }
    }

    /// <summary>
    /// Gets the current workspace database size in MB.
    /// </summary>
    public static double GetDatabaseSizeMB()
    {
        return WorkspaceDatabaseService.Instance.GetDatabaseSizeBytes() / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Compacts the workspace database to free up space.
    /// </summary>
    public static void CompactDatabase()
    {
        WorkspaceDatabaseService.Instance.Compact();
    }
}

