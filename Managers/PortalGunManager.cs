using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using StardewModdingAPI;
using StardewValley;

namespace MultiplayerPortalGuns
{
    /// <summary>
    /// PortalGunManager provides a set of functions for updating the logic of the
    /// portal guns. It creates the PortalGun objects and retrieves data
    /// </summary>
    class PortalGunManager
    {
        public const int MAX_PORTAL_GUNS = 4;
        public const int MAX_PORTALS = 2;

        PortalGun[] PortalGuns = new PortalGun[MAX_PORTAL_GUNS];
        CustomObjectData[] PortalGunObjects = new CustomObjectData[MAX_PORTAL_GUNS];

        /*Loading*/
        public PortalGunManager(IContentHelper contentHelper)
        {
            LoadPortalGuns(contentHelper);
        }

        public void LoadPortalGuns(IContentHelper contentHelper)
        {
            for (int i = 0; i < MAX_PORTAL_GUNS; i++)
            {
                string portalGunId = "PortalGun" + i + "Id";
                Texture2D portalGunTexture = contentHelper.Load<Texture2D>(
                    $"Assets{Path.DirectorySeparatorChar}PortalGun" + (i + 1) + ".png");

                PortalGunObjects[i] = CustomObjectData.newObject(portalGunId, portalGunTexture,
                    Color.White, "Portal Gun " + i, "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "",
                    craftingData: new CraftingData("Portal Gun " + i, "388 1"));

                PortalGuns[i] = new PortalGun(portalGunId, i, MAX_PORTALS);
            }
        }

        public PortalPosition NewPortalPosition(int playerIndex, int portalIndex)
        {
            return PortalGuns[playerIndex].NewPortalPosition(portalIndex);
        }

        public PortalPosition GetPortalPosition(int playerIndex, int portalIndex)
        {
            return PortalGuns[playerIndex].GetPortalPosition(portalIndex);
        }

        public void AddPortals(List<PortalPosition> portalSpritesToAdd)
        {
            foreach (PortalPosition portalPosition in portalSpritesToAdd)
                AddPortal(portalPosition);
        }
        public void AddPortal(PortalPosition portalPosition)
        {
            PortalGuns[portalPosition.PlayerIndex].AddPortal(portalPosition);
        }

        /// <summary>
        /// Returns the portal the targeted by the passed portalPosition
        /// </summary>
        public Portal GetTargetPortal(PortalPosition portalPosition)
        {
            return PortalGuns[portalPosition.PlayerIndex]
                .GetTargetPortal(portalPosition.Index);
        }

        public Warp GetWarp(PortalPosition portalPosition)
        {
            return PortalGuns[portalPosition.PlayerIndex].GetWarp(portalPosition.Index);
        }

        public void RemovePortals(int portalGunIndex)
        {
            PortalGuns[portalGunIndex].RemovePortals();
        }

        public List<Portal> GetPortals(int portalGunIndex)
        {
            return PortalGuns[portalGunIndex].GetPortals().ToList();
        }

        /******************************************************************************
        * PortalGun Logic
       *****************************************************************************/
        /// <summary>
        /// Logically adds each portalPosition to their respective PortalGun
        /// and updates the respective warps and sprites. Used when loading the game
        /// </summary>
        public void ReloadPortals(List<PortalPosition> portalPositions)
        {
            foreach (PortalPosition portalPosition in portalPositions)
            {
                if (portalPosition != null)
                    AddPortal(portalPosition);
            }
        }


    }
}
