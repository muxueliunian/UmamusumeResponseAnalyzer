using Spectre.Console.Rendering;

namespace UmamusumeResponseAnalyzer.LiveDisplay;

public interface ILiveDisplayOutput
{
    LiveDisplayWorkspace? CurrentWorkspace { get; }
    LiveDisplayWorkspace CreateWorkspace(string title);
    void SwitchWorkspace(LiveDisplayWorkspace workspace);
    void BindWorkspaceHotkey(LiveDisplayWorkspace workspace, ConsoleKey key, ConsoleModifiers modifiers = 0, string? description = null);
    void SetPanel(LiveDisplayWorkspace workspace, string key, string title, IRenderable content, bool fullBleed = false);
    void SetPanel(LiveDisplayWorkspace workspace, string key, string title, IRenderable content, bool fullBleed, bool switchToWorkspace)
    {
        if (!switchToWorkspace)
            throw new NotSupportedException("当前 LiveDisplay output 实现不支持 switchToWorkspace: false。");

        SetPanel(workspace, key, title, content, fullBleed);
    }

    void Log(LiveDisplayWorkspace workspace, string text, LiveDisplaySeverity severity = LiveDisplaySeverity.Info);
    void MarkupLog(LiveDisplayWorkspace workspace, string markup, LiveDisplaySeverity severity = LiveDisplaySeverity.Info);
    void Notify(LiveDisplayWorkspace workspace, string text, LiveDisplaySeverity severity = LiveDisplaySeverity.Info, TimeSpan? ttl = null);
}

public sealed class LiveDisplayWorkspace : IEquatable<LiveDisplayWorkspace>
{
    static readonly StringComparer TitleComparer = StringComparer.OrdinalIgnoreCase;

    LiveDisplayWorkspace(string title)
    {
        Title = Normalize(title, nameof(title));
    }

    public string Title { get; }

    public static LiveDisplayWorkspace Create(string title)
    {
        return new LiveDisplayWorkspace(title);
    }

    public bool Equals(LiveDisplayWorkspace? other)
    {
        return other is not null && TitleComparer.Equals(Title, other.Title);
    }

    public override bool Equals(object? obj)
    {
        return obj is LiveDisplayWorkspace other && Equals(other);
    }

    public override int GetHashCode()
    {
        return TitleComparer.GetHashCode(Title);
    }

    public override string ToString()
    {
        return Title;
    }

    public static bool operator ==(LiveDisplayWorkspace? left, LiveDisplayWorkspace? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(LiveDisplayWorkspace? left, LiveDisplayWorkspace? right)
    {
        return !(left == right);
    }

    static string Normalize(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Workspace title 不能为空。", parameterName);

        return value.Trim();
    }
}

public enum LiveDisplaySeverity
{
    Trace,
    Info,
    Success,
    Warning,
    Error,
}
