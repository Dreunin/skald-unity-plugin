using System.Threading.Tasks;
using UnityEngine;

namespace Skald.Code.Editor
{
    public class SyncWithScald
    {
        // Called by the custom inspector when the user clicks Login
        public Awaitable Login()
        {
            Debug.Log("Logging in");
            // TODO: Log in
			SyncWithScaldState.IsLoggedIn = true;
            return null;
        }

        // Called by the custom inspector when the user clicks Sync
        public Awaitable Sync()
        {
            Debug.Log("Syncing");
            // TODO: Put sync logic here
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
