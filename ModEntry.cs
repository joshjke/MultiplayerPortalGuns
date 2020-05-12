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
using xTile.Dimensions;

namespace MultiplayerPortalGuns
{
    class ModEntry : Mod
    {
        // contains portal retract key
        private ModConfig config;

        public PortalGunManager PortalGuns;
        public PortalSpriteManager PortalSprites;

        // Assigned player index for mapping to a personal portal gun
        private int PlayerIndex;
        public List<long> PlayerList = new List<long>();

        // Used for late updating the map's tile sprites in multiplayer when a portal is made
        

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

            // SMAPI events
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
            if (PortalSprites != null)
                PortalSprites.AnimatePortals();
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

            // determine which portal was shot
            int portalIndex = 0;
            if (e.Button.IsUseToolButton())
                portalIndex = 0;

            else if (e.Button.IsActionButton())
                portalIndex = 1;

            // animate player
            Game1.switchToolAnimation();

            // create a new portal at the cursor position with the given index
            PortalPosition portalPos = PortalGuns.NewPortalPosition(PlayerIndex, portalIndex);
            // portal could not be created at the given position
            if (portalPos == null)
            {
                this.Monitor.Log("portalPosition is null", LogLevel.Debug);
                return;
            }
            // remove the original portal if any
            RemoveWarpAndSprite(PortalGuns.GetPortalPosition(PlayerIndex, portalIndex));
            // set the portal to animate opening when placed
            portalPos.AnimationFrame = 0;
            // add the portal to the world
            AddPortal(portalPos);
            // play the portal shooting sound
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

            // remove the original portal
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
            PortalSprites.AddPortalSprite(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "AddPortalSprite", modIDs: new[] { this.ModManifest.UniqueID });
        }
        /// <summary>
        /// Removes warp, sprite, and messsages other players to remove the sprite
        /// </summary>
        private void RemoveWarpAndSprite(PortalPosition portalPosition)
        {
            RemoveWarp(portalPosition);
            PortalSprites.RemovePortalSprite(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "RemovePortalSprite", modIDs: new[] { this.ModManifest.UniqueID });
        }

        private bool AddWarp(PortalPosition portalPosition)
        {
            return WarpManager.AddWarp(portalPosition, Context.IsMainPlayer, PortalGuns, this.Helper.Multiplayer, this.ModManifest.UniqueID); 
        }
        /// <summary>
        /// The main player removes the warp at the portalPosition
        /// </summary>
        private bool RemoveWarp(PortalPosition portalPosition)
        {
            return WarpManager.RemoveWarp(portalPosition, Context.IsMainPlayer, PortalGuns, this.Helper.Multiplayer, this.ModManifest.UniqueID);
        }

        /******************************************************************************
         * Portal Resetting. Mostly used for entering mine locations
         *****************************************************************************/
        private void ResetWarpsAndSprites(List<PortalPosition> portalPositions)
        {
            if (portalPositions == null)
                return;
            foreach (PortalPosition portalPosition in portalPositions)
                ResetWarpAndSprite(portalPosition);
        }

        private void ResetWarpAndSprite(PortalPosition portalPosition)
        {
            RemoveWarp(portalPosition);
            AddWarp(portalPosition);
            PortalSprites.RemovePortalSprite(portalPosition);
            PortalSprites.AddPortalSprite(portalPosition);
        }


        /******************************************************************************
         * Portal Removal
         *****************************************************************************/
        private void RetractPortals(int portalGunIndex)
        {
            RemovePortals(portalGunIndex);

            PortalGuns.RemovePortals(portalGunIndex);
            // Notify players
            this.Helper.Multiplayer.SendMessage(portalGunIndex, "UpdateRemovePortals", modIDs: new[] { this.ModManifest.UniqueID });

            Game1.switchToolAnimation();
        }

        private void RemovePortals(int gunIndex)
        {
            foreach (Portal removablePortal in PortalGuns.GetPortals(gunIndex))
                RemoveWarpAndSprite(removablePortal.PortalPos);
        }



        /******************************************************************************
         * Player enters a new location
         *****************************************************************************/
        private void Warped(object sender, WarpedEventArgs e)
        {
            string locationName = e.NewLocation.Name;
 
            LoadTileSheet(e.NewLocation);


            PortalSprites.RemovePortalSprites(locationName);
            PortalSprites.AddPortalSprites(locationName);

            if (locationName.Contains("UndergroundMine")) 
            {
                // reload warps and sprites in the location
                List<PortalPosition> minePostions = PortalSprites.GetActiveSprites(locationName);
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
                PortalSprites.AddPortalSprite(e.ReadAs<PortalPosition>());
           

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "RemovePortalSprite")
                PortalSprites.RemovePortalSprite(e.ReadAs<PortalPosition>());

            // keeping the logic for all the portal guns the same
            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "UpdateAddPortal")
                PortalGuns.AddPortal(e.ReadAs<PortalPosition>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "UpdateRemovePortals")
                PortalGuns.RemovePortals(e.ReadAs<int>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "GetActivePortalSprites")
                SendActivePortalSprites(e.FromPlayerID);

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "SendActivePortalSprites")
                SetActivePortalSprites(e.ReadAs<List<PortalPosition>>());

        }

        /// <summary>
        /// The hosts sends a newly connected player their active portalsprites
        /// </summary>
        private void SendActivePortalSprites(long playerId)
        {
            if (!Context.IsMainPlayer)
                return;
            this.Helper.Multiplayer.SendMessage(PortalSprites.GetAllActiveSprites(),
                "SendActivePortalSprites",
                modIDs: new[] { this.ModManifest.UniqueID }, new long[] { playerId });
        }
        /// <summary>
        /// A newly connected player sets their active portals from the host
        /// No need to set warps, since the host already handles those
        /// </summary>
        private void SetActivePortalSprites(List<PortalPosition> portalSpritesToAdd)
        {
            PortalSprites.AddPortalSprites(portalSpritesToAdd);
            PortalGuns.AddPortals(portalSpritesToAdd);
        }

        /// <summary>
        /// Newly connected player is assigned an id to use with their portal gun
        /// </summary>
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
            PortalSprites = new PortalSpriteManager();
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
                Helper.Data.WriteJsonFile(LocationSaveFileName, PortalSprites.GetAllActiveSprites());
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
                location.map.AddTileSheet(tileSheet);
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
            if (Context.IsMainPlayer)
                Helper.Data.WriteJsonFile(LocationSaveFileName, PortalSprites.GetAllActiveSprites());
        }

    }
}
