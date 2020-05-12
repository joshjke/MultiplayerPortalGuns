/// Stardew Valley Multiplayer Portal Guns Mod
/// by Josh Kennedy

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
using xTile.Dimensions;
using System;
using Antlr.Runtime.Tree;

using SimpleSoundManager;
using SimpleSoundManager.Framework;
using Microsoft.Xna.Framework.Audio;

namespace MultiplayerPortalGuns
{
    class ModEntry : Mod
    {
        // For portal retract key
        private ModConfig config;

        public const int MAX_PORTAL_GUNS = 4;
        public const int MAX_PORTALS = 2;
        public const int MAX_PORTAL_SPRITES = 90;
        public const int PORTAL_ANIM_FRAMES = 5;

        private int PlayerIndex;
        public List<long> PlayerList = new List<long>();

        PortalGun[] PortalGuns = new PortalGun[MAX_PORTAL_GUNS];
        CustomObjectData[] PortalGunObjects = new CustomObjectData[MAX_PORTAL_GUNS];

        // Used for late updating the map's tile sprites in multiplayer when a portal is made
        LocationUpdater<PortalPosition> AddedPortalSprites;
        LocationUpdater<PortalPosition> RemovedPortalSprites;

        // Used for updating the portal tables of farmhands when they join
        LocationUpdater<PortalPosition> ActivePortalSprites;
        
        // Portals to be animated if they are placed in the location the player
        // is currently in
        LocationUpdater<PortalPosition> AnimatedPortals;

        // Filepaths
        private string LocationSaveFileName;
        private string TileSheetPath;
        private string SoundsPath;

        SoundManager sound;


        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();

            Directory.CreateDirectory(
                $"{this.Helper.DirectoryPath}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}");

            TileSheetPath = this.Helper.Content.GetActualAssetKey($"Assets{Path.DirectorySeparatorChar}PortalsAnimated3.png", ContentSource.ModFolder);

            SoundsPath = $"{this.Helper.DirectoryPath}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}Sounds";


            helper.Events.GameLoop.SaveLoaded += this.AfterLoad;
            helper.Events.Player.Warped += this.Warped;
            helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
            helper.Events.Multiplayer.PeerContextReceived += Multiplayer_PeerContextReceived;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.GameLoop.Saved += GameLoop_Saved;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;

            sound = new SoundManager();
            sound.loadWavFile(this.Helper, "retractPortals", SoundsPath);

           
            XACTSound aCTSound = new XACTSound(())
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            AnimatePortals();

            

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

            else if (e.Button.ToString().ToLower() == "p" && Context.IsMainPlayer)
                Helper.Data.WriteJsonFile(LocationSaveFileName, ActivePortalSprites.GetAllItems());

        }

        /******************************************************************************
         * Portal Addition and Placement
         *****************************************************************************/
        private void PortalSpawner(ButtonPressedEventArgs e)
        {
            if (Game1.menuUp || Game1.activeClickableMenu != null || Game1.isFestival()
                || Game1.player.ActiveObject == null || 
                !Game1.player.CurrentItem.DisplayName.Contains("Portal Gun"))
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
                this.Monitor.Log("portalPosition is null", LogLevel.Debug);
                //Game1.currentLocation.playSoundAt("debuffSpell", Game1.player.getTileLocation());
                return;
            }

            Portal portal = new Portal(portalPos);

            //SpawnPortals(index);

            RemoveWarpAndSprite(PortalGuns[PlayerIndex].GetPortal(index).PortalPos);
            portalPos.AnimationFrame = 0;
            AddPortal(portalPos);

            // Tile animation and stuff

            // TODO animate the portal spawning
            //PortalAnimationFrame[i] + i * 5

            // Bells and whistles

            // Portal Animation here
            Game1.currentLocation.playSoundAt("debuffSpell", Game1.player.getTileLocation());
        }

        //private void OtherPlayerAddedPortal()

        private void AddPortals(List<PortalPosition> PortalsToAdd)
        {
            if (PortalsToAdd == null)
                return;
            foreach (PortalPosition portal in PortalsToAdd.ToList())
                AddPortal(portal);
        }
        /// <summary>
        /// Places a portal at portalPosition
        /// Prerequisites: checking for a valid placement position
        /// </summary>
        /// <param name="portalPosition">The portal to be placed</param>
        /// <returns></returns>
        private bool AddPortal(PortalPosition portalPosition)
        {
            // remove the target's warp
            PortalPosition targetPortalPos = GetTargetPortal(portalPosition).PortalPos;
            RemoveWarp(targetPortalPos);

            // assign the portal to the gun to handle the targeting warp structure
            //   but not any of the map placement
            PortalGuns[portalPosition.PlayerIndex].AddPortal(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "UpdateAddPortal", modIDs: new[] { this.ModManifest.UniqueID });

            // add the new target portal's warp and sprites
            AddWarp(targetPortalPos);

            RemoveWarpAndSprite(portalPosition);
            // add the placed portal's warps and sprites
            Portal portal = PortalGuns[portalPosition.PlayerIndex].GetPortal(portalPosition.Index);
            AddWarpAndSprite(portal.PortalPos);

            return true;
        }

        private void AddWarpsAndSprites(List<PortalPosition> portalPositions)
        {
            foreach (PortalPosition portalPosition in portalPositions)
                AddWarpAndSprite(portalPosition);
        }

        private void AddWarpAndSprite(PortalPosition portalPosition)
        {
            AddWarp(portalPosition);
            AddPortalSprite(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "AddPortalSprite", modIDs: new[] { this.ModManifest.UniqueID });
        }

        private void RemoveWarpAndSprite(PortalPosition portalPosition)
        {
            RemoveWarp(portalPosition);
            RemovePortalSprite(portalPosition);
            this.Helper.Multiplayer.SendMessage(portalPosition, "RemovePortalSprite", modIDs: new[] { this.ModManifest.UniqueID });
        }

        public Portal GetTargetPortal(PortalPosition portalPosition)
        {
            return PortalGuns[portalPosition.PlayerIndex]
                .GetTargetPortal(portalPosition.Index);
        }

        private bool ReloadPortals(List<PortalPosition> portalPositions)
        {
            foreach (PortalPosition portalPosition in portalPositions)
            {
                if (portalPosition != null)
                    UpdateAddPortal(portalPosition);
            }
            AddWarpsAndSprites(portalPositions);
            return true;
        }

        private bool UpdateAddPortal(PortalPosition portalPosition)
        {
            return PortalGuns[portalPosition.PlayerIndex].AddPortal(portalPosition);
        }

        private void UpdateRemovePortals(int portalGunIndex)
        {
            PortalGuns[portalGunIndex].RemovePortals();
        }

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
            Warp warp = GetWarp(portalPosition);
            if (warp == null)
                return false;

            // add it to the game location
            Game1.getLocationFromName(portalPosition.LocationName).warps.Add(warp);
            return true;
        }

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
            Warp warp = GetWarp(portalPosition);
            if (warp != null)
                location.warps.Remove(GetWarp(portalPosition));

            return true;
        }

        private Warp GetWarp(PortalPosition portalPosition)
        {
            return PortalGuns[portalPosition.PlayerIndex].GetWarp(portalPosition.Index);
        }

        private bool RemoveWarps(List<PortalPosition> portalPositions)
        {
            if (portalPositions == null)
                return false;
            bool allRemoved = true;
            foreach (PortalPosition portalPosition in portalPositions.ToList())
            {
                if (!RemoveWarp(portalPosition))
                    allRemoved = false;
            }
            return allRemoved;
        }


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
         * Portal Removal
         *****************************************************************************/
        private void RetractPortals(int portalGunIndex)
        {
            RemovePortals(portalGunIndex);
            /*for (int i = 0; i < MAX_PORTALS; i++)
            {
                RemoveWarpAndSprite(PortalGuns[portalGunIndex].GetPortal(i).PortalPos);
            }*/

            PortalGuns[portalGunIndex].RemovePortals();
            // Notify players
            this.Helper.Multiplayer.SendMessage(portalGunIndex, "UpdateRemovePortals", modIDs: new[] { this.ModManifest.UniqueID });

            // Bells and Whistles
            Game1.switchToolAnimation();
            //Game1.currentLocation.playSoundAt("serpentDie", Game1.player.getTileLocation());
            //sound.playSound("retractPortals");
            if (sound.sounds.TryGetValue("retractPortals", out Sound retractSound))
                retractSound.play();
            
            //sound.


        }

        private bool RemovePortals(int gunIndex)
        {
            //Portal[] portals = PortalGuns[gunIndex].GetPortals();
            //List<PortalPosition>() portalsToRemove()
            foreach (Portal removablePortal in PortalGuns[gunIndex].GetPortals().ToList())
            {
                /*if (RemovePortal(removablePortal) == false)
                    return false;*/
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
         * Multiplayer Routing
         *****************************************************************************/
        private void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            /* // Message from Host to targeted Peer to give PortalTable to them
             if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "SetPortalTable" + this.PlayerIndex)
                 SetPortalTable(e.ReadAs<LocationUpdater<PortalPosition>>());*/
            long holdthisThanks = Game1.player.UniqueMultiplayerID;
            // Message from host to targeted Peer to assign PlayerIndex
            if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "PlayerIndex")
                LoadPortalGuns(e.ReadAs<int>());


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
                UpdateAddPortal(e.ReadAs<PortalPosition>());

            else if (e.FromModID == "JoshJKe.PortalGun" && e.Type == "UpdateRemovePortals")
                UpdateRemovePortals(e.ReadAs<int>());


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
                UpdateAddPortal(portalPosition);
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

            this.Monitor.Log("hitting after load, SaveLoadedEventArgs");

            if (Context.IsMainPlayer)
            {
                PlayerIndex = 0;
                //PlayerList.Add(Game1.player.uniqueMultiplayerID);
                LoadPortalGuns(PlayerIndex);

                LoadPortalTextures();
                LoadMinePortals();

                LoadSavedPortals();
            }
            else
            {
               LoadPortalTextures();
               this.Helper.Multiplayer.SendMessage(Game1.player.UniqueMultiplayerID, "GetActivePortalSprites",
               modIDs: new[] { this.ModManifest.UniqueID });
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

        private void LoadPortalGuns(int playerNumber)
        {
            PlayerIndex = playerNumber;
            for (int i = 0; i < MAX_PORTAL_GUNS; i++)
            {
                string portalGunId = "PortalGun" + i + "Id";
                Texture2D portalGunTexture = this.Helper.Content.Load<Texture2D>(
                    $"Assets{Path.DirectorySeparatorChar}PortalGun" + (i + 1) + ".png");

                PortalGunObjects[i] = CustomObjectData.newObject(portalGunId, portalGunTexture,
                    Color.White, "Portal Gun " + i, "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "",
                    craftingData: new CraftingData("Portal Gun " + i, "388 1"));

                /*if (i == playerNumber)
                    PortalGunObjects[i].craftingData = new CraftingData("Portal Gun " + i, "388 1");*/

                PortalGuns[i] = new PortalGun(portalGunId, i, MAX_PORTALS);
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
                ReloadPortals(portalPositions);

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

        private void GameLoop_Saved(object sender, SavedEventArgs e)
        {
            // error here
            if (Context.IsMainPlayer)
                Helper.Data.WriteJsonFile(LocationSaveFileName, ActivePortalSprites.GetAllItems());
        }

    }
}
