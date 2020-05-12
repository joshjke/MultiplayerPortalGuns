/// Stardew Valley Multiplayer Portal Guns Mod
/// by JoshJKe

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.Locations;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using xTile.Layers;
using xTile.Tiles;

using PyTK.CustomElementHandler;
using System;


namespace MultiplayerPortalGuns
{
    class ModEntry : Mod
    {
        // contains portal retract key
        private ModConfig config;

        //public const int MAX_PORTAL_GUNS = 4;
        
        public const int MAX_PORTAL_SPRITES = 50;
        public const int PORTAL_ANIM_FRAMES = 5;

        public PortalGunManager PortalGuns;

        // Assigned player index for mapping to a personal portal gun
        private int PlayerIndex;
        public List<long> PlayerList = new List<long>();

        // Used for late updating the map's tile sprites in multiplayer when a portal is made
        LocationUpdater<PortalPosition> AddedPortalSprites;
        LocationUpdater<PortalPosition> RemovedPortalSprites;

        // Used for saving/loading, updating late join farmhands, and mine levels
        LocationUpdater<PortalPosition> ActivePortalSprites;

        // Portals having their animations played
        LocationUpdater<PortalPosition> AnimatedPortals;

        // Filepaths
        private string LocationSaveFileName;
        private string TileSheetPath;


        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();

            // setting filepaths
            Directory.CreateDirectory(
                $"{this.Helper.DirectoryPath}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}");
            TileSheetPath = this.Helper.Content.GetActualAssetKey($"Assets{Path.DirectorySeparatorChar}PortalsAnimated3.png", ContentSource.ModFolder);

            // SMAPI events used
            helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.AfterLoad;
            helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;
            helper.Events.Multiplayer.PeerContextReceived += this.Multiplayer_PeerContextReceived;
            helper.Events.GameLoop.UpdateTicked += this.GameLoop_UpdateTicked;
            helper.Events.Player.Warped += this.Warped;
            helper.Events.GameLoop.Saved += this.GameLoop_Saved;
        }

        /// <summary>
        /// Animates portal sprites every frame
        /// </summary>
        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            AnimatePortals();
        }
        /// <summary>
        /// Routes input based on the portal gun's buttons
        /// </summary>
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

        /******************************************************************************
         * Portal Addition and Placement
         *****************************************************************************/
         /// <summary>
         /// After player has used the tool/action button, check all conditions for
         /// placing a portal
         /// </summary>
         /// <param name="e"></param>
        private void PortalSpawner(ButtonPressedEventArgs e)
        {
            if (Game1.menuUp || Game1.activeClickableMenu != null 
                || Game1.isFestival() || Game1.player.ActiveObject == null 
                || !Game1.player.CurrentItem.DisplayName.Contains("Portal Gun"))
                return;

            int portalIndex = 0;
            if (e.Button.IsUseToolButton())
                portalIndex = 0;

            else if (e.Button.IsActionButton())
                portalIndex = 1;

            Game1.switchToolAnimation();

            PortalPosition portalPos = PortalGuns.NewPortalPosition(PlayerIndex, portalIndex);
            if (portalPos == null)
            {
                this.Monitor.Log("portalPosition is null", LogLevel.Debug);
                return;
            }

            RemoveWarpAndSprite(PortalGuns.GetPortalPosition(PlayerIndex, portalIndex));
            portalPos.AnimationFrame = 0;
            AddPortal(portalPos);

            Game1.currentLocation.playSoundAt("debuffSpell", Game1.player.getTileLocation());
        }

        /// <summary>
        /// Places a portal at portalPosition
        /// Prerequisites: the player has shot a portal into a valid location and
        ///  tile position
        /// </summary>
        private void AddPortal(PortalPosition portalPosition)
        {
            // remove the target's warp
            PortalPosition targetPortalPos = PortalGuns.GetTargetPortal(portalPosition).PortalPos;
            RemoveWarp(targetPortalPos);

            // assign the portal to the gun to handle the targeting warp structure
            //   but not any of the map placement
            PortalGuns.AddPortal(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "UpdateAddPortal", modIDs: new[] { this.ModManifest.UniqueID });

            // add the new target portal's warp and sprites
            AddWarp(targetPortalPos);

            RemoveWarpAndSprite(portalPosition);

            // add the placed portal's warps and sprites
            AddWarpAndSprite(portalPosition);
        }

        /******************************************************************************
         * Warp and Sprite Logic
         *****************************************************************************/
        private void AddWarpsAndSprites(List<PortalPosition> portalPositions)
        {
            foreach (PortalPosition portalPosition in portalPositions)
                AddWarpAndSprite(portalPosition);
        }
        /// <summary>
        /// Adds warp, sprite, and messsages other players to add the sprite
        /// </summary>
        private void AddWarpAndSprite(PortalPosition portalPosition)
        {
            AddWarp(portalPosition);
            AddPortalSprite(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "AddPortalSprite", modIDs: new[] { this.ModManifest.UniqueID });
        }
        /// <summary>
        /// Removes warp, sprite, and messsages other players to remove the sprite
        /// </summary>
        private void RemoveWarpAndSprite(PortalPosition portalPosition)
        {
            RemoveWarp(portalPosition);
            RemovePortalSprite(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "RemovePortalSprite", modIDs: new[] { this.ModManifest.UniqueID });
        }

       

        /******************************************************************************
         * Warp logic
         *****************************************************************************/
        /// <summary>
        /// The main player adds the warp to the world
        /// </summary>
        private bool AddWarp(PortalPosition portalPosition)
        {
            if (portalPosition.LocationName == "" || portalPosition.LocationName == null)
                return false;
            // Warps are global, so only have host handle them to avoid duplicates and ghosts
            if (!Context.IsMainPlayer)
            {
                this.Helper.Multiplayer.SendMessage(portalPosition, "AddWarp", modIDs: new[] { this.ModManifest.UniqueID });
                return true;
            }
            // if a warp was created, add it
            Warp warp = PortalGuns.GetWarp(portalPosition);
            if (warp == null)
                return false;

            // add it to the game location
            Game1.getLocationFromName(portalPosition.LocationName).warps.Add(warp);
            return true;
        }
        /// <summary>
        /// The main player removes the warp at the portalPosition
        /// </summary>
        private bool RemoveWarp(PortalPosition portalPosition)
        {
            // Warps are global, so only have host handle them to avoid duplicates and ghosts
            if (!Context.IsMainPlayer)
            {
                this.Helper.Multiplayer.SendMessage(portalPosition, "RemoveWarp", modIDs: new[] { this.ModManifest.UniqueID });
                return false;
            }
            string locationName = portalPosition.LocationName;
            if (locationName == "" || locationName == null)
                return false;
            GameLocation location = Game1.getLocationFromName(portalPosition.LocationName);

            // remove the warp from the location
            Warp warp = PortalGuns.GetWarp(portalPosition);
            if (warp != null)
                location.warps.Remove(PortalGuns.GetWarp(portalPosition));

            return true;
        }

        /******************************************************************************
         * Portal Sprite Additions
         *****************************************************************************/

        private bool AddPortalSprite(PortalPosition portalPosition)
        {
            AddedPortalSprites.RemoveItem(portalPosition);

            ActivePortalSprites.RemoveItem(portalPosition);
            ActivePortalSprites.AddItem(portalPosition.LocationName, portalPosition);

            // sometimes the current location is null if loading is taking a while
            if (Game1.currentLocation != null 
                && portalPosition.LocationName != Game1.currentLocation.NameOrUniqueName)
            {
                portalPosition.AnimationFrame = PORTAL_ANIM_FRAMES - 1;
                AddedPortalSprites.AddItem(portalPosition.LocationName, portalPosition);
                return false;
            }
            // animation frame 0 indicates the animation should be played
            else if (portalPosition.AnimationFrame == 0)
            {
                portalPosition.AnimationFrame = 0;
                AddedPortalSprites.RemoveItem(portalPosition);
                AnimatedPortals.AddItem(portalPosition.LocationName, portalPosition);
                return true;
            }
            // place at full portal size
            else
            {
                portalPosition.AnimationFrame = PORTAL_ANIM_FRAMES - 1;
                PlacePortalSprite(portalPosition);
                AddedPortalSprites.RemoveItem(portalPosition);
            }
            return true;
        }
        private void PlacePortalSprite(PortalPosition portalPosition)
        {
            // remove the existing sprite from the map
            try { Game1.getLocationFromName(portalPosition.LocationName).removeTile(
                portalPosition.X, portalPosition.Y, "Buildings"); } catch (IndexOutOfRangeException) { }

            // Add the sprite to the map
            Layer layer = Game1.getLocationFromName(portalPosition.LocationName).map.GetLayer("Buildings");
            TileSheet tileSheet = Game1.getLocationFromName(portalPosition.LocationName).map.GetTileSheet("z_portal-spritesheet");

            layer.Tiles[portalPosition.X, portalPosition.Y] = new StaticTile(
                layer, tileSheet, BlendMode.Additive, GetPortalSpriteIndex(portalPosition));
        }

        /******************************************************************************
         * Portal Animations
         *****************************************************************************/
        private void AnimatePortals() 
        {
            if (AnimatedPortals == null)
                return;
            List<PortalPosition> portaPositions = AnimatedPortals.GetAllItems();
            if (portaPositions.Count < 1)
                return;

            foreach (PortalPosition portalPosition in portaPositions)
            {
                if (Game1.currentLocation != null
                    && Game1.currentLocation.NameOrUniqueName == portalPosition.LocationName
                    && portalPosition.AnimationFrame < PORTAL_ANIM_FRAMES)
                {
                    PlacePortalSprite(portalPosition);
                    ++portalPosition.AnimationFrame;
                }
                else
                {
                    AnimatedPortals.RemoveItem(portalPosition);
                    portalPosition.AnimationFrame = PORTAL_ANIM_FRAMES - 1;
                    AddedPortalSprites.AddItem(portalPosition.LocationName, portalPosition);
                }
            }
        }
        private int GetPortalSpriteIndex(PortalPosition portalPosition)
        {
            int portalSpriteIndex = 
                portalPosition.PlayerIndex * 10 // player's portals
                + portalPosition.Index * 5 // portal color offset
                + portalPosition.AnimationFrame; // animation frame
            return portalSpriteIndex % MAX_PORTAL_SPRITES; // for overflow
        }
        private bool AddPortalSprites(List<PortalPosition> portalPositions)
        {
            if (portalPositions == null)
                return false;
            bool allRemoved = true;
            foreach (PortalPosition portalPosition in portalPositions.ToList())
            {
                if (!AddPortalSprite(portalPosition))
                    allRemoved = false;
            }
            return allRemoved;
        }

        /******************************************************************************
         * Portal Sprite Removal
         *****************************************************************************/

        public bool RemovePortalSprite(PortalPosition portalPosition)
        {
            return RemovePortalSprite(portalPosition.LocationName, portalPosition);
        }

        public bool RemovePortalSprite(string locationName, PortalPosition portalPosition)
        {
            // if it has already been added previously (same indices, location, and coords) remove from queue
            AddedPortalSprites.RemoveItem(portalPosition);
            AnimatedPortals.RemoveItem(portalPosition);
            ActivePortalSprites.RemoveItem(portalPosition);

            if (Game1.currentLocation != null && locationName != Game1.currentLocation.NameOrUniqueName)
            {
                RemovedPortalSprites.AddItem(locationName, new PortalPosition(portalPosition));
                return false;
            }
            else // TODO if the OldTiles need to be saved, this needs to be edited
            {
                try
                {
                    Game1.getLocationFromName(locationName).removeTile(portalPosition.X, portalPosition.Y, "Buildings");
                } 
                catch (IndexOutOfRangeException) { }

                RemovedPortalSprites.RemoveItem(portalPosition);
            }
            return true;
        }

        private bool RemovePortalSprites(string locationName, List<PortalPosition> portalPositions)
        {
            if (portalPositions == null)
                return false;
            bool allRemoved = true;
            foreach (PortalPosition portalPosition in portalPositions.ToList())
            {
                if (!RemovePortalSprite(locationName, portalPosition))
                    allRemoved = false;
            }
            return allRemoved;
        }

        /******************************************************************************
         * Portal Resetting. Mostly used for entering mine locations
         *****************************************************************************/
        private void ResetWarpsAndSprites(List<PortalPosition> portalPositions)
        {
            if (portalPositions == null)
                return;
            foreach (PortalPosition portalPosition in portalPositions)
            {
                ResetWarpAndSprite(portalPosition);
            }
        }

        private void ResetWarpAndSprite(PortalPosition portalPosition)
        {
            RemoveWarp(portalPosition);
            AddWarp(portalPosition);
            RemovePortalSprite(portalPosition);
            AddPortalSprite(portalPosition);
        }


        /******************************************************************************
         * Portal Removal
         *****************************************************************************/
        private void RetractPortals(int portalGunIndex)
        {
            RemovePortals(portalGunIndex);
            /*for (int i = 0; i < MAX_PORTALS; i++)
            {
                RemoveWarpAndSprite(PortalGuns[portalGunIndex].GetPortal(i).PortalPos);
            }*/

            PortalGuns.RemovePortals(portalGunIndex);
            // Notify players
            this.Helper.Multiplayer.SendMessage(portalGunIndex, "UpdateRemovePortals", modIDs: new[] { this.ModManifest.UniqueID });

            // Bells and Whistles
            Game1.switchToolAnimation();
            //Game1.currentLocation.playSoundAt("serpentDie", Game1.player.getTileLocation());

        }

        private bool RemovePortals(int gunIndex)
        {
            //Portal[] portals = PortalGuns[gunIndex].GetPortals();
            //List<PortalPosition>() portalsToRemove()
            foreach (Portal removablePortal in PortalGuns.GetPortals(gunIndex))
            {
                RemoveWarpAndSprite(removablePortal.PortalPos);
            }
            return true;
        }



        /******************************************************************************
         * Player enters a new location
         *****************************************************************************/
        private void Warped(object sender, WarpedEventArgs e)
        {
            string locationName = e.NewLocation.Name;
 
            LoadTileSheet(e.NewLocation);


            RemovePortalSprites(locationName, RemovedPortalSprites.GetItemsInLocation(locationName));
            AddPortalSprites(AddedPortalSprites.GetItemsInLocation(locationName));

            if (locationName.Contains("UndergroundMine")) 
            {
                // reload warps and sprites in the location
                List<PortalPosition> minePostions = ActivePortalSprites.GetItemsInLocation(locationName);
                if (minePostions != null)
                    ResetWarpsAndSprites(minePostions.ToList());
            }
        }


        /******************************************************************************
         * Multiplayer Routing
         *****************************************************************************/
        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            // Message from host to targeted Peer to assign PlayerIndex
            if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "PlayerIndex")
                this.PlayerIndex = e.ReadAs<int>();


            // Warps are global, so they are to be handled by the host only
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "AddWarp")
            {
                if (Context.IsMainPlayer)
                    AddWarp(e.ReadAs<PortalPosition>());
            }

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "RemoveWarp")
            {
                if (Context.IsMainPlayer)
                    RemoveWarp(e.ReadAs<PortalPosition>());
            }

            // Portal Sprites are local so all players need to handle the other players'
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "AddPortalSprite")
            {
                PortalPosition portalPosition = e.ReadAs<PortalPosition>();
                if (Game1.currentLocation != null 
                    && Game1.currentLocation.NameOrUniqueName == portalPosition.LocationName)
                {
                    // animate portal 
                    portalPosition.AnimationFrame = 0;
                }
                else
                {
                    portalPosition.AnimationFrame = PORTAL_ANIM_FRAMES - 1;
                }
                AddPortalSprite(portalPosition);
            }

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "RemovePortalSprite")
                RemovePortalSprite(e.ReadAs<PortalPosition>());

            // keeping the logic for all the portal guns the same
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "UpdateAddPortal")
                PortalGuns.UpdateAddPortal(e.ReadAs<PortalPosition>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "UpdateRemovePortals")
                PortalGuns.UpdateRemovePortals(e.ReadAs<int>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "GetActivePortalSprites")
                SendActivePortalSprites(e.FromPlayerID);

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "SendActivePortalSprites")
                SetActivePortalSprites(e.ReadAs<List<PortalPosition>>());

        }

        private void SendActivePortalSprites(long playerId)
        {
            if (!Context.IsMainPlayer)
                return;
            this.Helper.Multiplayer.SendMessage(this.ActivePortalSprites.GetAllItems(),
                "SendActivePortalSprites",
                modIDs: new[] { this.ModManifest.UniqueID }, new long[] { playerId });
        }

        private void SetActivePortalSprites(List<PortalPosition> portalSpritesToAdd)
        {
            AddedPortalSprites = new LocationUpdater<PortalPosition>();

            RemovedPortalSprites = new LocationUpdater<PortalPosition>();

            //LoadPortalTextures();

            foreach (PortalPosition portalPosition in portalSpritesToAdd)
            {
                AddPortalSprite(new PortalPosition(portalPosition));
                PortalGuns.UpdateAddPortal(portalPosition);
            }

        }

        
        private void Multiplayer_PeerContextReceived(object sender, PeerContextReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            if (!PlayerList.Contains(e.Peer.PlayerID))
            {
                PlayerList.Add(e.Peer.PlayerID);
                int playerIndexForNewPlayer = PlayerList.Count;
                this.Helper.Multiplayer.SendMessage(playerIndexForNewPlayer, "PlayerIndex", modIDs: new[] { this.ModManifest.UniqueID }, new long[] { e.Peer.PlayerID });
            }
        }

        /******************************************************************************
         * Loading Textures, Portal Guns, and Locations
         *****************************************************************************/
        private void AfterLoad(object sender, SaveLoadedEventArgs e)
        {
            AddedPortalSprites = new LocationUpdater<PortalPosition>();
            RemovedPortalSprites = new LocationUpdater<PortalPosition>();

            ActivePortalSprites = new LocationUpdater<PortalPosition>();
            AnimatedPortals = new LocationUpdater<PortalPosition>();
            PortalGuns = new PortalGunManager(this.Helper.Content);

            this.Monitor.Log("hitting after load, SaveLoadedEventArgs");

            LoadPortalTextures();

            if (Context.IsMainPlayer)
            {
                PlayerIndex = 0;
                LoadMinePortals();
                LoadSavedPortals();
            }
            else
            {
               this.Helper.Multiplayer.SendMessage(Game1.player.UniqueMultiplayerID, "GetActivePortalSprites",
               modIDs: new[] { this.ModManifest.UniqueID });
            }
        }

        private void LoadSavedPortals()
        {
            // Reads or Creates a Portal Gun save data file
            LocationSaveFileName =
                $"Data{Path.DirectorySeparatorChar}{Constants.SaveFolderName}.json";
            // path/Data/SaveFolderName.json
                
            List<PortalPosition> portalPositions = Helper.Data.ReadJsonFile<List<PortalPosition>>(LocationSaveFileName);

            if (portalPositions == null)
                Helper.Data.WriteJsonFile(LocationSaveFileName, ActivePortalSprites.GetAllItems());
            else
            {
                PortalGuns.ReloadPortals(portalPositions);
                AddWarpsAndSprites(portalPositions);
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

        private void LoadPortalTextures()
        {
            foreach (GameLocation location in GetLocations())
                LoadTileSheet(location);
        }

        private void LoadMinePortals()
        {
            int mineLevel = Game1.player.deepestMineLevel;
            for (int i = 1; i <= mineLevel; i++)
            {
                GameLocation location = Game1.getLocationFromName($"UndergroundMine{i}");
                LoadTileSheet(location);
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

        private void GameLoop_Saved(object sender, SavedEventArgs e)
        {
            if (Context.IsMainPlayer && ActivePortalSprites != null)
                Helper.Data.WriteJsonFile(LocationSaveFileName, ActivePortalSprites.GetAllItems());
        }

    }
}
