using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearData : IExposable
    {
        public string factionDefName;
        public List<KindGearData> kindGearData = new List<KindGearData>();
        
        // 优化查询效率的字典索引
        [Unsaved]
        private Dictionary<string, KindGearData> kindGearDataDict;

        public FactionGearData() { }

        public FactionGearData(string factionDefName)
        {
            this.factionDefName = factionDefName;
            InitializeDictionary();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Collections.Look(ref kindGearData, "kindGearData", LookMode.Deep);
            if (kindGearData == null)
                kindGearData = new List<KindGearData>();
                
            // 重新初始化字典索引
            InitializeDictionary();
        }
        
        private void InitializeDictionary()
        {
            kindGearDataDict = new Dictionary<string, KindGearData>();
            foreach (var kindData in kindGearData)
            {
                if (!kindGearDataDict.ContainsKey(kindData.kindDefName))
                {
                    kindGearDataDict.Add(kindData.kindDefName, kindData);
                }
            }
        }

        public KindGearData GetOrCreateKindData(string kindDefName)
        {
            if (kindGearDataDict == null)
            {
                InitializeDictionary();
            }
            
            if (kindGearDataDict.TryGetValue(kindDefName, out var data))
            {
                return data;
            }
            
            data = new KindGearData(kindDefName);
            kindGearData.Add(data);
            kindGearDataDict.Add(kindDefName, data);
            return data;
        }

        public void ResetToDefault()
        {
            kindGearData.Clear();
            if (kindGearDataDict != null)
            {
                kindGearDataDict.Clear();
            }
        }
        
        // 添加或更新兵种数据
        public void AddOrUpdateKindData(KindGearData data)
        {
            var existing = kindGearData.FirstOrDefault(k => k.kindDefName == data.kindDefName);
            if (existing != null)
            {
                kindGearData.Remove(existing);
            }
            kindGearData.Add(data);
            
            // 同步更新字典索引
            if (kindGearDataDict == null)
            {
                InitializeDictionary();
            }
            
            if (kindGearDataDict.ContainsKey(data.kindDefName))
            {
                kindGearDataDict[data.kindDefName] = data;
            }
            else
            {
                kindGearDataDict.Add(data.kindDefName, data);
            }
        }
        
        // 获取指定兵种的数据
        public KindGearData GetKindData(string kindDefName)
        {
            return kindGearData.FirstOrDefault(k => k.kindDefName == kindDefName);
        }

        public FactionGearData DeepCopy()
        {
            var copy = new FactionGearData(factionDefName);
            foreach (var kind in kindGearData)
            {
                copy.kindGearData.Add(kind.DeepCopy());
            }
            copy.InitializeDictionary();
            return copy;
        }
    }
}