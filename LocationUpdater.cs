
using System.Collections.Generic;


namespace MultiplayerPortalGuns
{
    internal class LocationUpdater<T>
    {
        public Dictionary<string, List<T>> LocationToList { get; set; }
        public Dictionary<int, string> HashCodeToLocation { get; set; }

        internal LocationUpdater()
        {
            LocationToList = new Dictionary<string, List<T>>();
            HashCodeToLocation = new Dictionary<int, string>();
        }

        public bool AddItem(string locationName, T item)
        {
            // if the portal already exists in the mappings
            if (HashCodeToLocation.ContainsKey(item.GetHashCode()))
            {
                // try to remove it
                if (!LocationToList[HashCodeToLocation[item.GetHashCode()]].Remove(item))
                    return false;
                // set the new mapping
                HashCodeToLocation[item.GetHashCode()] = locationName;
            }
            else // portal does not exist in mapping
            {
                // if location does not have a mapping with a portal list
                if (!LocationToList.ContainsKey(locationName))
                {
                    // add location and give it a new portal list
                    LocationToList.Add(locationName, new List<T>());
                }
                // add the new portal to the mapping
                HashCodeToLocation.Add(item.GetHashCode(), locationName);
            }
            // add the portal to the Location's portal list
            LocationToList[locationName].Add(item);

            return true;
        }

        public T GetItem(T item)
        {
            if (HashCodeToLocation.TryGetValue(item.GetHashCode(), out string locationName))
            {
                return LocationToList[locationName][LocationToList[locationName].IndexOf(item)];
            }
            else
                return default;
        }

        public List<T> GetItemList(string locationName)
        {
            List<T> itemList;
            if (LocationToList.TryGetValue(locationName, out itemList))
                return itemList;
            else
                return null;
        }

        public bool RemoveItem(T item)
        {
            if (HashCodeToLocation.ContainsKey(item.GetHashCode()))
            {
                if (!LocationToList[HashCodeToLocation[item.GetHashCode()]].Remove(item))
                    return false;
            }
            if (!HashCodeToLocation.Remove(item.GetHashCode()))
                return false;

            return true;
        }

        public string GetLocationName(int id)
        {
            if (HashCodeToLocation.TryGetValue(id, out string locationName))
                return locationName;
            else
                return "";
        }
    }

}
