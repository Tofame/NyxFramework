namespace Sandbox.UI;

internal readonly record struct BestiaryEntry(
    string Id,
    string Name,
    string Classification,
    int HitPoints,
    int Experience,
    int Speed,
    int Armor,
    string Description);

/// <summary>Demo bestiary data for the Sandbox NyxGUI panel.</summary>
internal static class BestiaryCatalog
{
    public static IReadOnlyList<BestiaryEntry> Entries { get; } =
    [
        new("rat", "Rat", "Mammal", 30, 5, 134, 1,
            "A common sewer rat. Weak alone, but swarms can overwhelm unprepared adventurers."),
        new("wolf", "Wolf", "Mammal", 85, 18, 156, 4,
            "Hunts in packs at night. Fast on open ground and dangerous when cornered."),
        new("tiger", "Tiger", "Mammal", 210, 65, 168, 12,
            "Strikes from cover with brutal force. High damage and excellent reflexes."),
        new("lion", "Lion", "Mammal", 240, 72, 170, 14,
            "King of the savanna. Roars to rally pride members before charging prey."),
        new("bear", "Bear", "Mammal", 280, 80, 142, 18,
            "Thick hide and crushing claws. Slow but devastating at melee range."),
        new("spider", "Spider", "Arachnid", 120, 34, 152, 6,
            "Weaves webs to slow victims. Poisonous bite causes steady health drain."),
        new("scorpion", "Scorpion", "Arachnid", 145, 42, 148, 10,
            "Armored tail delivers venom. Prefers ambush in desert ruins."),
        new("snake", "Snake", "Reptile", 95, 28, 160, 3,
            "Slithers silently through grass. Quick strike, low endurance."),
        new("orc", "Orc", "Humanoid", 320, 95, 150, 16,
            "Brutish warrior fond of axes. Often found guarding war camps."),
        new("troll", "Troll", "Humanoid", 450, 130, 128, 22,
            "Regenerates wounds over time. Weak to fire and sustained pressure."),
        new("vampire", "Vampire", "Undead", 380, 145, 162, 14,
            "Drains life with each hit. Vulnerable to holy magic and sunlight."),
        new("zombie", "Zombie", "Undead", 260, 70, 118, 8,
            "Mindless and relentless. Resistant to fear, weak to fire."),
        new("skeleton", "Skeleton", "Undead", 200, 55, 140, 6,
            "Rattling bones animated by dark magic. Arrows pass through gaps."),
        new("ghost", "Ghost", "Undead", 310, 110, 175, 2,
            "Phases through walls. Physical weapons deal reduced damage."),
        new("dragon", "Dragon", "Reptile", 1200, 650, 145, 35,
            "Ancient wyrm breathing fire. Terrifying boss-tier threat."),
        new("hydra", "Hydra", "Reptile", 980, 520, 138, 28,
            "Multiple heads regenerate unless cauterized quickly."),
        new("phoenix", "Phoenix", "Magical", 860, 480, 190, 20,
            "Reborn from ashes when slain. Fire aura damages nearby foes."),
        new("golem", "Golem", "Construct", 720, 390, 110, 40,
            "Stone body shrugs off blades. Slow but nearly impervious."),
        new("slime", "Slime", "Ooze", 160, 38, 122, 5,
            "Splits when struck too hard. Acidic touch melts light armor."),
        new("minotaur", "Minotaur", "Humanoid", 540, 210, 148, 24,
            "Labyrinth guardian wielding a massive axe. Charges in straight lines."),
        new("demon", "Demon", "Fiend", 890, 460, 158, 26,
            "Summoned from infernal planes. Casts firebolts between melee swings."),
    ];

    public static BestiaryEntry Default => Entries[0];
}
