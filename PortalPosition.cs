
namespace MultiplayerPortalGuns
{
    /// <summary> For messaging portals between clients </summary>
    internal class PortalPosition
    {
        /// <summary> X tile position </summary>
        public int X { get; set; }
        /// <summary> Y tile position </summary>
        public int Y { get; set; }
        /// <summary> LocationName of Portal's location</summary>
        public string LocationName { get; set; }

        public bool Equals(PortalPosition other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y)
                && LocationName.Equals(other.LocationName);
        }
    }
}
