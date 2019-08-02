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
        public Dictionary<string, List<PortalPosition>> LocationsPortals { get; set; }
        public Dictionary<int, string> PortalsLocation { get; set; }

        internal LocationPortals()
        {
            LocationsPortals = new Dictionary<string, List<PortalPosition>>();
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

        public bool AddPortal(PortalPosition portal)
        {
            // if the portal already exists in the mappings
            if (PortalsLocation.ContainsKey(portal.Id))
            {
                // try to remove it
                if (!LocationsPortals[PortalsLocation[portal.Id]].Remove(portal))
                    return false;
                // set the new mapping
                PortalsLocation[portal.Id] = portal.LocationName;
            }
            else // portal does not exist in mapping
            {
                // if location does not have a mapping with a portal list
                if (!LocationsPortals.ContainsKey(portal.LocationName))
                {
                    // add location and give it a new portal list
                    LocationsPortals.Add(portal.LocationName, new List<PortalPosition>());
                }
                // add the new portal to the mapping
                PortalsLocation.Add(portal.Id, portal.LocationName);
            }
            // add the portal to the Location's portal list
            LocationsPortals[portal.LocationName].Add(portal);
            
            return true;
        }
    }
}
