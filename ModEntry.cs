using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

using System.Collections.Generic;
using System.IO;

namespace MultiplayerPortalGuns
{
    class ModEntry : Mod
    {
        // For portal retract key
        private ModConfig config;

        public const int MAX_PORTAL_GUNS = 4;
        public const int MAX_PORTALS = 2;

        private int PlayerIndex;
        public List<long> PlayerList = new List<long>();

        PortalGun[] PortalGuns = new PortalGun[MAX_PORTAL_GUNS];
        CustomObjectData[] PortalGunObjects = new CustomObjectData[MAX_PORTAL_GUNS];

        LocationUpdater<PortalPosition> PortalTable = new LocationUpdater<PortalPosition>();
        LocationUpdater<PortalPosition> PortalQueue = new LocationUpdater<PortalPosition>();
        LocationUpdater<Warp> RemovalQueue = new LocationUpdater<Warp>();


        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();

            Directory.CreateDirectory(
                $"{this.Helper.DirectoryPath}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}");

            helper.Events.GameLoop.SaveLoaded += this.AfterLoad;
            helper.Events.Player.Warped += this.Warped;
            helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;

        }

        private void AfterLoad(object sender, SaveLoadedEventArgs e)
        {

        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {

            if (!Context.IsWorldReady)
                return;

            else if (e.Button.IsUseToolButton() || e.Button.IsActionButton())
                PortalSpawner(e);


            // Retract portals of current portal gun
            else if (e.Button.ToString().ToLower() == this.config.RetractPortals.ToLower())
                RetractPortals(PlayerIndex);

        }

        private void RetractPortals(int portalGunIndex)
        {
            for (int i = 0; i < MAX_PORTALS; i++)
            {
                // Handle Warps
                RemoveWarp(PortalGuns[portalGunIndex].GetPortal(i).LocationName,
                    PortalGuns[portalGunIndex].GetWarp(i));
                // Handle Table
                RemovePortal(PortalGuns[portalGunIndex].GetPortal(i));
            }

            PortalGuns[portalGunIndex].RemovePortals();


            // Bells and Whistles
            Game1.switchToolAnimation();
            Game1.currentLocation.playSoundAt("serpentDie", Game1.player.getTileLocation());

            // Notify players
            int message = PlayerIndex;
            this.Helper.Multiplayer.SendMessage(message, "RetractPortals", modIDs: new[] { this.ModManifest.UniqueID });

        }

        private void PortalSpawner (ButtonPressedEventArgs e)
        {
            if (Game1.menuUp || Game1.activeClickableMenu != null || Game1.isFestival()
                || Game1.player.ActiveObject == null || Game1.player.CurrentItem.DisplayName != "Portal Gun")
                return;

            int index = 0;
            if (e.Button.IsUseToolButton())
                index = 0;

            else if (e.Button.IsActionButton())
                index = 1;

            PortalPosition portal = PortalGuns[PlayerIndex].GetPortalPosition(index);

            if (portal == null)
                return;

            PortalQueue.RemoveItem(portal);
            RemovePortal(portal);
            AddPortal(portal);

            // Tile animation and stuff

            this.Helper.Multiplayer.SendMessage(portal, "AddPortal", modIDs: new[] { this.ModManifest.UniqueID });

            // Bells and whistles
            Game1.switchToolAnimation();
            Game1.currentLocation.playSoundAt("debuffSpell", Game1.player.getTileLocation());

        }


        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "AddPortal")
                EnqueuePortal(e.ReadAs<PortalPosition>());
            
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "PlayerNumber")
                LoadPortalGuns(e.ReadAs<int>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "RetractPortals")
                RetractPortals(e.ReadAs<int>());

        }

        private void EnqueuePortal(PortalPosition portal)
        {
            if (portal.LocationName == Game1.currentLocation.Name)
            {
                PortalQueue.RemoveItem(portal);
                PortalTable.AddItem(portal.LocationName, portal);
                // tile animation, and sound
            }
            PortalQueue.AddItem(portal.LocationName, portal);
        }

        private void Warped(object sender, WarpedEventArgs e)
        {
            UpdateLocationsPortals(e.NewLocation.Name);
        }

        private void UpdateLocationsPortals(string newLocation)
        {
            List<PortalPosition> PortalsToAdd = PortalQueue.GetItemList(newLocation);

            // Handle outdated portals 
            DequeuePortalTable(PortalsToAdd);
            // Handle outdated warps
            RemoveQueuedWarps();
            // 
            AddPortals(PortalsToAdd);
        }

        
        private void DequeuePortalTable(List<PortalPosition> PortalsToAdd)
        {
            // remove existing portals's warps and table references
            foreach (PortalPosition portal in PortalsToAdd)
                RemovePortal(portal);
        }
        private bool RemovePortal(PortalPosition portal)
        {
            // if portal is not in the tables
            if (!PortalTable.HashCodeToLocation.ContainsKey(portal.Id))
                return false;

            // if existing portal exists in current map
            if (Game1.currentLocation.Name == PortalTable.GetLocationName(portal.Id))
            {
                // remove warp
                Game1.getLocationFromName(PortalTable.GetLocationName(portal.Id))
                    .warps.Remove(PortalGuns[portal.PlayerIndex].GetWarp(portal.Index));
                /* 
                 * TODO: replace tile here
                 */
            }
            else // portal exists in map, but not current location
                RemovalQueue.AddItem(portal.LocationName, PortalGuns[PlayerIndex].GetWarp(portal.Index));


            PortalTable.RemoveItem(portal);
            return true;
        }

        private bool RemoveWarp(string locationName, Warp warp)
        {
            if (!RemovalQueue.LocationToList.ContainsKey(locationName))
                return false;

            if (locationName == Game1.currentLocation.Name)
            {
                Game1.currentLocation.warps.Remove(warp);
                RemovalQueue.RemoveItem(warp);
            }

            else
                RemovalQueue.AddItem(locationName, warp);

            return true;
        }

        private void RemoveQueuedWarps()
        {
            string currentLocation = Game1.currentLocation.Name;
            if (!RemovalQueue.LocationToList.ContainsKey(currentLocation))
                return;

            // Remove every warp queued for removal in the currentLocation
            List<Warp> OldWarps = RemovalQueue.GetItemList(currentLocation);
            foreach (Warp warp in OldWarps)
                RemoveWarp(currentLocation, warp);

        }

        private void AddPortals(List<PortalPosition> PortalsToAdd)
        {
            foreach (PortalPosition portal in PortalsToAdd)
                AddPortal(portal);
        }

        private bool AddPortal(PortalPosition portal)
        {
            if (Game1.currentLocation.Name != portal.LocationName)
                return false;
 
            // add into PortalGun object
            PortalGuns[portal.PlayerIndex].AddPortal(portal);

            // add Warp tile into location
            Game1.getLocationFromName(Game1.currentLocation.Name).warps
                .Add(PortalGuns[portal.PlayerIndex].GetWarp(portal.Index));

            PortalTable.AddItem(portal.LocationName, portal);

            return true;
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
