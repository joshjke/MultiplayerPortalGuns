using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPortalGuns
{
    class ModEntry : Mod
    {
        public List<long> PlayerList = new List<long>();
        private int PlayerIndex;
        //public LocationPortals LocationPortals = new LocationPortals();
        LocationUpdater<PortalPosition> PortalTable = new LocationUpdater<PortalPosition>();
        LocationUpdater<PortalPosition> PortalQueue = new LocationUpdater<PortalPosition>();
        LocationUpdater<Warp> RemovalQueue = new LocationUpdater<Warp>();

        public const int MAX_PORTAL_GUNS = 4;
        PortalGun[] PortalGuns = new PortalGun[MAX_PORTAL_GUNS];
        CustomObjectData[] PortalGunObjects = new CustomObjectData[MAX_PORTAL_GUNS];

        public Dictionary<string, List<PortalPosition>> locationPortals = new Dictionary<string, List<PortalPosition>>();
        //public Dictionary<int, string> portalsLocation = new Dictionary<int, string>();

        public override void Entry(IModHelper helper)
        {
            //throw new NotImplementedException();
            helper.Events.GameLoop.SaveLoaded += this.AfterLoad;
            helper.Events.Player.Warped += this.Warped;
            helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;

        }

        private void AfterLoad(object sender, SaveLoadedEventArgs e)
        {

        }

        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "AddPortal")
            {
                PortalPosition portal = e.ReadAs<PortalPosition>();

                if (portal.LocationName == Game1.currentLocation.Name)
                {
                    PortalQueue.RemoveItem(portal);
                    PortalTable.AddItem(portal.LocationName, portal);
                    // tile animation, and sound
                }
                PortalQueue.AddItem(portal.LocationName, portal);
            }
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "PlayerNumber")
            {
                LoadPortalGuns(e.ReadAs<int>());
            }
        }

        private bool RemoveWarp(PortalPosition portal)
        {

        }

        private void Warped(object sender, WarpedEventArgs e)
        {
            List<PortalPosition> PortalsToAdd = PortalQueue.GetItemList(e.NewLocation.Name);

            // remove existing portals's warps and table references
            foreach (PortalPosition portal in PortalsToAdd)
            {
                // if portal already exists
                if (PortalTable.HashCodeToLocation.ContainsKey(portal.Id))
                {
                    // if existing portal exists in current map
                    if (e.NewLocation.Name == PortalTable.GetLocationName(portal.Id))
                    {
                        // remove warp
                        Game1.getLocationFromName(PortalTable.GetLocationName(portal.Id))
                            .warps.Remove(PortalGuns[portal.PlayerIndex].GetWarp(portal.Index));
                        /* 
                         * TODO: replace tile here
                         */
                    }
                    else // portal exists in map, but not current location
                    {
                        RemovalQueue.AddItem(portal.LocationName, PortalGuns[PlayerIndex].GetWarp(portal.Index));
                        PortalTable.RemoveItem(portal);
                    }
                }
            }

            List<Warp> WarpsToRemove;
            if (RemovalQueue.LocationToList.ContainsKey(e.NewLocation.Name))
            {
                WarpsToRemove = RemovalQueue.GetItemList(e.NewLocation.Name);
                foreach (Warp warp in WarpsToRemove)
                {
                    // remove from map
                    RemovalQueue.RemoveItem(warp);
                }
            }

            foreach (PortalPosition portal in PortalsToAdd)
            {
                // add into PortalGun object
                PortalGuns[portal.PlayerIndex].AddPortal(portal);
                // add Warp tile into location
                Game1.getLocationFromName(portal.LocationName).warps.Add(PortalGuns[portal.PlayerIndex].GetWarp(portal.Index));
                // TODO: play animation
            }

        }

        private void Multiplayer_PeerContextReceived(object sender, PeerContextReceivedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                if (!PlayerList.Contains(e.Peer.PlayerID))
                {
                    PlayerList.Add(e.Peer.PlayerID);
                    int message = PlayerList.Count;
                    this.Helper.Multiplayer.SendMessage(message, "PlayerNumber", modIDs: new[] { this.ModManifest.UniqueID });
                    //this.Monitor.Log("SENT retract portal json", LogLevel.Debug);
                }
            }
        }

        private void AfterLoad(object sender, EventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                PlayerIndex = 1;
                PlayerList.Add(0);
                LoadPortalGuns(1);
            }

        }

        private void LoadPortalGuns(int playerNumber)
        {
            for (int i = 0; i < MAX_PORTAL_GUNS; i++)
            {
                string portalGunId = "PortalGun" + i + "Id";
                Texture2D portalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun" + i + ".png");
                PortalGunObjects[i] = CustomObjectData.newObject(portalGunId, portalGunTexture, Color.White, "Portal Gun",
                    "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Portal Gun", "388 1"));
                if (i == playerNumber)
                    PortalGunObjects[i].craftingData = new CraftingData("Portal Gun", "388 1");

                PortalGuns[i] = new PortalGun(portalGunId, i);

            }
        }

    }
}
