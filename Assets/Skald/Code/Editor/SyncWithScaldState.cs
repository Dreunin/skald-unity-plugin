using UnityEditor;
using UnityEngine;

namespace Skald.Code.Editor
{
    internal static class SyncWithScaldState
    {
        private const string LoggedInKey = "Skald.SyncWithScald.LoggedIn";
        private const string LogoSearchQuery = "SkaldLogo t:Texture2D";

        public static bool IsLoggedIn
        {
            get => EditorPrefs.GetBool(LoggedInKey, false);
            set => EditorPrefs.SetBool(LoggedInKey, value);
        }

        public static Texture2D LoadLogoTexture()
        {
            string[] guids = AssetDatabase.FindAssets(LogoSearchQuery);
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}


