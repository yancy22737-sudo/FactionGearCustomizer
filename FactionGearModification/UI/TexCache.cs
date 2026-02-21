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
        public static readonly Texture2D UndoTex;
        public static readonly Texture2D RedoTex;

        static TexCache()
        {
            // 提前加载并缓存贴图，避免在UI循环中实时读取硬盘
            // 尝试加载图标，如果失败则使用 null 安全处理
            CopyTex = TryLoadTexture("UI/Buttons/Copy");
            PasteTex = TryLoadTexture("UI/Buttons/Paste");
            ApplyTex = TryLoadTexture("UI/Buttons/Confirm"); // 使用 Confirm 代替 Apply，这个更可能存在
            
            // Undo/Redo icons - using arrow icons from RimWorld's UI
            UndoTex = TryLoadTexture("UI/Buttons/Previous"); // 使用 Previous 作为 Undo图标
            RedoTex = TryLoadTexture("UI/Buttons/Next");     // 使用 Next 作为 Redo图标
            
            // Fallback to rotate icons if Previous/Next don't exist
            if (UndoTex == null)
                UndoTex = TryLoadTexture("UI/Buttons/RotateLeft");
            if (RedoTex == null)
                RedoTex = TryLoadTexture("UI/Buttons/RotateRight");
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