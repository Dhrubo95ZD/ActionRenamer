using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ActionRenamer;

/// <summary>
/// Sci-fi theme for status effect names. Full-name entries win; otherwise each word is swapped
/// through <see cref="Words"/>. Names with no themed word stay as they are ("Damage Up").
/// </summary>
public static class StatusTheme
{
    /// <summary>Whole-name renames for well-known buffs and debuffs.</summary>
    public static readonly Dictionary<string, string> FullNames = new()
    {
        // General
        ["Regen"] = "Nano-Regen",
        ["Medica II"] = "Med-Field II",
        ["Medica III"] = "Med-Field III",
        ["Doom"] = "Countdown",
        ["Brink of Death"] = "Critical Condition",
        ["Weakness"] = "System Fatigue",
        ["Transcendent"] = "Reboot Shield",
        ["Resurrection Restricted"] = "Revival Lockout",
        ["Raise"] = "Revival Signal",
        ["Haste"] = "Overclock",
        ["Slow"] = "Lag",
        ["Bind"] = "Tether",
        ["Sleep"] = "Stasis",
        ["Silence"] = "Comms Jam",
        ["Blind"] = "Optic Jam",
        ["Paralysis"] = "Neural Jam",
        ["Stun"] = "Shock",
        ["Confused"] = "Glitched",
        ["Pacification"] = "Weapons Lockout",
        ["Stoneskin"] = "Ferro-Skin",
        ["Charm"] = "Neural Hijack",
        ["Disease"] = "Virus",
        ["Poison"] = "Toxin",
        ["Lucid Dreaming"] = "Power Saver",
        ["Swiftcast"] = "Quick Boot",
        ["Surecast"] = "Anchor Lock",
        // Tanks
        ["Hallowed Ground"] = "Invulnerability Field",
        ["Requiescat"] = "Overcharge",
        ["Holmgang"] = "Holdfast",
        ["Living Dead"] = "Last Stand",
        ["Walking Dead"] = "Final Countdown",
        ["Delirium"] = "Overdrive",
        ["Darkside"] = "Null Mode",
        ["The Blackest Night"] = "Black Barrier",
        // Healers
        ["Asylum"] = "Med Zone",
        ["Aquaveil"] = "Hydro Veil",
        ["Divine Benison"] = "Prime Barrier",
        ["Liturgy of the Bell"] = "Pulse Beacon",
        ["Seraphism"] = "Drone Overdrive",
        ["Fey Union"] = "Drone Link",
        ["Kardia"] = "Bio-Link",
        ["Kardion"] = "Bio-Linked",
        ["Physis"] = "Nanite Mist",
        ["Physis II"] = "Nanite Mist II",
        ["Kerachole"] = "Kera-Shield",
        ["Haima"] = "Hemo-Shield",
        ["Panhaima"] = "Wide Hemo-Shield",
        ["Haimatinon"] = "Hemo-Layer",
        ["Panhaimatinon"] = "Wide Hemo-Layer",
        ["Holos"] = "Holo-Field",
        ["Holosakos"] = "Holo-Barrier",
        ["Krasis"] = "Mixer Boost",
        ["Soteria"] = "Recovery Boost",
        ["Zoe"] = "Amp",
        ["Eukrasia"] = "Augment",
        ["Synastry"] = "Sync-Link",
        ["Divination"] = "Prediction Engine",
        ["Earthly Star"] = "Ground Beacon",
        ["Giant Dominance"] = "Beacon Charged",
        ["Horoscope"] = "Forecast",
        ["Horoscope Helios"] = "Forecast Field",
        ["Neutral Sect"] = "Neutral Mode",
        ["Macrocosmos"] = "Macro Field",
        ["Lady of Crowns"] = "Crown Card II",
        ["Lord of Crowns"] = "Crown Card",
        ["The Balance"] = "Melee Data Card",
        ["The Spear"] = "Ranged Data Card",
        // Casters and ranged
        ["Ley Lines"] = "Power Grid",
        ["Firestarter"] = "Ignition",
        ["Thunderhead"] = "Arc Charge",
        ["Enochian"] = "Core Overcharge",
        ["Manafication"] = "Flux Overload",
        ["Dualcast"] = "Dual Process",
        ["Devilment"] = "Overdrive Step",
        ["Troubadour"] = "Sonic Shield",
        ["Hyperphantasia"] = "Hyper Render",
        ["Reawakened"] = "Rebooted",
        ["Enshrouded"] = "Cloaked",
        ["Soulsow"] = "Core Charge",
        ["Death's Design"] = "Termination Mark",
        ["Immortal Sacrifice"] = "Overload Stack",
        ["Mudra"] = "Hand Sequence",
        ["Ten Chi Jin"] = "Triple Sequence",
        ["Kassatsu"] = "Override",
        ["Meikyo Shisui"] = "Mirror Stance",
    };

    /// <summary>Word swaps. Keys are capitalised; lower-case forms are added automatically.</summary>
    public static readonly Dictionary<string, string> Words = new()
    {
        // Magic and spiritual words
        ["Soul"] = "Core", ["Souls"] = "Cores", ["Soulbound"] = "Core-Bound",
        ["Spirit"] = "Drive", ["Spirits"] = "Drives", ["Spiritual"] = "Neural",
        ["Magic"] = "Tech", ["Magical"] = "Tech", ["Magicked"] = "Powered",
        ["Spell"] = "Program", ["Spells"] = "Programs", ["Spellbound"] = "Hacked",
        ["Phantom"] = "Holo", ["Ghost"] = "Holo", ["Ghostly"] = "Holo",
        ["Divine"] = "Prime", ["Holy"] = "Photon", ["Hallowed"] = "Shielded", ["Sacred"] = "Secure",
        ["Blessing"] = "Uplink", ["Blessings"] = "Uplinks", ["Blessed"] = "Uplinked",
        ["Curse"] = "Malware", ["Cursed"] = "Infected", ["Hex"] = "Malware",
        ["Astral"] = "Stellar", ["Umbral"] = "Void", ["Aetherial"] = "Energy", ["Aether"] = "Energy",
        ["Arcane"] = "Quantum", ["Occult"] = "Black-Box", ["Mystic"] = "Cipher", ["Mythic"] = "Legendary",
        ["Fey"] = "Drone", ["Faerie"] = "Drone", ["Fairy"] = "Drone",
        ["Summon"] = "Deploy", ["Summoned"] = "Deployed", ["Mage"] = "Engineer", ["Mage's"] = "Engineer's",
        ["Mana"] = "Flux", ["Sorcery"] = "Tech", ["Sorcerer"] = "Technician", ["Witch"] = "Engineer", ["Wizard"] = "Engineer",
        ["Enchanted"] = "Augmented", ["Enchantment"] = "Augment", ["Rune"] = "Code", ["Runic"] = "Coded",
        ["Ritual"] = "Procedure", ["Altar"] = "Platform", ["Shrine"] = "Station", ["Relic"] = "Artifact",
        ["Saint"] = "Paragon", ["Saint's"] = "Paragon's", ["Angel"] = "Guardian Drone", ["Angelic"] = "Guardian",
        ["Seraph"] = "Sentinel Drone", ["Demon"] = "Rogue Mech", ["Demonic"] = "Rogue", ["Devil"] = "Rogue Mech",
        ["God"] = "Overseer", ["Gods"] = "Overseers", ["Goddess"] = "Overseer", ["Godly"] = "Overseer",
        ["Heaven"] = "Orbit", ["Heavenly"] = "Orbital", ["Heavens"] = "Orbit", ["Hell"] = "Inferno",
        ["Prayer"] = "Signal", ["Destiny"] = "Trajectory", ["Fate"] = "Probability", ["Fortune"] = "Odds",
        ["Miracle"] = "Breakthrough", ["Reborn"] = "Rebooted", ["Resurrection"] = "Revival", ["Awakened"] = "Activated",
        ["Omen"] = "Signal", ["Oracle"] = "Predictor", ["Prophecy"] = "Projection", ["Zodiac"] = "Constellation",
        ["Amulet"] = "Pendant", ["Talisman"] = "Token", ["Charm"] = "Neural Hijack",
        ["Sanctuary"] = "Safe Zone", ["Sanctity"] = "Integrity", ["Chakra"] = "Energy Node",
        ["Malefic"] = "Malware", ["Aspected"] = "Calibrated", ["Diurnal"] = "Daycycle", ["Nocturnal"] = "Nightcycle",
        ["Celestial"] = "Orbital", ["Arcana"] = "Data Card", ["Eukrasian"] = "Augmented",
        ["Muse"] = "Render", ["Motif"] = "Template", ["Sketch"] = "Blueprint", ["Lemure"] = "Shade Drone",
        ["Edict"] = "Directive", ["Decree"] = "Directive", ["Ordained"] = "Assigned", ["Judgment"] = "Verdict",
        ["Calamity's"] = "Catastrophe's",
        ["Saints"] = "Paragons", ["Angel's"] = "Guardian's", ["Unholy"] = "Corrupt", ["Cursekeeper"] = "Malware Keeper",
        ["Souldeep"] = "Core-Deep", ["Spellforge"] = "Code Forge", ["Spellblade"] = "Tech Blade",
        ["Magick"] = "Tech", ["Magicks"] = "Tech", ["Ensorcelled"] = "Hacked",
        // The Twelve (in-game gods) -> neutral tech words
        ["Twelve's"] = "Grid's", ["Byregot's"] = "Forge's", ["Nald'thal's"] = "Market's", ["Nophica's"] = "Harvest",
        ["Thaliak's"] = "Data", ["Nymeia's"] = "Cycle", ["Halone's"] = "Steel's", ["Menphina's"] = "Moon's",
        ["Azeyma's"] = "Sun's", ["Rhalgr's"] = "Quake's", ["Oschon's"] = "Ranger's", ["Llymlaen's"] = "Tide's",
        ["Althyk's"] = "Time's",
        ["Spiritbond"] = "Gear Sync", ["Spiritbonding"] = "Gear Sync", ["She"] = "He", ["Her"] = "His",
        // Elements
        ["Fire"] = "Thermal", ["Flame"] = "Plasma", ["Flames"] = "Plasma", ["Burning"] = "Overheating", ["Blaze"] = "Plasma Burst",
        ["Ice"] = "Cryo", ["Frost"] = "Cryo", ["Blizzard"] = "Cryo Storm", ["Frozen"] = "Cryo-Locked",
        ["Lightning"] = "Voltage", ["Thunder"] = "Arc", ["Thundercloud"] = "Arc Charge",
        ["Wind"] = "Pressure", ["Winds"] = "Pressure", ["Aero"] = "Pneumatic",
        ["Earth"] = "Seismic", ["Stone"] = "Ferro", ["Quake"] = "Seismic Pulse",
        ["Water"] = "Hydro", ["Waters"] = "Hydro", ["Tide"] = "Surge", ["Tides"] = "Surges",
        ["Light"] = "Photon", ["Dark"] = "Null", ["Darkness"] = "Null Field", ["Shadow"] = "Stealth", ["Shadows"] = "Stealth",
        ["Elemental"] = "Elemental", ["Chaos"] = "Entropy", ["Ruin"] = "Breach", ["Death"] = "Termination",
        // Creatures and body
        ["Dragon"] = "Mech", ["Dragon's"] = "Mech's", ["Wyrm"] = "Mech", ["Beast"] = "Bio-Mech",
        ["Blood"] = "Bio", ["Bloodied"] = "Damaged", ["Flesh"] = "Bio", ["Heart"] = "Reactor",
        ["Phoenix"] = "Firebird Drone", ["Ifrit"] = "Inferno Drone", ["Titan"] = "Bulwark Drone", ["Garuda"] = "Gale Drone",
        ["Bahamut"] = "Dread Drone", ["Primal"] = "Colossus",
        // Abstract
        ["Sacrifice"] = "Overload", ["Kinship"] = "Link", ["Bonds"] = "Links", ["Grace"] = "Buffer",
        ["Favor"] = "Priority", ["Authority's"] = "Command's", ["Royal"] = "Command", ["Banner"] = "Beacon",
        ["Ward"] = "Firewall", ["Aegis"] = "Bulwark", ["Seal"] = "Lock", ["Sealed"] = "Locked",
        ["Memory"] = "Cache", ["Memories"] = "Cache", ["Wisdom"] = "Analytics", ["Venom"] = "Toxin",
        // Songs and dance (no music theme)
        ["Ballad"] = "Broadcast", ["Paeon"] = "Signal", ["Minuet"] = "Pulse", ["Minne"] = "Pulse",
        ["Finale"] = "Final Broadcast", ["Song"] = "Signal", ["Songs"] = "Signals", ["Samba"] = "Pulse",
        ["Dance"] = "Routine", ["Dancer"] = "Operative",
        // No female characters
        ["Lady"] = "Lord", ["Queen"] = "Commander", ["Princess"] = "Heir", ["Maiden"] = "Scout",
        ["Mother"] = "Matrix", ["Sister"] = "Unit", ["Empress"] = "Sovereign", ["Nymph"] = "Drone",
        ["Siren"] = "Alarm", ["Mermaid"] = "Diver",
    };

    private static readonly Regex WordPattern = new(@"[A-Za-z][A-Za-z']*", RegexOptions.Compiled);
    private static Dictionary<string, string>? allWords;

    private static Dictionary<string, string> AllWords()
    {
        if (allWords != null) return allWords;
        allWords = new Dictionary<string, string>(Words);
        foreach (var (k, v) in Words)
        {
            var lower = char.ToLowerInvariant(k[0]) + k[1..];
            allWords.TryAdd(lower, char.ToLowerInvariant(v[0]) + v[1..]);
            allWords.TryAdd(k.ToUpperInvariant(), v.ToUpperInvariant());
        }
        return allWords;
    }

    /// <summary>Sci-fi version of a status name, or the name itself when nothing applies.</summary>
    public static string Apply(string name)
    {
        if (FullNames.TryGetValue(name, out var full))
            return full;
        return Apply(name, null);
    }

    /// <summary>Word swaps only (no full-name table), with optional extra words checked first.</summary>
    public static string Apply(string name, IReadOnlyDictionary<string, string>? extra)
    {
        var words = AllWords();
        return WordPattern.Replace(name, m =>
        {
            if (extra != null)
            {
                if (extra.TryGetValue(m.Value, out var e)) return e;
                var lower = m.Value.Length > 1 && char.IsLower(m.Value[0]);
                if (lower && extra.TryGetValue(char.ToUpperInvariant(m.Value[0]) + m.Value[1..], out var el))
                    return char.ToLowerInvariant(el[0]) + el[1..];
            }
            return words.TryGetValue(m.Value, out var r) ? r : m.Value;
        });
    }
}
