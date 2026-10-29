using System.Collections.Generic;

namespace ActionRenamer;

/// <summary>Starter rules. Every rule is editable in the settings window.</summary>
public static class Presets
{
    public static List<RenameRule> Default()
    {
        var r = new List<RenameRule>();

        void Word(string group, string find, string replace) =>
            r.Add(new RenameRule { Group = group, Find = find, Replace = replace, Mode = MatchMode.WholeWord });
        void Name(string group, string find, string replace) =>
            r.Add(new RenameRule { Group = group, Find = find, Replace = replace, Mode = MatchMode.Exact });

        // General wording in tooltips and descriptions. Longer phrases first so they win.
        Word("Wording", "Magic Damage", "Tech Damage");
        Word("Wording", "magic damage", "tech damage");
        Word("Wording", "Healing Magic", "Medical Tech");
        Word("Wording", "healing magic", "medical tech");
        Word("Wording", "Spells", "Techniques");
        Word("Wording", "spells", "techniques");
        Word("Wording", "Spell", "Technique");
        Word("Wording", "spell", "technique");
        Word("Wording", "Magical", "Technological");
        Word("Wording", "magical", "technological");
        Word("Wording", "Magic", "Tech");
        Word("Wording", "magic", "tech");
        Word("Wording", "Aetherial", "Energy");
        Word("Wording", "aetherial", "energy");

        // Job and class names.
        Word("Jobs", "Black Mage", "Black Engineer");
        Word("Jobs", "White Mage", "Field Medic");
        Word("Jobs", "Red Mage", "Red Duelist");
        Word("Jobs", "Blue Mage", "Blue Mimic");
        Word("Jobs", "Summoner", "Drone Commander");
        Word("Jobs", "Astrologian", "Astronomer");
        Word("Jobs", "Thaumaturge", "Pyrotechnician");
        Word("Jobs", "Conjurer", "Medic");
        Word("Jobs", "Arcanist", "Drone Engineer");

        // Sample action names (exact match, so descriptions are untouched). Add your own per job.
        Name("Black Mage", "Fire", "Incendiary Round");
        Name("Black Mage", "Blizzard", "Cryo Round");
        Name("Black Mage", "Thunder", "Arc Round");
        Name("White Mage", "Holy", "Radiant Burst");
        Name("White Mage", "Cure", "Med Patch");
        Name("White Mage", "Raise", "Revival Shock");
        Name("Astrologian", "Draw", "Load Data Card");

        return r;
    }
}
