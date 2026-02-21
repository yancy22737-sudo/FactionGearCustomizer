using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearCustomizerSettings : ModSettings
    {
        // 版本控制
        private int version = 2;
        public List<FactionGearData> factionGearData = new List<FactionGearData>();
        public List<FactionGearPreset> presets = new List<FactionGearPreset>();
        
        // 优化查询效率的字典索引
        [Unsaved]
        private Dictionary<string, FactionGearData> factionGearDataDict;

        // [新增] 强制忽略原版限制的全局开关
        public bool forceIgnoreRestrictions = false;

        // [New] Current active preset name
        public string currentPresetName = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref version, "version", 2);
            Scribe_Values.Look(ref forceIgnoreRestrictions, "forceIgnoreRestrictions", false);
            Scribe_Values.Look(ref currentPresetName, "currentPresetName");
            
            // 处理不同版本的数据结构
            if (version == 1)
            {
                // 旧版本使用字典，需要转换
                Dictionary<string, FactionGearData> oldFactionGearData = new Dictionary<string, FactionGearData>();
                Scribe_Collections.Look(ref oldFactionGearData, "factionGearData", LookMode.Value, LookMode.Deep);
                if (oldFactionGearData != null)
                {
                    factionGearData = oldFactionGearData.Values.ToList();
                }
            }
            else
            {
                // 新版本使用列表
                Scribe_Collections.Look(ref factionGearData, "factionGearData", LookMode.Deep);
            }
            
            if (factionGearData == null)
                factionGearData = new List<FactionGearData>();

            Scribe_Collections.Look(ref presets, "presets", LookMode.Deep);
            if (presets == null)
                presets = new List<FactionGearPreset>();
                
            // 重新初始化字典索引
            InitializeDictionary();
        }
        
        private void InitializeDictionary()
        {
            factionGearDataDict = new Dictionary<string, FactionGearData>();
            foreach (var factionData in factionGearData)
            {
                if (!factionGearDataDict.ContainsKey(factionData.factionDefName))
                {
                    factionGearDataDict.Add(factionData.factionDefName, factionData);
                }
            }
        }

        public void ResetToDefault()
        {
            factionGearData.Clear();
            if (factionGearDataDict != null)
            {
                factionGearDataDict.Clear();
            }
            FactionGearManager.LoadDefaultPresets();
            currentPresetName = null;
            Write();
        }

        public FactionGearData GetOrCreateFactionData(string factionDefName)
        {
            if (factionGearDataDict == null)
            {
                InitializeDictionary();
            }
            
            if (factionGearDataDict.TryGetValue(factionDefName, out var data))
            {
                return data;
            }
            
            data = new FactionGearData(factionDefName);
            factionGearData.Add(data);
            factionGearDataDict.Add(factionDefName, data);
            return data;
        }

        public void AddPreset(FactionGearPreset preset)
        {
            presets.Add(preset);
            Write();
        }

        public void RemovePreset(FactionGearPreset preset)
        {
            presets.Remove(preset);
            Write();
        }

        public void UpdatePreset(FactionGearPreset preset)
        {
            Write();
        }

        public FactionGearCustomizerSettings DeepCopy()
        {
            var copy = new FactionGearCustomizerSettings();
            copy.version = this.version;
            copy.forceIgnoreRestrictions = this.forceIgnoreRestrictions;
            copy.currentPresetName = this.currentPresetName;
            foreach (var faction in this.factionGearData)
            {
                copy.factionGearData.Add(faction.DeepCopy());
            }
            foreach (var preset in this.presets)
            {
                copy.presets.Add(preset.DeepCopy());
            }
            // Re-initialize dictionary for the copy
            // Use reflection or just let it initialize on demand
            return copy;
        }

        public void RestoreFrom(FactionGearCustomizerSettings source)
        {
            this.version = source.version;
            this.forceIgnoreRestrictions = source.forceIgnoreRestrictions;
            this.currentPresetName = source.currentPresetName;
            this.factionGearData.Clear();
            foreach (var faction in source.factionGearData)
            {
                this.factionGearData.Add(faction.DeepCopy());
            }
            this.presets.Clear();
            foreach (var preset in source.presets)
            {
                this.presets.Add(preset.DeepCopy());
            }
            // Clear dictionary to force re-initialization
            this.factionGearDataDict = null;
        }
    }
}