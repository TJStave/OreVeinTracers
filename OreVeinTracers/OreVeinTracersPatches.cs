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
            Block brokenBlock = world.BlockAccessor.GetBlock(pos);
            if (brokenBlock?.BlockMaterial != EnumBlockMaterial.Ore || (!brokenBlock?.Variant?.ContainsKey("type") ?? true))
            {
                return true;
            }
            BlockPos decorPos = pos.Copy();
            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                facing.IterateThruFacingOffsets(decorPos);

                Block decorBlock = world.BlockAccessor.GetBlock(decorPos);

                if (decorBlock?.FirstCodePart() != "rock" && decorBlock?.FirstCodePart() != "crackedrock")
                {
                    continue;
                }

                BlockPos searchPos = decorPos.Copy();
                bool oreFound = false;
                foreach (BlockFacing searchFacing in BlockFacing.ALLFACES)
                {
                    searchFacing.IterateThruFacingOffsets(searchPos);
                    if (searchFacing == facing.Opposite)
                    {
                        continue;
                    }
                    Block checkBlock = world.BlockAccessor.GetBlock(searchPos);
                    if (CheckVariant(checkBlock) == CheckVariant(brokenBlock))
                    {
                        Block tracerBlock = world.BlockAccessor.GetBlock("oreveintracers:oretracer-" + CheckVariant(brokenBlock)) ?? world.BlockAccessor.GetBlock("oreveintracers:placeholder");

                        world.BlockAccessor.SetDecor(tracerBlock, decorPos, facing.Opposite);

                        oreFound = true;
                        break;
                    }
                }
                if (!oreFound)
                {
                    Dictionary<int, Block> decorsDict = world.BlockAccessor.GetSubDecors(decorPos);
                    if(decorsDict is null)
                    {
                        continue;
                    }
                    foreach (int key in decorsDict.Keys)
                    {
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
        public static string CheckVariant(Block block)
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
