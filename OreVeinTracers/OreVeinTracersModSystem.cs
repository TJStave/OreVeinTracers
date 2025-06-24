using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;
using System;

namespace OreVeinTracers;

public class OreVeinTracersModSystem : ModSystem
{
    internal static Harmony harmony;
    public static OreVeinTracersConfig config;

    public const string OreVeinTracersPatchCategory = "oreVeinTracers";
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        TryToLoadConfig(api);

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

    private void TryToLoadConfig(ICoreAPI api)
    {
        //It is important to surround the LoadModConfig function in a try-catch. 
        //If loading the file goes wrong, then the 'catch' block is run.
        try
        {
            config = api.LoadModConfig<OreVeinTracersConfig>("oreveintracersconfig.json");
            if (config == null) //if the 'oreveintracersconfig.json' file isn't found...
            {
                config = new OreVeinTracersConfig();
            }
            //Save a copy of the mod config.
            api.StoreModConfig<OreVeinTracersConfig>(config, "oreveintracersconfig.json");
        }
        catch (Exception e)
        {
            //Couldn't load the mod config... Create a new one with default settings, but don't save it.
            Mod.Logger.Error("Could not load config! Loading default settings instead.");
            Mod.Logger.Error(e);
            config = new OreVeinTracersConfig();
        }
    }
}
