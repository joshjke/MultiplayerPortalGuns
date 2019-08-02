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
        public Dictionary<string, List<PortalPosition>> LocationToList { get; set; }
        public Dictionary<int, string> IdToLocation { get; set; }

        internal LocationPortals()
        {
            LocationToList = new Dictionary<string, List<PortalPosition>>();
            IdToLocation = new Dictionary<int, string>();
        }

        public bool AddPortal(PortalPosition portal)
        {
            // if the portal already exists in the mappings
            if (IdToLocation.ContainsKey(portal.Id))
            {
                // try to remove it
                if (!LocationToList[IdToLocation[portal.Id]].Remove(portal))
                    return false;
                // set the new mapping
                IdToLocation[portal.Id] = portal.LocationName;
            }
            else // portal does not exist in mapping
            {
                // if location does not have a mapping with a portal list
                if (!LocationToList.ContainsKey(portal.LocationName))
                {
                    // add location and give it a new portal list
                    LocationToList.Add(portal.LocationName, new List<PortalPosition>());
                }
                // add the new portal to the mapping
                IdToLocation.Add(portal.Id, portal.LocationName);
            }
            // add the portal to the Location's portal list
            LocationToList[portal.LocationName].Add(portal);
            
            return true;
        }
        public bool RemovePortal(PortalPosition portal)
        {
            if (IdToLocation.ContainsKey(portal.Id))
            {
                if (!LocationToList[IdToLocation[portal.Id]].Remove(portal))
                    return false;
            }
            if (!IdToLocation.Remove(portal.Id))
                return false;

            return true;
        }
    }

}
