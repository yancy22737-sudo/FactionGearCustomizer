using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearPreset : IExposable
    {
        public string name = "New Preset";
        public string description = "";
        // 存储包含修改的派系数据
        public List<FactionGearData> factionGearData = new List<FactionGearData>();
        // 存储该预设所需的模组列表
        public List<string> requiredMods = new List<string>();

        public FactionGearPreset() { }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "New Preset");
            Scribe_Values.Look(ref description, "description", "");
            Scribe_Collections.Look(ref factionGearData, "factionGearData", LookMode.Deep);
            Scribe_Collections.Look(ref requiredMods, "requiredMods", LookMode.Value);

            if (factionGearData == null) factionGearData = new List<FactionGearData>();
            if (requiredMods == null) requiredMods = new List<string>();
        }

        // 自动计算并更新所需的模组列表
        public void CalculateRequiredMods()
        {
            HashSet<string> mods = new HashSet<string>();
            foreach (var faction in factionGearData)
            {
                foreach (var kind in faction.kindGearData)
                {
                    var allGear = kind.weapons.Concat(kind.meleeWeapons).Concat(kind.armors).Concat(kind.apparel).Concat(kind.others);
                    foreach (var gear in allGear)
                    {
                        var def = gear.ThingDef;
                        if (def != null && def.modContentPack != null && !def.modContentPack.IsCoreMod)
                        {
                            mods.Add(def.modContentPack.Name);
                        }
                    }
                }
            }
            requiredMods = mods.ToList();
        }

        // 从当前游戏设置中抓取被修改的数据并深拷贝保存
        public void SaveFromCurrentSettings(List<FactionGearData> currentSettingsData)
        {
            factionGearData.Clear();
            foreach (var faction in currentSettingsData)
            {
                // 只保存有修改兵种的派系
                var modifiedKinds = faction.kindGearData.Where(k => k.isModified).ToList();
                if (modifiedKinds.Any())
                {
                    // 创建深拷贝
                    FactionGearData newFactionData = new FactionGearData(faction.factionDefName);
                    foreach (var kind in modifiedKinds)
                    {
                        KindGearData newKindData = new KindGearData(kind.kindDefName)
                        {
                            isModified = true,
                            weapons = kind.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            meleeWeapons = kind.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            armors = kind.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            apparel = kind.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            others = kind.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList()
                        };
                        newFactionData.kindGearData.Add(newKindData);
                    }
                    factionGearData.Add(newFactionData);
                }
            }
            CalculateRequiredMods();
        }
    }
}