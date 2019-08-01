using StardewValley;
using System;

namespace MultiplayerPortalGuns
{
    /// <summary> Contains Warp and PortalPosition methods </summary>
    internal class Portal : IEquatable<Portal>
    {
        public int id { get; set; }
        /// <summary> Contains members for X, Y, and LocationName </summary>
        public PortalPosition PortalPos { get; set; }
        /// <summary> Used to keep track of the active/inactive Warp </summary>
        public Warp Warp { get; set; }

        public override bool Equals(object other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public bool Equals(Portal other)
        {
            return id == other.id;
        }

        public override int GetHashCode()
        {
            return id;
        }

    }
}
