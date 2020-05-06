
using StardewValley;

namespace MultiplayerPortalGuns
{
   class PortalGun
    {
        private int MaxPortals;
        private string Name { get; }

        private int PlayerIndex { get; }

        Portal[] portals;

        public PortalGun(string Name, int PlayerIndex, int MaxPortals)
        {
            this.Name = Name;
            this.PlayerIndex = PlayerIndex;
            this.MaxPortals = MaxPortals;

            portals = new Portal[MaxPortals];

            for (int i = 0; i < MaxPortals; i++)
            {
                portals[i] = new Portal
                {
                    PortalPos = new PortalPosition(i, this.Name, this.PlayerIndex),
                    Warp = null
                };
            }
        }

        public void RemovePortals()
        {
            RemoveWarps();
            for (int i = 0; i < MaxPortals; i++)
            {
                portals[i].PortalPos.LocationName = null;
            }
        }

        void RemoveWarps()
        {
            // (sanitize for active multiplayer)
            for (int i = 0; i < MaxPortals; i++)
                portals[i].Warp = null;
        }

        public Warp GetWarp(int index)
        {
            return portals[index].Warp;
        }

        public bool AddPortal(PortalPosition portalPos)
        {
            /*// sanitization for multiplayer
            if (!ValidPortalPos(portalPos))
                return false;*/

            portals[portalPos.Index].PortalPos = portalPos;

            RemoveWarps(); // set warps to null
            CreateWarps(); // create new based on new portalPos

            return true;
        }

        public Portal GetPortal(int index)
        {
            return portals[index];
        }

        public Portal[] GetPortals()
        {
            return this.portals;
        }

        public PortalPosition GetPortalPosition(int index)
        {
            
            PortalPosition portal = new PortalPosition(index, this.Name, this.PlayerIndex, 
                (int)Game1.currentCursorTile.X, (int)Game1.currentCursorTile.Y, Game1.currentLocation.Name);
            if (ValidPortalPos(portal))
            {
                //portals[index].PortalPos = portal;
                //CreateWarps();
                return portal;
            }

            else
                return null;
        }

        private bool ValidPortalPos(PortalPosition portalPos)
        {
            return Game1.getLocationFromName(portalPos.LocationName).isTileLocationTotallyClearAndPlaceable(portalPos.X, portalPos.Y)
                && Game1.getLocationFromName(portalPos.LocationName).isTileLocationTotallyClearAndPlaceable(portalPos.X + 1, portalPos.Y);
        }

        // returns number of created warps
        int CreateWarps()
        {
            int counter = 0;
            for (int i = 0; i < MaxPortals; i++)
            {
                if (CreateWarp(i))
                    ++counter;
            }
            return counter;
        }

        bool CreateWarp(int index)
        {
            if (portals[index].PortalPos.LocationName == ""
                || portals[index].PortalPos.LocationName == null)
                return false;

            int target = GetTargetIndex(index);
            if (portals[target].PortalPos.LocationName == ""
                || portals[target].PortalPos.LocationName == null)
                return false;

            portals[index].Warp = new Warp(
                portals[index].PortalPos.X, 
                portals[index].PortalPos.Y, 
                portals[target].PortalPos.LocationName,
                portals[target].PortalPos.X + 1, 
                portals[target].PortalPos.Y,
                false
                );

            return true;
        }

        private int GetTargetIndex(int index)
        {
            return index == 0 ? 1 : 0;
        }

        public Portal GetTargetPortal(int index)
        {
            return portals[GetTargetIndex(index)];
        }
    }
}
