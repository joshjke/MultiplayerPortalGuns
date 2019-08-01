using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPortalGuns
{
    internal class LocationPortals
    {
        public Dictionary<string, List<Portal>> LocationsPortals { get; set; }
        public Dictionary<int, string> PortalsLocation { get; set; }

        internal LocationPortals()
        {
            LocationsPortals = new Dictionary<string, List<Portal>>();
            PortalsLocation = new Dictionary<int, string>();
        }
        /*
        /// <summary>
        /// List of Game Locations
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<GameLocation> GetLocations()
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );
        }
        */

        public bool AddPortal(Portal newPortal)
        {
            // if the portal already exists in the mappings
            if (PortalsLocation.ContainsKey(newPortal.id))
            {
                // try to remove it
                if (!LocationsPortals[PortalsLocation[newPortal.id]].Remove(newPortal))
                    return false;
                // set the new mapping
                PortalsLocation[newPortal.id] = newPortal.PortalPos.LocationName;
            }
            else // portal does not exist in mapping
            {
                // if location does not have a mapping with a portal list
                if (!LocationsPortals.ContainsKey(newPortal.PortalPos.LocationName))
                {
                    // add location and give it a new portal list
                    LocationsPortals.Add(newPortal.PortalPos.LocationName, new List<Portal>());
                }
                // add the new portal to the mapping
                PortalsLocation.Add(newPortal.id, newPortal.PortalPos.LocationName);
            }
            // add the portal to the Location's portal list
            LocationsPortals[newPortal.PortalPos.LocationName].Add(newPortal);
            
            return true;
        }
    }
}
