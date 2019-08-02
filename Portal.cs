using StardewValley;
using System;

namespace MultiplayerPortalGuns
{
    /// <summary> Contains Warp and PortalPosition methods </summary>
    internal class Portal
    {
        /// <summary> Contains members for X, Y, and LocationName </summary>
        public PortalPosition PortalPos { get; set; }
        /// <summary> Used to keep track of the active/inactive Warp </summary>
        public Warp Warp { get; set; }

    }
}
