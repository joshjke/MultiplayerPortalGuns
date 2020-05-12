using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xTile.Layers;
using xTile.Tiles;

namespace MultiplayerPortalGuns
{
    /// <summary>
    /// Handles the placement, removal, and animations for portal sprites
    /// </summary>
    class PortalSpriteManager
    {
        public const int MAX_PORTAL_SPRITES = 50;
        public const int PORTAL_ANIM_FRAMES = 5;

        LocationUpdater<PortalPosition> AddedPortalSprites;
        LocationUpdater<PortalPosition> RemovedPortalSprites;

        // Used for saving/loading, updating late join farmhands, and mine levels
        LocationUpdater<PortalPosition> ActivePortalSprites;

        // Portals having their animations played
        LocationUpdater<PortalPosition> AnimatedPortals;


        public PortalSpriteManager()
        {
            AddedPortalSprites = new LocationUpdater<PortalPosition>();
            RemovedPortalSprites = new LocationUpdater<PortalPosition>();
            ActivePortalSprites = new LocationUpdater<PortalPosition>();
            AnimatedPortals = new LocationUpdater<PortalPosition>();
        }


        public List<PortalPosition> GetActiveSprites(string locationName)
        {
            return ActivePortalSprites.GetItemsInLocation(locationName);
        }
        public List<PortalPosition> GetAllActiveSprites()
        {
            return ActivePortalSprites.GetAllItems();
        }

        /******************************************************************************
        * Portal Sprite Additions
        *****************************************************************************/

        public bool AddPortalSprite(PortalPosition portalPosition)
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
            try
            {
                Game1.getLocationFromName(portalPosition.LocationName).removeTile(
              portalPosition.X, portalPosition.Y, "Buildings");
            }
            catch (IndexOutOfRangeException) { }

            // Add the sprite to the map
            Layer layer = Game1.getLocationFromName(portalPosition.LocationName).map.GetLayer("Buildings");
            TileSheet tileSheet = Game1.getLocationFromName(portalPosition.LocationName).map.GetTileSheet("z_portal-spritesheet");

            layer.Tiles[portalPosition.X, portalPosition.Y] = new StaticTile(
                layer, tileSheet, BlendMode.Additive, GetPortalSpriteIndex(portalPosition));
        }

        /******************************************************************************
         * Portal Animations
         *****************************************************************************/
        public void AnimatePortals()
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

        public void AddPortalSprites(List<PortalPosition> portalPositions)
        {
            foreach (PortalPosition portalPosition in portalPositions)
            {
                AddPortalSprite(new PortalPosition(portalPosition));
            }
        }
        public void AddPortalSprites(string locationName)
        {
            List<PortalPosition> portalPositions = AddedPortalSprites.GetItemsInLocation(locationName);

            if (portalPositions == null)
                return;

            foreach (PortalPosition portalPosition in portalPositions.ToList())
                AddPortalSprite(portalPosition);

            return;
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
            else
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

        public void RemovePortalSprites(string locationName)
        {
            List<PortalPosition> portalPositions = RemovedPortalSprites.GetItemsInLocation(locationName);

            if (portalPositions == null)
                return;

            foreach (PortalPosition portalPosition in portalPositions.ToList())
                RemovePortalSprite(locationName, portalPosition);

            return;
        }

    }
}
