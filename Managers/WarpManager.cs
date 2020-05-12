using StardewModdingAPI;
using StardewValley;

namespace MultiplayerPortalGuns
{
    /// <summary>
    /// Handles the warp logic for creating and removing warps
    /// </summary>
    static class WarpManager
    {

        /// <summary>
        /// The main player adds the warp to the world
        /// </summary>
        public static bool AddWarp(PortalPosition portalPosition, bool IsMainPlayer, PortalGunManager portalGuns, IMultiplayerHelper multiplayer, string uniqueModId)
        {
            if (portalPosition.LocationName == "" || portalPosition.LocationName == null)
                return false;
            // Warps are global, so only have host handle them to avoid duplicates and ghosts
            if (!IsMainPlayer)
            {
                multiplayer.SendMessage(portalPosition, "AddWarp", modIDs: new[] { uniqueModId });
                return true;
            }
            // if a warp was created, add it
            Warp warp = portalGuns.GetWarp(portalPosition);
            if (warp == null)
                return false;

            // add it to the game location
            Game1.getLocationFromName(portalPosition.LocationName).warps.Add(warp);
            return true;
        }
        /// <summary>
        /// The main player removes the warp at the portalPosition
        /// </summary>
        public static bool RemoveWarp(PortalPosition portalPosition, bool IsMainPlayer, PortalGunManager portalGuns, IMultiplayerHelper multiplayer, string uniqueModId)
        {
            // Warps are global, so only have host handle them to avoid duplicates and ghosts
            if (!IsMainPlayer)
            {
                multiplayer.SendMessage(portalPosition, "RemoveWarp", modIDs: new[] { uniqueModId });
                return false;
            }
            string locationName = portalPosition.LocationName;
            if (locationName == "" || locationName == null)
                return false;
            GameLocation location = Game1.getLocationFromName(portalPosition.LocationName);

            // remove the warp from the location
            Warp warp = portalGuns.GetWarp(portalPosition);
            if (warp != null)
                location.warps.Remove(portalGuns.GetWarp(portalPosition));

            return true;
        }

    }
}
