namespace Sandbox.UI;

internal readonly record struct QuestLogEntry(string Id, string Title, string Description);

/// <summary>Demo quest data for the Sandbox quest log panel.</summary>
internal static class QuestLogCatalog
{
    public static IReadOnlyList<QuestLogEntry> Entries { get; } =
    [
        new(
            "mission_1",
            "Mission #1 — Rats in the Sewers",
            "Clear the overflow tunnels beneath the market square.\n\n"
            + "{#FFFF00}Objective:{/} Defeat {#FF5555}10 sewer rats{/}.\n"
            + "{#FFFF00}Reward:{/} {#55FF55}120 experience{/}, {#FFFF00}25 gold{/}.\n\n"
            + "Hint: Rats swarm when you linger. Pull them in small groups."),
        new(
            "mission_2",
            "Mission #2 — Wolf Patrol",
            "The north road patrol reports wolves near the lumber camp.\n\n"
            + "{#FFFF00}Objective:{/} Slay {#FF5555}6 wolves{/} and report to the sergeant.\n"
            + "{#FFFF00}Reward:{/} {#55FF55}280 experience{/}, {#888888}leather scraps{/}.\n\n"
            + "The alpha appears only at night."),
        new(
            "mission_3",
            "Mission #3 — Lost Shipment",
            "A merchant caravan vanished east of the bridge.\n\n"
            + "{#FFFF00}Objective:{/} Find the {#FF5555}caravan ledger{/} and return it to the guild hall.\n"
            + "{#FFFF00}Reward:{/} {#55FF55}350 experience{/}, {#FFFF00}80 gold{/}, {#55AAFF}reputation +1{/}.\n\n"
            + "Bandit tracks lead toward the old quarry."),
        new(
            "mission_4",
            "Mission #4 — Spider Nest",
            "Webbing blocks the mine entrance. Miners refuse to return.\n\n"
            + "{#FFFF00}Objective:{/} Destroy {#FF5555}8 spider eggs{/} in the lower gallery.\n"
            + "{#FFFF00}Reward:{/} {#55FF55}420 experience{/}, {#55AAFF}antidote vials{/}.\n\n"
            + "Bring fire. The brood-mother is deeper in."),
        new(
            "mission_5",
            "Mission #5 — Crypt Investigation",
            "Strange lights were seen in the ruined chapel crypt.\n\n"
            + "Objective: Place ward stones at three altars.\n"
            + "Reward: 600 experience, holy symbol.\n\n"
            + "Undead rise if you stay too long after dark."),
        new(
            "mission_6",
            "Mission #6 — Harbor Sabotage",
            "Someone is sinking supply barges at the docks.\n\n"
            + "Objective: Recover three crates of salted fish from the bay.\n"
            + "Reward: 220 experience, fishing permit.\n\n"
            + "Watch for kraken spawn at high tide."),
        new(
            "mission_7",
            "Mission #7 — Herb Gathering",
            "The apothecary needs moonpetal before the next full moon.\n\n"
            + "Objective: Collect 12 moonpetal sprigs from the cliffs.\n"
            + "Reward: 180 experience, healing salve recipe.\n\n"
            + "Ghosts patrol the cliff path after dusk."),
        new(
            "mission_8",
            "Mission #8 — Bandit Camp",
            "Scouts marked a camp north of the quarry road.\n\n"
            + "Objective: Burn the bandit supply tent.\n"
            + "Reward: 310 experience, 45 gold.\n\n"
            + "Their captain flees if half the camp is alerted."),
        new(
            "mission_9",
            "Mission #9 — Library Theft",
            "A folio on planar gates was stolen from the academy.\n\n"
            + "Objective: Question three suspects and return the folio.\n"
            + "Reward: 260 experience, mage guild favor.\n\n"
            + "One suspect is lying about their alibi."),
        new(
            "mission_10",
            "Mission #10 — Golem Awakening",
            "An old war golem stirred in the foundry ruins.\n\n"
            + "Objective: Shut down the crystal core without destroying it.\n"
            + "Reward: 520 experience, golem shard.\n\n"
            + "Lightning damage shorts the control panel."),
        new(
            "mission_11",
            "Mission #11 — Golem Awakening",
            "An old war golem stirred in the foundry ruins.\n\n"
            + "Objective: Shut down the crystal core without destroying it.\n"
            + "Reward: 520 experience, golem shard.\n\n"
            + "Lightning damage shorts the control panel."),
    ];

    public static QuestLogEntry Default => Entries[0];
}
