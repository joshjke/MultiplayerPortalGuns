using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PyTK.Extensions;
using PyTK.Types;
using PyTK.CustomElementHandler;
using PyTK.CustomTV;
using Microsoft.Xna.Framework;
using StardewValley;

namespace MultiplayerPortalGuns
{
   class PortalGun
    {
        private string name { get; }

        Portal[] portals = new Portal[2];

        PortalGun(string name)
        {
            this.name = name;

            for (int i = 0; i < 2; i++)
            {
                portals[i] = new Portal
                {
                    // unique id for retrieving portal from Lists
                    id = new StringBuilder().Append(name).Append(i).GetHashCode(),
                    PortalPos = new PortalPosition
                    {
                        LocationName = null
                    },
                    Warp = null
                };
            }
        }

        PortalGun ReassignPortals(PortalGun portalGun)
        {
            //CreatePortal(portalGun.portals[0])
            return portalGun;
        }

        void AddWarps()
        {
            // (sanitize for active multiplayer)
            for (int i = 0; i < 2; i++)
            {
                if (portals[0].Warp != null)
                {
                    // if (Game1.getLocationFromName(portals[i].portalPos.locationName) == Game1.currentLocation) // dirty multiplayer line
                    Game1.getLocationFromName(portals[i].PortalPos.LocationName).warps.Add(portals[i].Warp);
                }
            }
        }

        void RemoveWarps()
        {
            // (sanitize for active multiplayer)
            for (int i = 0; i < 2; i++)
            {
                if (portals[i].Warp != null)
                {
                    Game1.getLocationFromName(portals[i].PortalPos.LocationName).warps.Remove(portals[i].Warp);
                    portals[i].Warp = null;
                }
            }
        }

        void RemovePortals()
        {
            RemoveWarps();
            for (int i = 0; i < 2; i++)
            {
                portals[i].PortalPos.LocationName = null;
            }
        }

        bool CreatePortal(int index)
        {
            PortalPosition portalPos = GetPortalPosition();
            return AddPortal(index, portalPos);
        }

        bool AddPortal(int index, PortalPosition portalPos)
        {
            // sanitize for multiplayer
            if (!IsPortalPosValid(portalPos))
                return false;

            portals[index].PortalPos = portalPos;

            RemoveWarps(); // remove old (sanitize for active multiplayer)
            CreateWarps(); // create new based on new portalPos
            AddWarps(); // add warps to map (sanitize for active multiplayer)

            return true;
        }

        PortalPosition GetPortalPosition()
        {
            PortalPosition portalPos = new PortalPosition
            {
                X = Game1.getMouseX(),
                Y = Game1.getMouseY(),
                LocationName = Game1.currentLocation.Name
            };

            return portalPos;
        }

        bool IsPortalPosValid(PortalPosition portalPos)
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
