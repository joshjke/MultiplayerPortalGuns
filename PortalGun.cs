
using StardewValley;

namespace MultiplayerPortalGuns
{
   class PortalGun
    {
        private string Name { get; }

        private int PlayerIndex { get; }

        Portal[] portals = new Portal[2];

        public PortalGun(string Name, int PlayerIndex)
        {
            this.Name = Name;
            this.PlayerIndex = PlayerIndex;

            for (int i = 0; i < 2; i++)
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
            for (int i = 0; i < 2; i++)
            {
                portals[i].PortalPos.LocationName = null;
            }
        }

        void RemoveWarps()
        {
            // (sanitize for active multiplayer)
            for (int i = 0; i < 2; i++)
                portals[i].Warp = null;

        }

        public Warp GetWarp(int index)
        {
            return portals[index].Warp;
        }

        public bool AddPortal(PortalPosition portalPos)
        {
            // sanitize for multiplayer
            if (!ValidPortalPos(portalPos))
                return false;

            portals[portalPos.Index].PortalPos = portalPos;

            RemoveWarps(); // set warps to null
            CreateWarps(); // create new based on new portalPos

            return true;
        }

        public PortalPosition GetPortal(int index)
        {
            return portals[index].PortalPos;
        }

        public PortalPosition GetPortalPosition(int index)
        {
            
            PortalPosition portal = new PortalPosition(index, this.Name, this.PlayerIndex, Game1.getMouseX(), Game1.getMouseY(), Game1.currentLocation.Name);
            if (ValidPortalPos(portal))
                return portal;

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
            for (int i = 0; i < 2; i++)
            {
                if (CreateWarp(i))
                    ++counter;
            }
            return counter;
        }

        bool CreateWarp(int index)
        {
            if (portals[index].PortalPos.LocationName == "")
                return false;

            int target = GetTargetIndex(index);
            if (portals[target].PortalPos.LocationName == "")
                return false;

            portals[index].Warp = new Warp(
                portals[index].PortalPos.X, 
                portals[index].PortalPos.Y, 
                portals[index].PortalPos.LocationName,
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
    }
}
