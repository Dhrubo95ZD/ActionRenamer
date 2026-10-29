using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace ActionRenamer;

/// <summary>
/// Installs the bundled icon mod (ActionRenamerIcons.pmp) into Penumbra and enables it for the
/// Interface collection. Runs once per new icon-pack version; waits for Penumbra if it loads later.
/// </summary>
public sealed class IconPackInstaller : IDisposable
{
    public const string ModName = "Action Renamer Icons";

    private readonly IDalamudPluginInterface pi;
    private readonly IFramework framework;
    private readonly IChatGui chat;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly Action save;
    private readonly IDisposable modAdded;

    private DateTime nextTry = DateTime.MinValue;
    private DateTime giveUp;
    private bool waitingForInstall;

    public string PackPath { get; }
    public string? BundledVersion { get; }
    public string Status { get; private set; } = "Not checked yet.";

    public IconPackInstaller(IDalamudPluginInterface pi, IFramework framework, IChatGui chat, IPluginLog log, Configuration config, Action save)
    {
        this.pi = pi;
        this.framework = framework;
        this.chat = chat;
        this.log = log;
        this.config = config;
        this.save = save;

        PackPath = Path.Combine(pi.AssemblyLocation.DirectoryName!, "ActionRenamerIcons.pmp");
        BundledVersion = ReadPackVersion(PackPath);
        modAdded = ModAdded.Subscriber(pi, OnModAdded);

        giveUp = DateTime.UtcNow.AddMinutes(3);
        framework.Update += OnUpdate;
    }

    /// <summary>Installs again even if this version was installed before.</summary>
    public void Reinstall()
    {
        config.InstalledIconPackVersion = string.Empty;
        save();
        waitingForInstall = false;
        giveUp = DateTime.UtcNow.AddMinutes(3);
        nextTry = DateTime.MinValue;
        framework.Update -= OnUpdate;
        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        if (DateTime.UtcNow < nextTry || waitingForInstall)
            return;
        nextTry = DateTime.UtcNow.AddSeconds(5);

        if (!config.AutoInstallIcons || BundledVersion == null || config.InstalledIconPackVersion == BundledVersion)
        {
            Status = BundledVersion == null ? "No icon pack bundled with this build." : $"Icon pack {config.InstalledIconPackVersion} installed.";
            framework.Update -= OnUpdate;
            return;
        }

        try
        {
            new ApiVersion(pi).Invoke();
        }
        catch
        {
            Status = "Waiting for Penumbra...";
            if (DateTime.UtcNow > giveUp)
            {
                Status = "Penumbra not found. Install Penumbra, then press Reinstall icons.";
                framework.Update -= OnUpdate;
            }
            return;
        }

        try
        {
            // Replace an older copy so Penumbra does not keep both.
            new DeleteMod(pi).Invoke(ModName, ModName);
            var result = new InstallMod(pi).Invoke(PackPath);
            if (result is not PenumbraApiEc.Success)
            {
                Status = $"Penumbra refused the icon pack: {result}.";
                log.Warning(Status);
                framework.Update -= OnUpdate;
                return;
            }
            waitingForInstall = true;
            Status = "Installing icon pack in Penumbra...";
        }
        catch (Exception e)
        {
            Status = "Icon pack install failed; see dalamud.log.";
            log.Error(e, "Icon pack install failed");
            framework.Update -= OnUpdate;
        }
    }

    private void OnModAdded(string modDirectory)
    {
        if (!waitingForInstall || !modDirectory.StartsWith(ModName, StringComparison.OrdinalIgnoreCase))
            return;
        waitingForInstall = false;
        framework.Update -= OnUpdate;

        try
        {
            foreach (var type in new[] { ApiCollectionType.Interface, ApiCollectionType.Default })
            {
                var collection = new GetCollection(pi).Invoke(type);
                if (collection != null)
                    new TrySetMod(pi).Invoke(collection.Value.Id, modDirectory, true);
            }
            config.InstalledIconPackVersion = BundledVersion ?? string.Empty;
            save();
            Status = $"Icon pack {BundledVersion} installed.";
            chat.Print("[Action Renamer] New skill icons installed in Penumbra. Log out and back in to see them.");
        }
        catch (Exception e)
        {
            Status = "Icon pack installed, but enabling it failed. Enable \"Action Renamer Icons\" in Penumbra.";
            log.Error(e, "Enabling icon pack failed");
        }
    }

    private static string? ReadPackVersion(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            using var zip = ZipFile.OpenRead(path);
            using var meta = zip.GetEntry("meta.json")!.Open();
            using var doc = JsonDocument.Parse(meta);
            return doc.RootElement.GetProperty("Version").GetString();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        framework.Update -= OnUpdate;
        modAdded.Dispose();
    }
}
