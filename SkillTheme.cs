using System.Collections.Generic;

namespace ActionRenamer;

/// <summary>
/// Sci-fi theme for job names, skill names and trait names. Full-name entries win; otherwise the
/// shared word swaps in <see cref="StatusTheme"/> apply (Soul -> Core, Fire -> Thermal, ...).
/// </summary>
public static class SkillTheme
{
    /// <summary>Job and class names (title case; lower and upper case forms are added automatically).</summary>
    public static readonly Dictionary<string, string> Jobs = new()
    {
        ["Thaumaturge"] = "Pyrotechnician",
        ["Conjurer"] = "Medic",
        ["Arcanist"] = "Drone Engineer",
        ["Summoner"] = "Drone Commander",
        ["Astrologian"] = "Astronomer",
        ["White Mage"] = "Field Medic",
        ["Black Mage"] = "Black Engineer",
        ["Red Mage"] = "Red Duelist",
        ["Blue Mage"] = "Blue Mimic",
        ["Reaper"] = "Harvester",
        ["Pictomancer"] = "Holo-Artist",
        ["Dancer"] = "Operative",
        ["Bard"] = "Signal Ranger",
        ["Monk"] = "Striker",
    };

    /// <summary>Whole-name renames for skills and traits.</summary>
    public static readonly Dictionary<string, string> FullNames = new()
    {
        // Arcanist / Summoner
        ["Physick"] = "Med Pulse",
        ["Summon Carbuncle"] = "Deploy Gem Drone",
        ["Brand of Purgatory"] = "Brand of Plasma",
        ["Deathflare"] = "Termination Flare",
        ["Dreadwyrm Trance"] = "Dread Mech Overdrive",
        ["Enhanced Dreadwyrm Trance"] = "Enhanced Dread Mech Overdrive",
        ["Fountain of Fire"] = "Fountain of Plasma",
        ["Necrotize"] = "Bio Collapse",
        // Astrologian
        ["Ascend"] = "Revival Beam",
        ["Benefic"] = "Med Shot",
        ["Benefic II"] = "Med Shot II",
        ["Aspected Benefic"] = "Calibrated Med Shot",
        ["Enhanced Benefic"] = "Enhanced Med Shot",
        ["Helios"] = "Sunburst",
        ["Aspected Helios"] = "Calibrated Sunburst",
        ["Aspected Helios Mastery"] = "Calibrated Sunburst Mastery",
        ["Helios Conjunction"] = "Sunburst Link",
        ["Collective Unconscious"] = "Collective Shield",
        ["Divination"] = "Prediction Engine",
        ["Enhanced Divination"] = "Enhanced Prediction Engine",
        ["Earthly Star"] = "Ground Beacon",
        ["Essential Dignity"] = "Essential Repair",
        ["Enhanced Essential Dignity"] = "Enhanced Essential Repair",
        ["Enhanced Essential Dignity II"] = "Enhanced Essential Repair II",
        ["Exaltation"] = "Exo Shield",
        ["Horoscope"] = "Forecast",
        ["Lady of Crowns"] = "Crown Card II",
        ["Lord of Crowns"] = "Crown Card",
        ["Macrocosmos"] = "Macro Field",
        ["Microcosmos"] = "Micro Field",
        ["Neutral Sect"] = "Neutral Mode",
        ["Enhanced Neutral Sect"] = "Enhanced Neutral Mode",
        ["Sun Sign"] = "Solar Signal",
        ["Synastry"] = "Sync-Link",
        // Black Mage / Thaumaturge
        ["Ley Lines"] = "Power Grid",
        ["Enhanced Ley Lines"] = "Enhanced Power Grid",
        ["Wreath of Fire"] = "Thermal Ring",
        ["Wreath of Ice"] = "Cryo Ring",
        ["Xenoglossy"] = "Xeno Burst",
        ["Firestarter"] = "Ignition",
        ["Triplecast"] = "Triple Process",
        // Blue Mage
        ["Angel Whisper"] = "Revival Whisper",
        ["Apokalypsis"] = "Total Meltdown",
        ["Conviction Marcato"] = "Conviction Strike",
        ["Divination Rune"] = "Prediction Code",
        ["Doom"] = "Countdown",
        ["Glass Dance"] = "Glass Shards",
        ["Matra Magic"] = "Matrix Barrage",
        ["Moon Flute"] = "Moon Beacon",
        ["Sea Shanty"] = "Sea Surge",
        ["Stotram"] = "Sonic Mend",
        ["Surpanakha"] = "Spiral Fan",
        ["Winged Reprobation"] = "Winged Sentence",
        // Bard
        ["Empyreal Arrow"] = "Orbital Arrow",
        ["Enhanced Empyreal Arrow"] = "Enhanced Orbital Arrow",
        ["Final Fantasia"] = "Final Barrage",
        ["Pitch Perfect"] = "Perfect Shot",
        ["Radiant Finale"] = "Radiant Broadcast",
        ["Enhanced Radiant Finale"] = "Enhanced Radiant Broadcast",
        ["Silent Nocturne"] = "Silent Volley",
        ["Troubadour"] = "Sonic Shield",
        ["Minstrel's Coda"] = "Ranger's Edge",
        // Conjurer / White Mage
        ["Cure"] = "Med Patch",
        ["Cure II"] = "Med Patch II",
        ["Cure III"] = "Med Patch III",
        ["Freecure"] = "Free Patch",
        ["Raise"] = "Revival Signal",
        ["Regen"] = "Nano-Regen",
        ["Afflatus Misery"] = "Overflow Misery",
        ["Afflatus Purgation"] = "Overflow Purge",
        ["Afflatus Rapture"] = "Overflow Surge",
        ["Afflatus Solace"] = "Overflow Solace",
        ["Transcendent Afflatus"] = "Reboot Overflow",
        ["Asylum"] = "Med Zone",
        ["Enhanced Asylum"] = "Enhanced Med Zone",
        ["Aquaveil"] = "Hydro Veil",
        ["Benediction"] = "Full Restore",
        ["Divine Benison"] = "Prime Barrier",
        ["Enhanced Divine Benison"] = "Enhanced Prime Barrier",
        ["Divine Caress"] = "Prime Shield",
        ["Holy"] = "Photon Burst",
        ["Holy III"] = "Photon Burst III",
        ["Liturgy of the Bell"] = "Pulse Beacon",
        ["Miracle of Nature"] = "Nature Override",
        ["Plenary Indulgence"] = "Wide Med Boost",
        ["Seraph Strike"] = "Sentinel Strike",
        ["Tetragrammaton"] = "Quad Patch",
        ["Enhanced Tetragrammaton"] = "Enhanced Quad Patch",
        // Dancer
        ["Contradance"] = "Counter Routine",
        ["Curing Waltz"] = "Med Wave",
        ["Devilment"] = "Overdrive Step",
        ["Enhanced Devilment"] = "Enhanced Overdrive Step",
        ["Shield Samba"] = "Shield Pulse",
        ["Dynamic Dancer"] = "Dynamic Operative",
        // Dragoon
        ["Battle Litany"] = "Battle Protocol",
        ["Doom Spike"] = "Breach Spike",
        ["Geirskogul"] = "Lance Overload",
        ["Nastrond"] = "Nova Thrust",
        ["Heavens' Thrust"] = "Orbital Thrust",
        ["Raiden Thrust"] = "Arc Thrust",
        // Dark Knight
        ["Dark Missionary"] = "Null Barrier",
        ["Delirium"] = "Overdrive",
        ["Scarlet Delirium"] = "Scarlet Overdrive",
        ["Enhanced Delirium"] = "Enhanced Overdrive",
        ["Living Dead"] = "Last Stand",
        ["Living Shadow"] = "Holo Clone",
        ["Enhanced Living Shadow"] = "Enhanced Holo Clone",
        ["Enhanced Living Shadow II"] = "Enhanced Holo Clone II",
        ["Enhanced Living Shadow III"] = "Enhanced Holo Clone III",
        ["Oblation"] = "Barrier Pulse",
        ["Quietus"] = "Shutdown",
        ["Shadowed Vigil"] = "Shadowed Watch",
        ["the Blackest Night"] = "the Black Barrier",
        ["The Blackest Night"] = "The Black Barrier",
        // Gladiator / Paladin
        ["Rage of Halone"] = "Rage of Steel",
        ["Rage of Halone Mastery"] = "Rage of Steel Mastery",
        ["Atonement"] = "Payback",
        ["Blade of Faith"] = "Blade of Focus",
        ["Confiteor"] = "Overcharge Strike",
        ["Expiacion"] = "Recoil Slash",
        ["Hallowed Ground"] = "Invulnerability Field",
        ["Holy Spirit"] = "Photon Lance",
        ["Requiescat"] = "Overcharge",
        ["Enhanced Requiescat"] = "Enhanced Overcharge",
        ["Requiescat Mastery"] = "Overcharge Mastery",
        ["Sepulchre"] = "Sealing Strike",
        ["Spirits Within"] = "Power Within",
        ["Spirits Within Mastery"] = "Power Within Mastery",
        ["Supplication"] = "Follow-up Strike",
        // Gunbreaker
        ["Demon Slaughter"] = "Mech Slaughter",
        ["Demon Slice"] = "Mech Slice",
        // Machinist
        ["Bishop Autoturret"] = "Sentinel Autoturret",
        ["Queen's Gambit"] = "Commander's Gambit",
        // Monk / Pugilist
        ["Enlightened Meditation"] = "Charged Focus",
        ["Enlightenment"] = "Overcharge Wave",
        ["Forbidden Meditation"] = "Forbidden Focus",
        ["Inspirited Meditation"] = "Inspired Focus",
        ["Steeled Meditation"] = "Steeled Focus",
        ["Deep Meditation"] = "Deep Focus",
        ["Deep Meditation II"] = "Deep Focus II",
        ["Rising Phoenix"] = "Rising Firebird",
        ["Six-sided Star"] = "Six-point Burst",
        ["the Forbidden Chakra"] = "the Forbidden Core",
        ["Beast Chakra Mastery"] = "Beast Core Mastery",
        ["Mantra"] = "Rally Cry",
        // Marauder / Warrior
        ["Holmgang"] = "Holdfast",
        ["Damnation"] = "Overload Guard",
        // Ninja
        ["Bhavacakra"] = "Cyclone Cutter",
        ["Ten"] = "Input A",
        ["Chi"] = "Input B",
        ["Jin"] = "Input C",
        ["Ten Chi Jin"] = "Triple Sequence",
        ["Enhanced Ten Chi Jin"] = "Enhanced Triple Sequence",
        ["Three Mudra"] = "Triple Input",
        ["Hellfrog Medium"] = "Inferno Frog",
        ["Hollow Nozuchi"] = "Hollow Burrow",
        ["Kassatsu"] = "Override",
        ["Enhanced Kassatsu"] = "Enhanced Override",
        ["Seiton Tenchu"] = "Seiton Strike",
        ["Zesho Meppo"] = "Zero Point",
        // Pictomancer
        ["Advent of Chocobastion"] = "Arrival of Chocobastion",
        ["Retribution of the Madeen"] = "Retribution of the Guardian",
        // Red Mage
        ["Dualcast"] = "Dual Process",
        ["Manafication"] = "Flux Overload",
        ["Enhanced Manafication"] = "Enhanced Flux Overload",
        ["Southern Cross"] = "Southern Arc",
        ["Veraero"] = "Pneumatic Burst",
        ["Veraero II"] = "Pneumatic Burst II",
        ["Veraero III"] = "Pneumatic Burst III",
        ["Verfire"] = "Thermal Burst",
        ["Verstone"] = "Ferro Burst",
        ["Verthunder"] = "Arc Burst",
        ["Verthunder II"] = "Arc Burst II",
        ["Verthunder III"] = "Arc Burst III",
        ["Vercure"] = "Med Burst",
        ["Verraise"] = "Revival Burst",
        // Rogue
        ["Death Blossom"] = "Blade Bloom",
        // Role and shared
        ["Standard-issue Elixir"] = "Standard-issue Stim",
        // Reaper
        ["Communio"] = "Final Uplink",
        ["Cross Reaping"] = "X Harvest",
        ["Enshroud"] = "Cloak Mode",
        ["Enhanced Enshroud"] = "Enhanced Cloak Mode",
        ["Gluttony"] = "Overfeed",
        ["Enhanced Gluttony"] = "Enhanced Overfeed",
        ["Grim Swathe"] = "Wide Swathe",
        ["Hell's Egress"] = "Phase Egress",
        ["Hell's Ingress"] = "Phase Ingress",
        ["Hellsgate"] = "Phase Gate",
        ["Infernal Slice"] = "Inferno Slice",
        ["Lemure's Slice"] = "Clone Slice",
        ["Tenebrae Lemurum"] = "Clone Barrage",
        ["Enhanced Avatar"] = "Enhanced Clone",
        ["Soulsow"] = "Core Charge",
        // Samurai
        ["Meditate"] = "Focus",
        ["Meikyo Shisui"] = "Mirror Stance",
        ["Enhanced Meikyo Shisui"] = "Enhanced Mirror Stance",
        ["Enhanced Meikyo Shisui II"] = "Enhanced Mirror Stance II",
        ["Tendo Setsugekka"] = "Ultra Setsugekka",
        ["Tendo Kaeshi Setsugekka"] = "Ultra Kaeshi Setsugekka",
        ["Third Eye"] = "Threat Sensor",
        ["Third Eye Mastery"] = "Threat Sensor Mastery",
        // Scholar
        ["Lustrate"] = "Med Burst",
        ["Recitation"] = "Readout",
        ["Enhanced Recitation"] = "Enhanced Readout",
        ["Seraphic Halo"] = "Sentinel Beam",
        ["Seraphism"] = "Drone Overdrive",
        ["Summon Eos"] = "Deploy Drone",
        ["Summon Seraph"] = "Deploy Sentinel Drone",
        // Sage
        ["Egeiro"] = "Revival Pulse",
        ["Eukrasia"] = "Augment",
        ["Kardia"] = "Bio-Link",
        ["Kerachole"] = "Kera-Shield",
        ["Enhanced Kerachole"] = "Enhanced Kera-Shield",
        ["Krasis"] = "Mixer Boost",
        ["Pneuma"] = "Pulse Beam",
        ["Psyche"] = "Neural Strike",
        ["Physis"] = "Nanite Mist",
        ["Physis II"] = "Nanite Mist II",
        ["Enhanced Physis II"] = "Enhanced Nanite Mist II",
        ["Physis Mastery"] = "Nanite Mist Mastery",
        ["Soteria"] = "Recovery Boost",
        ["Enhanced Soteria"] = "Enhanced Recovery Boost",
        ["Zoe"] = "Amp",
        ["Enhanced Zoe"] = "Enhanced Amp",
        ["Holos"] = "Holo-Field",
        ["Haima"] = "Hemo-Shield",
        ["Panhaima"] = "Wide Hemo-Shield",
        // Viper
        ["Ouroboros"] = "Loop Drive",
        ["Reawaken"] = "Reboot",
    };

    /// <summary>Extra word swaps used by skills (added to the shared status words).</summary>
    public static readonly Dictionary<string, string> Words = new()
    {
        ["Aethercharge"] = "Energy Charge", ["Aetheric"] = "Energy", ["Aetherflow"] = "Energy Flow", ["Aetherpact"] = "Energy Pact",
        ["Malediction"] = "Malware", ["Saintly"] = "Paragon", ["Paean"] = "Signal", ["Halone"] = "Steel",
        ["Fated"] = "Locked", ["Meditation"] = "Focus", ["Enochian"] = "Overcharge", ["Darkside"] = "Null Mode",
        ["Pictomancy"] = "Holo-Art", ["Pictomancer"] = "Holo-Artist", ["Summoning"] = "Deployment", ["Hell's"] = "Inferno's",
        ["Medica"] = "Med-Field", ["Manafont"] = "Flux Reactor", ["Manaward"] = "Flux Shield", ["Reaper"] = "Harvester",
        ["Mantra"] = "Rally Cry", ["Afflatus"] = "Overflow", ["Avatar"] = "Clone", ["Mudra"] = "Input",
        ["Trance"] = "Overdrive", ["Halo"] = "Beam", ["Cross"] = "X",
        ["Troubadour"] = "Sonic Shield", ["Souleater"] = "Core Eater",
    };

    /// <summary>Sci-fi version of a job, skill or trait name, or the name itself when nothing applies.</summary>
    public static string Apply(string name)
    {
        if (FullNames.TryGetValue(name, out var full))
            return full;
        if (Jobs.TryGetValue(name, out var job))
            return job;
        return StatusTheme.Apply(name, Words);
    }

    /// <summary>Job names in every case the game uses (Viper, viper, VIPER).</summary>
    public static IEnumerable<(string From, string To)> JobVariants()
    {
        foreach (var (from, to) in Jobs)
        {
            yield return (from, to);
            yield return (from.ToLowerInvariant(), to.ToLowerInvariant());
            yield return (from.ToUpperInvariant(), to.ToUpperInvariant());
        }
    }
}
