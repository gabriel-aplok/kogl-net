using System.Numerics;
using ImGuiNET;
using Kogl.Common;

namespace Kogl.Windowing.ImGuiImpl;

public static class ImGuiConsole
{
    private static string _searchFilter = string.Empty;
    private static bool _showTrace = true;
    private static bool _showDebug = true;
    private static bool _showInfo = true;
    private static bool _showWarn = true;
    private static bool _showError = true;
    private static bool _showCritical = true;

    private static int _lastLogCount = 0;

    public static void DrawConsoleWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(600, 400), ImGuiCond.FirstUseEver);
        ImGui.Begin("Console Log");

        // top bar
        if (ImGui.Button("Clear"))
        {
            LogCat.ClearHistory();
        }

        ImGui.SameLine();
        if (ImGui.Button("Scroll to Bottom"))
        {
            ImGui.SetScrollHereY(ImGui.GetScrollMaxY());
        }

        ImGui.Separator();

        // filters
        ImGui.Text("Filters:");
        ImGui.SameLine();
        ImGui.Checkbox("Trace", ref _showTrace);
        ImGui.SameLine();
        ImGui.Checkbox("Debug", ref _showDebug);
        ImGui.SameLine();
        ImGui.Checkbox("Info", ref _showInfo);
        ImGui.SameLine();
        ImGui.Checkbox("Warn", ref _showWarn);
        ImGui.SameLine();
        ImGui.Checkbox("Error", ref _showError);
        ImGui.SameLine();
        ImGui.Checkbox("Critical", ref _showCritical);

        // substring search bar
        ImGui.SetNextItemWidth(-1); // stretch to fill the right border
        ImGui.InputTextWithHint(
            "##FilterInput",
            "Filter logs by message or category...",
            ref _searchFilter,
            256
        );

        ImGui.Separator();

        // scrolling region
        ImGui.BeginChild(
            "ScrollingRegion",
            new Vector2(0, -35),
            ImGuiChildFlags.None,
            ImGuiWindowFlags.HorizontalScrollbar
        );

        LogEntry[] logs = LogCat.GetHistorySnapshot();
        bool wasAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY();

        foreach (LogEntry entry in logs)
        {
            // level filter validation
            bool shouldDisplay = entry.Level switch
            {
                LogLevel.Trace => _showTrace,
                LogLevel.Debug => _showDebug,
                LogLevel.Info => _showInfo,
                LogLevel.Warn => _showWarn,
                LogLevel.Error => _showError,
                LogLevel.Critical => _showCritical,
                _ => true,
            };
            if (!shouldDisplay)
                continue;

            // search substring validation
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                bool matchesCategory = entry.Category.Contains(
                    _searchFilter,
                    StringComparison.OrdinalIgnoreCase
                );
                bool matchesMessage = entry.Message.Contains(
                    _searchFilter,
                    StringComparison.OrdinalIgnoreCase
                );
                if (!matchesCategory && !matchesMessage)
                {
                    continue;
                }
            }

            // color mapping
            Vector4 color = entry.Level switch
            {
                LogLevel.Trace => new Vector4(0.6f, 0.6f, 0.6f, 1.0f), // Dim Gray
                LogLevel.Debug => new Vector4(0.3f, 0.7f, 1.0f, 1.0f), // Light Cyan
                LogLevel.Info => new Vector4(0.4f, 1.0f, 0.4f, 1.0f), // Vibrant Green
                LogLevel.Warn => new Vector4(1.0f, 0.8f, 0.0f, 1.0f), // Warm Yellow
                LogLevel.Error => new Vector4(1.0f, 0.2f, 0.2f, 1.0f), // Red
                LogLevel.Critical => new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Magenta
                _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            };

            // clean formatting
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $"[{entry.Timestamp:HH:mm:ss}]");
            ImGui.SameLine();
            ImGui.TextColored(color, $"[{entry.Level.ToString().ToUpper()}]");
            ImGui.SameLine();
            ImGui.TextUnformatted($"[{entry.Category}] {entry.Message}");
        }

        // auto-scroll
        if (logs.Length > _lastLogCount)
        {
            if (wasAtBottom)
            {
                ImGui.SetScrollHereY(1.0f);
            }
            _lastLogCount = logs.Length;
        }
        else if (logs.Length < _lastLogCount)
        {
            // reset state seamlessly if user clears history
            _lastLogCount = logs.Length;
        }

        ImGui.EndChild();
        ImGui.Separator();

        // bottom statusbar display
        ImGui.TextDisabled($"Total Buffers: {logs.Length} entries | Current Filter Active");

        ImGui.End();
    }
}
