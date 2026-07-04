namespace UmamusumeResponseAnalyzer
{
    internal interface IKeyboardOverlaySink
    {
        void ShowPopup(KeyboardPopup popup);
        void HidePopup();
        void ShowCommandInput(KeyboardCommandInput input);
        void HideCommandInput();
    }

    internal sealed record KeyboardPopup(
        IReadOnlyList<KeyboardPopupLine> Lines,
        int ScrollOffset = 0,
        DateTimeOffset? ExpiresAt = null,
        KeyboardPopupSelection? Selection = null);

    internal sealed record KeyboardPopupSelection(
        IReadOnlyList<int> LineIndexes,
        int SelectedIndex,
        Func<int, Task> ConfirmAsync)
    {
        public int BoundedSelectedIndex => LineIndexes.Count == 0
            ? -1
            : Math.Clamp(SelectedIndex, 0, LineIndexes.Count - 1);

        public int SelectedLineIndex => BoundedSelectedIndex < 0 ? -1 : LineIndexes[BoundedSelectedIndex];

        public KeyboardPopupSelection Normalize()
        {
            var selectedIndex = BoundedSelectedIndex;
            return selectedIndex == SelectedIndex ? this : this with { SelectedIndex = selectedIndex };
        }
    }

    internal sealed record KeyboardPopupLine(string Text, ConsoleColor Color, bool IsMarkup);
    internal sealed record KeyboardCommandInput(string Text, IReadOnlyList<string> CompletionCandidates)
    {
        public KeyboardCommandInput(string text) : this(text, [])
        {
        }
    }

    public sealed class KeyboardHandlerContext
    {
        readonly List<KeyboardPopupLine> lines = [];

        public int LineCount => lines.Count;

        public KeyboardHandlerContext WriteLine(string text = "", ConsoleColor color = ConsoleColor.White)
        {
            lines.Add(new KeyboardPopupLine(text, color, IsMarkup: false));
            return this;
        }

        public KeyboardHandlerContext MarkupLine(string markup = "")
        {
            lines.Add(new KeyboardPopupLine(markup, ConsoleColor.White, IsMarkup: true));
            return this;
        }

        internal KeyboardPopup ToPopup(int scrollOffset = 0, KeyboardPopupSelection? selection = null)
        {
            return new KeyboardPopup(lines.ToArray(), scrollOffset, Selection: selection);
        }
    }
}
