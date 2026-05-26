using UnityEditor;
using UnityEngine;

namespace Skald.Code.Editor
{
    internal static class SyncWithSkaldState
    {
        private const string LoggedInKey = "Skald.SyncWithSkald.LoggedIn";
        private const string TokenKey = "Skald.SyncWithSkald.Token";
        private const string LogoSearchQuery = "SkaldLogo t:Texture2D";

        public static bool IsLoggedIn // TODO: upgrade to OS keychain / credential manager storage
        {
            get => EditorPrefs.GetBool(LoggedInKey, false);
            private set => EditorPrefs.SetBool(LoggedInKey, value);
        }

        public static string Token
        {
            get => EditorPrefs.GetString(TokenKey, null);
            private set => EditorPrefs.SetString(TokenKey, value);
        }

        public static void Login(string token)
        {
            IsLoggedIn = true;
            Token = token;
        }

        public static void Logout()
        {
            IsLoggedIn = false;
            Token = null;
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
