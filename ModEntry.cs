using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using xTile.Layers;
using xTile.Tiles;

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
        LocationUpdater<Warp> WarpRemovalQueue = new LocationUpdater<Warp>();

        private string TileSheetPath;


        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();

            Directory.CreateDirectory(
                $"{this.Helper.DirectoryPath}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}");

            TileSheetPath = this.Helper.Content.GetActualAssetKey($"Assets{Path.DirectorySeparatorChar}PortalsAnimated3.png", ContentSource.ModFolder);

            helper.Events.GameLoop.SaveLoaded += this.AfterLoad;
            helper.Events.Player.Warped += this.Warped;
            helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;

        }


        private void AfterLoad(object sender, SaveLoadedEventArgs e)
        {
            PortalQueue = new LocationUpdater<PortalPosition>();
            WarpRemovalQueue = new LocationUpdater<Warp>();

            this.Monitor.Log("hitting after load, SaveLoadedEventArgs");

            if (Context.IsMainPlayer)
            {
                PlayerIndex = 1;
                PlayerList.Add(0);
                LoadPortalGuns(1);

                LoadPortalTextures();
                LoadMinePortals();
            }
            
            // TODO LoadPortalSaves();
        }
        /* // Not sure why there's a duplicate function, investigate w/ mutliplayer
        private void AfterLoad(object sender, EventArgs e)
        {
            this.Monitor.Log("hitting after load, eventArs");
            if (Context.IsMainPlayer)
            {
                PlayerIndex = 1;
                PlayerList.Add(0);
                LoadPortalGuns(1);

                LoadPortalTextures();
                LoadMinePortals();
            }

            // TODO LoadPortalSaves();
        } */

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
                RemovePortals(portalGunIndex);
            }

            PortalGuns[portalGunIndex].RemovePortals();


            // Bells and Whistles
            Game1.switchToolAnimation();
            Game1.currentLocation.playSoundAt("serpentDie", Game1.player.getTileLocation());

            // Notify players
            int message = PlayerIndex;
            this.Helper.Multiplayer.SendMessage(message, "RetractPortals", modIDs: new[] { this.ModManifest.UniqueID });

        }

        private void PortalSpawner(ButtonPressedEventArgs e)
        {
            if (Game1.menuUp || Game1.activeClickableMenu != null || Game1.isFestival()
                || Game1.player.ActiveObject == null || Game1.player.CurrentItem.DisplayName != "Portal Gun")
                return;

            int index = 0;
            if (e.Button.IsUseToolButton())
                index = 0;

            else if (e.Button.IsActionButton())
                index = 1;
            // debugstuff
            Game1.switchToolAnimation();


            PortalPosition portalPos = PortalGuns[PlayerIndex].GetPortalPosition(index);


            if (portalPos == null)
            {
                this.Monitor.Log("portal is null", LogLevel.Debug);
                //Game1.currentLocation.playSoundAt("debuffSpell", Game1.player.getTileLocation());
                return;
            }

            Portal portal = new Portal(portalPos);
            
            //SpawnPortals(index);

            PortalQueue.RemoveItem(portalPos);
            RemovePortals(PlayerIndex);

            //AddPortals
            AddPortal(portalPos);

            // Tile animation and stuff

            // TODO SAVE OLD TILE HERE

            // Removing old tile

            Game1.getLocationFromName(portalPos.LocationName).removeTile(portalPos.X, portalPos.Y, "Buildings");

            Layer layer = Game1.getLocationFromName(portalPos.LocationName).map.GetLayer("Buildings");
            TileSheet tileSheet = Game1.getLocationFromName(portalPos.LocationName).map.GetTileSheet("z_portal-spritesheet");
            // save old tile
            portal.OldTile = layer.Tiles[portalPos.X, portalPos.Y];
            // place portal sprite on tile location
            layer.Tiles[portalPos.X, portalPos.Y] = new StaticTile(layer, tileSheet, BlendMode.Additive, tileIndex:4); // Multiplayer error here "The specified tileSheet is not in the parent map"

            // TODO animate the portal spawning
            //PortalAnimationFrame[i] + i * 5

            this.Helper.Multiplayer.SendMessage(portalPos, "AddPortal", modIDs: new[] { this.ModManifest.UniqueID });

            // Bells and whistles
            Game1.currentLocation.playSoundAt("debuffSpell", Game1.player.getTileLocation());

        }



        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            // Message from Host to targeted Peer to give PortalTable to them
            if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "SetPortalTable" + this.PlayerIndex)
                SetPortalTable(e.ReadAs<LocationUpdater<PortalPosition>>());

            // Message from any player to add a portal to the game
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "AddPortal")
                EnqueuePortal(e.ReadAs<PortalPosition>());

            // Message from host to targeted Peer to assign PlayerIndex
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "PlayerIndex" + Game1.player.UniqueMultiplayerID)
                LoadPortalGuns(e.ReadAs<int>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "RetractPortals")
                RetractPortals(e.ReadAs<int>());

        }

        private void SetPortalTable(LocationUpdater<PortalPosition> PortalTable)
        {
            this.PortalTable = PortalTable;
        }

        private void EnqueuePortal(PortalPosition portal)
        {
            if (Context.IsMainPlayer || portal.LocationName == Game1.currentLocation.Name)
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
            if (Context.IsMainPlayer)
            {
                if (!(e.NewLocation is MineShaft mine))
                    return;

                if (mine.mineLevel == Game1.player.deepestMineLevel)
                    return;

                // deepestMineLevel has changed
                LoadMinePortals();
            }
            else
            {
                LoadTileSheet(e.NewLocation);
            }
        }

        private void LoadTileSheet(GameLocation location)
        {
            // Add the tilesheet.
            TileSheet tileSheet = new TileSheet(
               id: "z_portal-spritesheet", // a unique ID for the tilesheet
               map: location.map,
               imageSource: TileSheetPath,
               sheetSize: new xTile.Dimensions.Size(800, 16), // the pixel size of your tilesheet image.
               tileSize: new xTile.Dimensions.Size(16, 16) // should always be 16x16 for maps
            );

            if (location != null && location.map != null && tileSheet != null)
            {
                //this.Monitor.Log("adding and loading tilesheet for location: " + location.Name, LogLevel.Debug);
                location.map.AddTileSheet(tileSheet);// Multiplayer load error here
                location.map.LoadTileSheets(Game1.mapDisplayDevice);
            }
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
            if (PortalsToAdd == null)
                return;

            foreach (PortalPosition portal in PortalsToAdd) // host failure here Object reference not set to an instance of an object
                RemovePortals(portal.PlayerIndex);
        }

        private bool RemovePortals(int gunIndex)
        {
            foreach (Portal removablePortal in PortalGuns[gunIndex].GetPortals())
            {
                /*if (RemovePortal(removablePortal) == false)
                    return false;*/
                RemovePortal(removablePortal);
            }
            return true;
        }
        private bool RemovePortal(Portal portal)
        {
            // if portal is not in the tables
            if (!PortalTable.ItemToLocation.ContainsKey(portal.PortalPos.Id))
                return false;

            // if existing portal exists in current map
            string locationName = PortalTable.GetLocationName(portal.PortalPos.Id);
            if (Context.IsMainPlayer || Game1.currentLocation.Name == locationName)
            {
                // remove warp
                //Game1.currentLocation.warps.Remove(portal.Warp);
                Game1.getLocationFromName(locationName).warps.Remove(portal.Warp);
                Layer layer = Game1.getLocationFromName(portal.PortalPos.LocationName).map.GetLayer("Buildings");

                // Remove portal tile
                if (layer.Tiles[portal.PortalPos.X, portal.PortalPos.Y] != null)
                    Game1.getLocationFromName(portal.PortalPos.LocationName).removeTile(portal.PortalPos.X, portal.PortalPos.Y, "Buildings");

                TileSheet tileSheet = Game1.getLocationFromName(portal.PortalPos.LocationName).map.GetTileSheet("z_portal-spritesheet");

                // Place the old tile
                layer.Tiles[portal.PortalPos.X, portal.PortalPos.Y] = portal.OldTile;
            }
            else // portal exists in map, but not current location
                RemovalQueue.AddItem(portal.PortalPos.LocationName, portal.Warp);

            PortalTable.RemoveItem(portal.PortalPos);

            return true;
        }
        /// <summary>
        /// Handles immediate / queuing removal of a warp
        /// </summary>
        /// <param name="portal">uses the portal's locationName and Warp</param>
        /// <returns></returns>
        private bool RemoveWarp(Portal portal)
        {
            return RemoveWarp(portal.PortalPos.LocationName, portal.Warp);
        }
        /// <summary>
        /// Handles immediate / queuing removal of a warp
        /// </summary>
        /// <param name="locationName">The location the warp is in</param>
        /// <param name="warp">The warp to be removed</param>
        /// <returns></returns>
        private bool RemoveWarp(string locationName, Warp warp)
        {
            if (locationName == null || locationName == "")
                return false;
            // remove immediately
            if (locationName == Game1.currentLocation.Name)
            {
                Game1.currentLocation.warps.Remove(warp);
                WarpRemovalQueue.RemoveItem(warp);
            }
            // remove later
            else
            {
                // add to warpRemovalQueue if not already in
                WarpRemovalQueue.AddItem(locationName, warp);
            }
            return true;
        }

        private void RemoveQueuedWarps()
        {
            string currentLocation = Game1.currentLocation.Name;
            if (!WarpRemovalQueue.LocationToItemList.TryGetValue(
                currentLocation, out List<Warp> OldWarps))
                return;

            // Remove every warp queued for removal in the currentLocation
            foreach (Warp warp in OldWarps)
                RemoveWarp(currentLocation, warp);

        }

        private void AddPortals(List<PortalPosition> PortalsToAdd)
        {
            if (PortalsToAdd == null)
                return;
            foreach (PortalPosition portal in PortalsToAdd)
                AddPortal(portal);
        }

        private bool AddPortal(PortalPosition portalPosition)
        {
            if (portalPosition.LocationName != Game1.currentLocation.Name)
            {
                // portal should be queued instead
                return false;
            }
            // get the old target's warp incase it is removed
            Portal targetPortal = GetTargetPortal(portalPosition);
            Warp oldTargetWarp = targetPortal.Warp;

            // add into PortalGun object
            PortalGuns[portalPosition.PlayerIndex].AddPortal(portalPosition);

            // if the warp was made
            Warp warp = PortalGuns[portalPosition.PlayerIndex].GetWarp(portalPosition.Index);
            if (warp == null)
                return false;

            // add it to the game location
            Game1.currentLocation.warps.Add(warp);

            // remove the old warp (note the target portal's location won't change)
            RemoveWarp(targetPortal.PortalPos.LocationName, oldTargetWarp);


            // add the new target portal's warp
            AddTargetPortal(targetPortal);

            Game1.getLocationFromName(Game1.currentLocation.Name).removeTile(portalPosition.X, portalPosition.Y, "Buildings");

            // ensure the portal is dequeued
            PortalQueue.RemoveItem(portalPosition);
            return true;
        }

        private void AddTargetPortal(Portal targetPortal)
        {
            if (Game1.currentLocation.Name == targetPortal.PortalPos.LocationName)
            {
                // replace the target portal's warp
                Game1.getLocationFromName(Game1.currentLocation.Name)
                    .warps.Add(targetPortal.Warp);
            }
            else
            {
                // add it to the PortalQueue
                PortalQueue.AddItem(targetPortal.PortalPos.LocationName,
                    targetPortal.PortalPos);
            }
        }

        public Portal GetTargetPortal(PortalPosition portalPosition)
        {
            return PortalGuns[portalPosition.PlayerIndex]
                .GetTargetPortal(portalPosition.Index);
        }

        private void Multiplayer_PeerContextReceived(object sender, PeerContextReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            if (!PlayerList.Contains(e.Peer.PlayerID))
            {
                PlayerList.Add(e.Peer.PlayerID);
                int PlayerIndex = PlayerList.Count;
                this.Helper.Multiplayer.SendMessage(PlayerIndex, "PlayerIndex" + e.Peer.PlayerID, modIDs: new[] { this.ModManifest.UniqueID });
                //this.Monitor.Log("SENT retract portal json", LogLevel.Debug);
            }

            LocationUpdater<PortalPosition> TableMessage = this.PortalTable;

            this.Helper.Multiplayer.SendMessage(TableMessage, "CreateQueue" + PlayerList.IndexOf(e.Peer.PlayerID), 
                modIDs: new[] { this.ModManifest.UniqueID });
        }



        private void LoadPortalGuns(int playerNumber)
        {
            for (int i = 0; i < MAX_PORTAL_GUNS; i++)
            {
                string portalGunId = "PortalGun" + i + "Id";
                Texture2D portalGunTexture = this.Helper.Content.Load<Texture2D>($"Assets{Path.DirectorySeparatorChar}PortalGun" + (i+1) + ".png");
                PortalGunObjects[i] = CustomObjectData.newObject(portalGunId, portalGunTexture, Color.White, "Portal Gun",
                    "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Portal Gun", "388 1"));
                if (i == playerNumber)
                    PortalGunObjects[i].craftingData = new CraftingData("Portal Gun", "388 1");

                PortalGuns[i] = new PortalGun(portalGunId, i, MAX_PORTALS);

            }
        }

        private void LoadPortalTextures()
        {
            foreach (GameLocation location in GetLocations())
            {
                // Add the tilesheet.
                TileSheet tileSheet = new TileSheet(
                   id: "z_portal-spritesheet", // a unique ID for the tilesheet
                   map: location.map,
                   imageSource: TileSheetPath,
                   sheetSize: new xTile.Dimensions.Size(800, 16), // the pixel size of your tilesheet image.
                   tileSize: new xTile.Dimensions.Size(16, 16) // should always be 16x16 for maps
                );

                if (location != null && location.map != null && tileSheet != null)
                {
                    //this.Monitor.Log("adding and loading tilesheet in all maps", LogLevel.Debug);
                    location.map.AddTileSheet(tileSheet);// Multiplayer load error here
                    location.map.LoadTileSheets(Game1.mapDisplayDevice);
                }
            }

        }

        /// <summary>Get all game locations.</summary>
        public static IEnumerable<GameLocation> GetLocations()
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );
        }

        private void LoadMinePortals()
        {
            int mineLevel = Game1.player.deepestMineLevel;
            for (int i = 1; i <= mineLevel; i++)
            {
                GameLocation location = Game1.getLocationFromName($"UndergroundMine{i}");

                // Add the tilesheet.
                TileSheet tileSheet = new TileSheet(
                   id: "z_portal-spritesheet", // a unique ID for the tilesheet
                   map: location.map,
                   imageSource: TileSheetPath,
                   sheetSize: new xTile.Dimensions.Size(800, 16), // the pixel size of your tilesheet image.
                   tileSize: new xTile.Dimensions.Size(16, 16) // should always be 16x16 for maps
                );
                location.map.AddTileSheet(tileSheet);
                location.map.LoadTileSheets(Game1.mapDisplayDevice);
            }
        }

    }
}
