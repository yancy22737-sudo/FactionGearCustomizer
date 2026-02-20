using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    // 静态构造函数类，用于提前缓存贴图资源
    [StaticConstructorOnStartup]
    public static class TexCache
    {
        public static readonly Texture2D CopyTex;
        public static readonly Texture2D PasteTex;
        public static readonly Texture2D ApplyTex;

        static TexCache()
        {
            // 提前加载并缓存贴图，避免在UI循环中实时读取硬盘
            // 尝试加载图标，如果失败则使用 null 安全处理
            CopyTex = TryLoadTexture("UI/Buttons/Copy");
            PasteTex = TryLoadTexture("UI/Buttons/Paste");
            ApplyTex = TryLoadTexture("UI/Buttons/Confirm"); // 使用 Confirm 代替 Apply，这个更可能存在
        }

        private static Texture2D TryLoadTexture(string path)
        {
            try
            {
                return ContentFinder<Texture2D>.Get(path, false);
            }
            catch
            {
                return null;
            }
        }
    }
}