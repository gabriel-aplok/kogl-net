using System.Numerics;
using ImGuiNET;
using Kogl.Core;

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

        // --- Top Control Panel ---
        if (ImGui.Button("Clear"))
        {
            Log.ClearHistory();
        }

        ImGui.SameLine();
        if (ImGui.Button("Scroll to Bottom"))
        {
            // Forces scroll to the bottom on the next frame inside the child window
            ImGui.SetNextWindowScroll(new Vector2(0, ImGui.GetScrollMaxY()));
        }

        ImGui.Separator();

        // --- Filters Area ---
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

        // Substring Search Bar
        ImGui.SetNextItemWidth(-1); // Stretch to fill the right border
        ImGui.InputTextWithHint(
            "##FilterInput",
            "Filter logs by message or category... (e.g. 'OPENGL')",
            ref _searchFilter,
            256
        );

        ImGui.Separator();

        // --- Scrolling Region Setup ---
        // Leave space for a sleek bottom command border or line status by adjusting height offset (-35)
        ImGui.BeginChild(
            "ScrollingRegion",
            new Vector2(0, -35),
            ImGuiChildFlags.None,
            ImGuiWindowFlags.HorizontalScrollbar
        );

        LogEntry[] logs = Log.GetHistorySnapshot();

        // Track if we are currently sitting at the absolute bottom of the log viewport before drawing
        bool wasAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY();

        foreach (LogEntry entry in logs)
        {
            // 1. Level Filter Validation
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

            // 2. Search Substring Validation (Checks message content & system categories)
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

            // 3. Color mapping
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

            // 4. Clean formatting
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $"[{entry.Timestamp:HH:mm:ss}]");
            ImGui.SameLine();
            ImGui.TextColored(color, $"[{entry.Level.ToString().ToUpper()}]");
            ImGui.SameLine();
            ImGui.TextUnformatted($"[{entry.Category}] {entry.Message}");
        }

        // --- Smart Auto-Scroll Handler ---
        // If the number of engine logs increases AND the user was already looking at the bottom,
        // snap the viewport down to track the new engine events automatically.
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
            // Reset state seamlessly if user clears history
            _lastLogCount = logs.Length;
        }

        ImGui.EndChild();
        ImGui.Separator();

        // --- Bottom StatusBar Display ---
        ImGui.TextDisabled($"Total Buffers: {logs.Length} entries | Current Filter Active");

        ImGui.End();
    }
}
