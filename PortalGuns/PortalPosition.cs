﻿
using Newtonsoft.Json;
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

        public int AnimationFrame = 0;
        public int Index { get; set; }
        public int PlayerIndex { get; set; }
        /// <summary> X tile position </summary>
        public int X { get; set; }
        /// <summary> Y tile position </summary>
        public int Y { get; set; }
        /// <summary> LocationName of Portal's location</summary>
        public string LocationName { get; set; }

        /*public PortalPosition(int Id)
        {
            this.Id = Id;
        }*/

        public PortalPosition(int index, string uniqueName, int playerIndex)
        {
            this.Index = index;
            this.PlayerIndex = playerIndex;
            this.LocationName = "";
            this.Id = GenerateId(uniqueName);
        }
        [JsonConstructor]
        public PortalPosition(int index, string uniqueName, int playerIndex, int X, int Y, string LocationName)
        {
            this.Index = index;
            this.PlayerIndex = playerIndex;
            this.X = X;
            this.Y = Y;
            this.LocationName = LocationName;
            this.Id = GenerateId(uniqueName);
        }
        
        public PortalPosition(PortalPosition portalPosition)
        {
            this.Index = portalPosition.Index;
            this.PlayerIndex = portalPosition.PlayerIndex;
            this.X = portalPosition.X;
            this.Y = portalPosition.Y;
            this.LocationName = portalPosition.LocationName;
            this.Id = GenerateId("savedToRemove");
        }

        private int GenerateId(string uniqueName)
        {
            // assumes index has been set
            return new StringBuilder()
                .Append(uniqueName)
                .Append(this.Index)
                .Append(this.X).Append(this.Y)
                .Append(this.LocationName)
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
