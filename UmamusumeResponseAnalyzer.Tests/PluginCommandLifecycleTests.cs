using System.Reflection;
using Spectre.Console;
using Spectre.Console.Rendering;
using UmamusumeResponseAnalyzer.LiveDisplay;
using UmamusumeResponseAnalyzer.Plugin;
using Xunit;

namespace UmamusumeResponseAnalyzer.Tests
{
    [Collection("PluginReload")]
    public sealed class PluginCommandLifecycleTests : IDisposable
    {
        readonly string _originalCwd;
        readonly string _tempDir;

        public PluginCommandLifecycleTests()
        {
            ResetKeyboardManager();
            SeedConfig();
            ResetPluginState();

            _originalCwd = Directory.GetCurrentDirectory();
            _tempDir = Path.Combine(Path.GetTempPath(), "ura-plugin-command-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "Plugins"));
            Directory.SetCurrentDirectory(_tempDir);
        }

        public void Dispose()
        {
            ResetKeyboardManager();
            ResetPluginState();
            Directory.SetCurrentDirectory(_originalCwd);
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public async Task PluginCommand_ReloadSuccessShowsProgressPopup()
        {
            var pluginPath = Path.Combine(_tempDir, "Plugins", "CommandPopup.dll");
            PluginCompiler.Compile(PluginSource("CommandPopup"), "CommandPopup", pluginPath);
            PluginManager.Init();

            var uiHost = new UiHost();
            KeyboardManager.OverlaySink = uiHost;
            var workspace = uiHost.CreateWorkspace("插件");
            uiHost.SetPanel(new LiveDisplayPanel(
                workspace,
                "Plugin",
                "main",
                "插件",
                new Panel("PluginBody").Expand(),
                DateTimeOffset.Now));

            await uiHost.HandleCommandAsync("/plugin reload CommandPopup");
            var output = Render(uiHost, width: 120, height: 35);

            Assert.Contains("Plugin command", output);
            Assert.Contains("CommandPopup", output);
            Assert.Contains("已重载", output);
            Assert.DoesNotContain("用法: /plugin", output);
        }

        static string PluginSource(string pluginName)
            => $$"""
                using System.Threading.Tasks;
                using Spectre.Console;
                using UmamusumeResponseAnalyzer.Plugin;

                public sealed class {{pluginName}} : IPlugin
                {
                    public string Name => "{{pluginName}}";
                    public string Author => "Test";
                    public string[] Targets => System.Array.Empty<string>();

                    public void Initialize(IPluginContext context) { }
                    public Task UpdatePlugin(ProgressContext ctx) => Task.CompletedTask;
                }
                """;

        static void ResetKeyboardManager()
        {
            using (KeyboardManager.SuspendInput())
            {
            }

            KeyboardManager.UnregisterAll();
            KeyboardManager.SetCommandHandler(null);
            KeyboardManager.OverlaySink = null;
            KeyboardManager.PopupAutoCloseDelay = TimeSpan.FromSeconds(3);
            LiveDisplayConsole.UnbindForTests();
        }

        static void ResetPluginState()
        {
            PluginManager.RequestAnalyzerMethods.Clear();
            PluginManager.ResponseAnalyzerMethods.Clear();
            PluginManager.ClearHostEventSubscriptions();
            PluginManager.Metadatas.Clear();
            PluginManager.AssemblyMetadatas.Clear();
            PluginManager.FailedPlugins.Clear();
            PluginManager.ContextGroups.Clear();
            foreach (var context in PluginManager.Contexts.Values)
                context.Unload();
            PluginManager.Contexts.Clear();
            PluginManager.AssemblyMap.Clear();
            PluginManager.Assemblies.Clear();
            foreach (var plugin in PluginManager.LoadedPlugins.ToList())
                KeyboardManager.UnregisterByOwner(plugin);
            PluginManager.LoadedPlugins.Clear();
        }

        static void SeedConfig()
        {
            var current = typeof(Config).GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Static)!;
            if (current.GetValue(null) is null)
                current.SetValue(null, new YamlConfig
                {
                    Core = new(),
                    Repository = new(),
                    Plugin = new(),
                    Updater = new(),
                    Language = new(),
                    Misc = new(),
                });
        }

        static string Render(UiHost uiHost, int width, int height)
        {
            var recording = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new FixedSizeConsoleOutput(recording, width, height) });
            uiHost.RenderSnapshot(console);
            return recording.ToString();
        }

        sealed class FixedSizeConsoleOutput(TextWriter writer, int width, int height) : IAnsiConsoleOutput
        {
            public TextWriter Writer { get; } = writer;
            public bool IsTerminal => false;
            public int Width { get; } = width;
            public int Height { get; } = height;

            public void SetEncoding(System.Text.Encoding encoding)
            {
            }
        }
    }
}
