using NyxDrawer.Creatures;
using Sandbox;
using Silk.NET.Input;

namespace Sandbox.Spells;

internal static class SpellCastActions
{
    public static bool TryCastSlot(
        int slotIndex,
        SpellCatalog catalog,
        Player player,
        IReadOnlyList<Npc> npcs,
        NyxGameMap.GameMap map,
        IInputContext? input,
        float camXf,
        float camYf,
        float? aimGameX,
        float? aimGameY,
        ActiveSpellEffects spellEffects,
        ActiveMissileEffects missileEffects,
        SandboxGameWorld? gameWorld = null)
    {
        if (slotIndex < 0 || slotIndex >= catalog.Spells.Count)
            return false;

        var spell = catalog.Spells[slotIndex];
        if (gameWorld is not null && gameWorld.IsNetworkActive)
        {
            gameWorld.SendSpellCastRequest(slotIndex, aimGameX, aimGameY, camXf, camYf, input);
            return true;
        }

        if (spell.MouseTarget)
        {
            if (input is null ||
                !SpellCastInput.TryGetMouseTile(input, camXf, camYf, map, aimGameX, aimGameY, out var mouseTx, out var mouseTy))
                return false;

            if (!SpellCaster.TryCastMissileToMouse(catalog, spell, player, map, mouseTx, mouseTy, out var flight))
                return false;

            missileEffects.Add(flight);
            return true;
        }

        if (!SpellCaster.TryCast(catalog, spell, player, npcs, map, out var hits))
            return false;

        spellEffects.AddHits(hits);
        return true;
    }

    public static string BuildTooltipText(SpellDefinition spell)
    {
        var lines = new List<string> { spell.Name, $"\"{spell.Words}\"" };

        if (spell.MouseTarget)
            lines.Add("Targets the mouse position.");
        else if (spell.SelfTarget)
            lines.Add("Area centered on you.");
        else if (spell.NeedTarget)
            lines.Add("Requires a nearby target.");
        else if (spell.Direction)
            lines.Add("Melee slash in facing direction.");
        else
            lines.Add("Instant cast.");

        return string.Join('\n', lines);
    }
}
