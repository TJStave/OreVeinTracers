using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OreVeinTracers
{
    [HarmonyPatchCategory(OreVeinTracersModSystem.OreVeinTracersPatchCategory)]
    class OreVeinTracersPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Block), nameof(Block.OnBlockBroken)), HarmonyPriority(Priority.HigherThanNormal)]
        public static bool OnBlockBrokenPrefix(IWorldAccessor world, BlockPos pos)
        {
            return GenerateTracer(world, pos);
        }

        // have to do it twice because the devs implemented a hacky fix in the BlockOre class, so it doesn't call the base function
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BlockOre), nameof(BlockOre.OnBlockBroken)), HarmonyPriority(Priority.HigherThanNormal)]
        public static bool OreOnBlockBrokenPrefix(IWorldAccessor world, BlockPos pos)
        {
            return GenerateTracer(world, pos);
        }

        private static bool GenerateTracer(IWorldAccessor world, BlockPos pos)
        {
            Block brokenBlock = world.BlockAccessor.GetBlock(pos.Copy());
            bool oreAround = false;

            // first check if any ore is nearby
            world.BlockAccessor.WalkBlocks(pos.AddCopy(-2, -2, -2), pos.AddCopy(2, 2, 2), (nblock, x, y, z) =>
            {
                if (nblock?.BlockMaterial == EnumBlockMaterial.Ore && (nblock?.Variant?.ContainsKey("type") ?? false))
                {
                    oreAround = true;
                }
            });

            // if no ore found, or broken block is not ore when config option onlyActivateWithinVein is enabled
            if (!oreAround ||
               (OreVeinTracersModSystem.config.onlyActivateWithinVein && (brokenBlock?.BlockMaterial != EnumBlockMaterial.Ore || (!brokenBlock?.Variant?.ContainsKey("type") ?? true))))
            {
                return true; // exit early
            }

            // make a copy of pos so the value isn't messed up
            BlockPos decorPos = pos.Copy();
            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                // iterate though surrounding blocks
                facing.IterateThruFacingOffsets(decorPos);

                // if block isn't stone, skip it
                Block decorBlock = world.BlockAccessor.GetBlock(decorPos);
                if (decorBlock?.FirstCodePart() != "rock" && decorBlock?.FirstCodePart() != "crackedrock")
                {
                    continue;
                }

                // make another copy, this ones for the blocks surrounding the previous layer
                BlockPos searchPos = decorPos.Copy();
                bool oreFound = false;
                foreach (BlockFacing searchFacing in BlockFacing.ALLFACES)
                {
                    // iterate through again
                    searchFacing.IterateThruFacingOffsets(searchPos);
                    // skip checking the original block that was broken
                    if (searchFacing == facing.Opposite)
                    {
                        continue;
                    }
                    // check if ore
                    Block checkBlock = world.BlockAccessor.GetBlock(searchPos);
                    if (checkBlock?.BlockMaterial == EnumBlockMaterial.Ore && (checkBlock?.Variant?.ContainsKey("type") ?? false))
                    {
                        // add an overlay to the block based on the ore found
                        // (edge case when more than 1 type of ore adjacent, will add the first type found, potentially misleading the player)
                        Block tracerBlock = world.BlockAccessor.GetBlock("oreveintracers:oretracer-" + DecodeVariant(checkBlock)) ?? world.BlockAccessor.GetBlock("oreveintracers:placeholder");
                        world.BlockAccessor.SetDecor(tracerBlock, decorPos, facing.Opposite);

                        oreFound = true;
                        break;
                    }
                }
                // no ore found surrounding this block
                if (!oreFound)
                {
                    //get all decors on the block
                    Dictionary<int, Block> decorsDict = world.BlockAccessor.GetSubDecors(decorPos);
                    if(decorsDict is null)
                    {
                        // didn't find anything? then don't need to do anything
                        continue;
                    }
                    foreach (int key in decorsDict.Keys)
                    {
                        // remove any tracer overlays, so the player knows there isn't any more ore around
                        if (decorsDict[key].FirstCodePart() == "oretracer")
                        {
                            world.BlockAccessor.SetDecor(world.BlockAccessor.GetBlock(0), decorPos, key);
                        }
                    }
                }
            }

            return true;
        }

        // function to deal with sub-ores (gold and silver in quartz/galena and peridot in olivine)
        public static string DecodeVariant(Block block)
        {
            string oreType = block?.Variant["type"];
            if (oreType?.Contains('_') == true)
            {
                return oreType.Split('_')[0];
            }
            else
            {
                return oreType;
            }
        }
    }
}
