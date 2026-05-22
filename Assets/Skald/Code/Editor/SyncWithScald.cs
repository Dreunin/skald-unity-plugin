using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Skald.Code.Editor
{
    public class SyncWithScald
    {
        // Called by the custom inspector when the user clicks Login
        public Awaitable Login()
        {
            Debug.Log("Logging in");
            // TODO: Log in
            try
            {
                Process.Start("explorer", "https://skald.dual-daggers.com/sync");

			    SyncWithScaldState.IsLoggedIn = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during sync: {e.Message}");
                
            }
            return null;
        }

        // Called by the custom inspector when the user clicks Sync
        public Awaitable Sync()
        {
            Debug.Log("Syncing");
            // TODO: Put sync logic here
            try
            {
                // sync
            } catch (Exception e)
            {
                Debug.LogError($"Error during sync: {e.Message}");
                //Logout if sync fails
                SyncWithScaldState.IsLoggedIn = false;
            }
            return null;
        }

        public Awaitable Logout()
        {
            Debug.Log("Logging out");
			SyncWithScaldState.IsLoggedIn = false;
            //TODO: Logout
            return null;
        }
    }
}
