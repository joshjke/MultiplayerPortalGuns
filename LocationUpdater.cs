
using System.Collections;
using System.Collections.Generic;


namespace MultiplayerPortalGuns
{
    /// <summary>
    /// LocationUpdater is a meant to be a solution for editing the SDV world
    /// while in a multiplayer context.
    /// Since only the active locations of a farmerhand (peer, not host) 
    /// can be edited w/out exceptions, a lookup table is kept for all modifications.
    /// 
    /// Anytime a location's map is edited, by a mod a "T" item should be added using
    /// AddItem(string locationName, T item) with data that can be interpreted 
    /// by the game to make the necessary modifications to the map later
    /// 
    /// Whenever a player enters a new location, GetItemList(string locationName)
    /// should be called to update the player's local map with the necessary T items
    /// 
    /// Currently all items must be unique
    /// 
    /// If an edit is removed, RemoveItem should be called to updated the tables for other players
    /// </summary>
    /// <typeparam name="T">Data type for containing map edits, referred to as Item</typeparam>
    internal class LocationUpdater<T>
    {
        public Dictionary<string, List<T>> LocationToItemList { get; set; }
        public Dictionary<int, string> ItemToLocation { get; set; }

        internal LocationUpdater()
        {
            LocationToItemList = new Dictionary<string, List<T>>();
            ItemToLocation = new Dictionary<int, string>();
        }

        public bool AddItem(string locationName, T item)
        {
            // Remove the item if it already exists
            RemoveItem(item);
            
            // if location does not have a mapping with an item list
            if (!LocationToItemList.ContainsKey(locationName))
            {
                // add location to dictionary and give it a new item list
                LocationToItemList.Add(locationName, new List<T>());
            }
            // add the item to the mapping
            ItemToLocation.Add(item.GetHashCode(), locationName);

            // add the item to the Location's item list
            LocationToItemList[locationName].Add(item);

            return true;
        }

        /// <summary>
        /// not sure why this function exists, but I'll keep it just in case
        /// it can return defualt if the item isn't found
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public T GetItemFromId(T item)
        {
            if (ItemToLocation.TryGetValue(item.GetHashCode(), out string locationName))
            {
                return LocationToItemList[locationName][LocationToItemList[locationName].IndexOf(item)];
            }
            else
                return default;
        }

        /// <summary>
        /// Returns all items for the corresponding location
        /// Useful for loading edits in a map when the player's location has changed
        /// </summary>
        /// <param name="locationName">SDV name of the map location</param>
        /// <returns></returns>
        public List<T> GetItemsInLocation(string locationName)
        {
            List<T> itemList;
            if (LocationToItemList.TryGetValue(locationName, out itemList))
                return itemList;
            else
                return null;
        }

        /// <summary>
        /// Removes the specified item, using it's hashcode
        /// </summary>
        /// <param name="item">item to to GetHashCode from</param>
        /// <returns></returns>
        public bool RemoveItem(T item)
        {
            if (ItemToLocation.ContainsKey(item.GetHashCode()))
            {
                if (!LocationToItemList[ItemToLocation[item.GetHashCode()]].Remove(item))
                    return false;
            }
            if (!ItemToLocation.Remove(item.GetHashCode()))
                return false;

            return true;
        }

        /// <summary>
        /// Gets the location name the item is located in
        /// </summary>
        /// <param name="id">The item to get the location from</param>
        /// <returns></returns>
        public string GetLocationName(int id)
        {
            if (ItemToLocation.TryGetValue(id, out string locationName))
                return locationName;
            else
                return "";
        }
    }

}
