using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;

namespace OreVeinTracers;

public class OreVeinTracersModSystem : ModSystem
{
    internal static Harmony harmony;

    public const string OreVeinTracersPatchCategory = "oreVeinTracers";
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        if (harmony == null)
        {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchCategory(OreVeinTracersPatchCategory);
            Mod.Logger.VerboseDebug("Ore Vein Tracers Patched");
        }
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll(Mod.Info.ModID);
        harmony = null;
        base.Dispose();
    }
}
