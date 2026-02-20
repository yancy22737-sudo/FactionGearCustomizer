using System;
using System.IO;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public static class PresetIOManager
    {
        // 导出预设为 Base64 字符串
        public static string ExportToBase64(FactionGearPreset preset)
        {
            string path = Path.Combine(GenFilePaths.ConfigFolderPath, "TempPresetExport.xml");
            try
            {
                Scribe.saver.InitSaving(path, "FactionGearPreset");
                Scribe_Deep.Look(ref preset, "Preset");
                Scribe.saver.FinalizeSaving();
                
                string xml = File.ReadAllText(path);
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xml));
            }
            catch (System.Exception e)
            {
                Log.Error("[FactionGearCustomizer] Export failed: " + e.Message);
                return null;
            }
        }

        // 从 Base64 字符串导入预设
        public static FactionGearPreset ImportFromBase64(string base64)
        {
            string path = Path.Combine(GenFilePaths.ConfigFolderPath, "TempPresetImport.xml");
            try
            {
                string xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                File.WriteAllText(path, xml);

                FactionGearPreset preset = null;
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref preset, "Preset");
                Scribe.loader.FinalizeLoading();

                return preset;
            }
            catch
            {
                return null; // 导入失败（字符串不合法）
            }
        }
    }
}