
using Newtonsoft.Json;
using StardewValley;
using System;
using System.Text;

namespace MultiplayerPortalGuns
{
    /// <summary> For messaging portals between clients </summary>
    internal class PortalPosition : IEquatable<PortalPosition>
    {
        /// <summary> Unique id for retrieving 
        /// PortalPosition from Lists </summary>
        public int Id { get; set; }

        public bool Modified { get; set; } = true;
        public bool Placed { get; set; } = false;
        public int Index { get; set; }
        public int PlayerIndex { get; set; }
        /// <summary> X tile position </summary>
        public int X { get; set; }
        /// <summary> Y tile position </summary>
        public int Y { get; set; }
        /// <summary> LocationName of Portal's location</summary>
        public GameLocation location { get; set; }

        /*public PortalPosition(int Id)
        {
            this.Id = Id;
        }*/

        public PortalPosition(int index, string uniqueName, int playerIndex)
        {
            this.Index = index;
            this.PlayerIndex = playerIndex;
            this.location = null;
            this.Id = GenerateId(uniqueName);
        }
        [JsonConstructor]
        public PortalPosition(int index, string uniqueName, int playerIndex, 
            int X, int Y, GameLocation location)
        {
            this.Index = index;
            this.PlayerIndex = playerIndex;
            this.X = X;
            this.Y = Y;
            this.location = location;
            this.Id = GenerateId(uniqueName);
        }

        private int GenerateId(string uniqueName)
        {
            // assumes index has been set
            int locationHash = location != null ? location.GetHashCode() : "null".GetHashCode();
            return new StringBuilder()
                .Append(uniqueName)
                .Append(this.Index)
                .Append(this.X).Append(this.Y)
                .Append(locationHash)
                .ToString()
                .GetHashCode();
        }

        public override bool Equals(object other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public bool Equals(PortalPosition other)
        {
            return this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return this.Id;
        }
    }
}
