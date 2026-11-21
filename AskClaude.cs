using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace ActionRenamer;

/// <summary>One line in the chat: who said it and what.</summary>
public sealed record AskMessage(bool FromPlayer, string Text, bool IsError = false);

/// <summary>
/// Asks Claude through the Claude Code app installed on this PC, signed in with the player's own subscription
/// (no API key). Each question runs "claude -p" locally with everything switched off except web search:
/// no file or shell tools, no settings, hooks or MCP servers, in an empty folder.
/// </summary>
public sealed class AskClaude
{
    private const string SystemPrompt =
        "You are a helper inside a FINAL FANTASY XIV plugin. Answer the player's question briefly (under 120 words) and accurately. "
        + "The GAME DATA section comes from the player's own game files: trust it over memory. "
        + "Never contradict the game data. Duty lines are sorted by level, lowest first: in a type and expansion, the lowest-level duty is the first one unlocked. "
        + "Use web search only when the game data and the conversation do not answer the question. "
        + "If neither the game data nor a search answers it, say you are not sure instead of guessing. "
        + "Give map positions as Zone (x, y). Plain text only, no markdown headings or tables.";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

    private readonly Configuration config;
    private readonly GameKnowledge knowledge;
    private readonly IPluginLog log;
    private readonly string workDir;
    private readonly List<AskMessage> messages = new();
    private readonly object gate = new();

    public bool Busy { get; private set; }

    public AskClaude(Configuration config, GameKnowledge knowledge, IPluginLog log, string configDirectory)
    {
        this.config = config;
        this.knowledge = knowledge;
        this.log = log;
        workDir = Path.Combine(configDirectory, "ask");
    }

    public AskMessage[] Messages
    {
        get { lock (gate) return messages.ToArray(); }
    }

    public void Clear()
    {
        lock (gate) messages.Clear();
    }

    /// <summary>The Claude Code program: the path set in the settings, else the standard install, else PATH.</summary>
    public string ClaudePath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(config.ClaudePath))
                return config.ClaudePath;
            var standard = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe");
            return File.Exists(standard) ? standard : "claude";
        }
    }

    /// <summary>Starts answering in the background. <paramref name="playerContext"/> is gathered on the game thread.</summary>
    public void Ask(string question, string playerContext)
    {
        question = question.Trim();
        if (question.Length == 0 || Busy)
            return;
        List<AskMessage> earlier;
        lock (gate)
        {
            earlier = messages.Where(m => !m.IsError).TakeLast(6).ToList();
            messages.Add(new AskMessage(true, question));
        }
        Busy = true;
        Task.Run(() =>
        {
            AskMessage reply;
            try
            {
                reply = Run(BuildPrompt(question, playerContext, earlier));
            }
            catch (Exception e)
            {
                log.Error(e, "Ask Claude failed");
                reply = new AskMessage(false, "Something went wrong: " + e.Message, true);
            }
            lock (gate) messages.Add(reply);
            Busy = false;
        });
    }

    private string BuildPrompt(string question, string playerContext, List<AskMessage> earlier)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PLAYER: " + playerContext);
        var facts = knowledge.Lookup(question);
        if (facts.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("GAME DATA (matches for the question):");
            sb.Append(facts);
        }
        if (earlier.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("EARLIER IN THIS CHAT:");
            foreach (var m in earlier)
                sb.AppendLine((m.FromPlayer ? "Player: " : "You: ") + m.Text);
        }
        sb.AppendLine();
        sb.AppendLine("QUESTION: " + question);
        return sb.ToString();
    }

    private AskMessage Run(string prompt)
    {
        Directory.CreateDirectory(workDir);
        var psi = new ProcessStartInfo(ClaudePath)
        {
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in new[]
        {
            "-p", "--model", config.AskModel is "sonnet" ? "sonnet" : "haiku",
            "--tools", config.AskWebSearch ? "WebSearch" : "",
            "--setting-sources", "",
            "--strict-mcp-config",
            "--no-session-persistence",
            "--output-format", "json",
            "--system-prompt", SystemPrompt,
        })
            psi.ArgumentList.Add(arg);
        if (config.AskWebSearch)
        {
            psi.ArgumentList.Add("--allowedTools");
            psi.ArgumentList.Add("WebSearch");
        }

        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException("Claude Code did not start.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new AskMessage(false, "Claude Code was not found on this PC. Install it, sign in once by running \"claude\", or set its path in Settings > General.", true);
        }

        using (process)
        {
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            process.StandardInput.Write(prompt);
            process.StandardInput.Close();

            if (!process.WaitForExit(Timeout))
            {
                try { process.Kill(true); } catch { /* already gone */ }
                return new AskMessage(false, "Claude took too long to answer. Try again.", true);
            }

            var json = output.Result;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var text = root.TryGetProperty("result", out var r) ? r.GetString() ?? string.Empty : string.Empty;
                var isError = root.TryGetProperty("is_error", out var e) && e.ValueKind == JsonValueKind.True;
                if (text.Length == 0)
                    return new AskMessage(false, "Claude sent an empty answer.", true);
                return new AskMessage(false, text.Trim(), isError);
            }
            catch (JsonException)
            {
                var detail = (errors.Result.Length > 0 ? errors.Result : json).Trim();
                if (detail.Contains("login", StringComparison.OrdinalIgnoreCase) || detail.Contains("auth", StringComparison.OrdinalIgnoreCase))
                    return new AskMessage(false, "Claude Code is not signed in. Run \"claude\" once on this PC and sign in.", true);
                return new AskMessage(false, "Claude Code answered in an unexpected way: " + detail[..Math.Min(detail.Length, 300)], true);
            }
        }
    }
}
