using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPortalGuns
{
    class ModEntry : Mod
    {
        public List<long> playerList = new List<long>();
        public override void Entry(IModHelper helper)
        {
            //throw new NotImplementedException();
            helper.Events.GameLoop.SaveLoaded += this.AfterLoad;


        }

        private void Multiplayer_PeerContextReceived(object sender, StardewModdingAPI.Events.PeerContextReceivedEventArgs e)
        {
            
        }

        private void AfterLoad(object sender, EventArgs e)
        {
            if (!Context.IsMultiplayer && Context.IsMainPlayer)
            {

            }
            bool hasPeers = false;
            foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
            {
                hasPeers = true;
                if (!playerList.Contains(peer.PlayerID))
                {
                    Game1.showRedMessage("Is Not Host");
                    Texture2D portalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun1.png");
                    CustomObjectData portalGun1 = CustomObjectData.newObject("PortalGun1Id", portalGunTexture, Color.White, "Portal Gun",
                        "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "");

                    Texture2D bluePortalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun2.png");
                    CustomObjectData portalGun2 = CustomObjectData.newObject("PortalGun2Id", bluePortalGunTexture, Color.White, "Blue Portal Gun",
                        "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Blue Portal Gun", "388 1"));
                }

            }
            if (!hasPeers)
            {
                Game1.showRedMessage("Is Host");
                playerList.Add(0);
                Texture2D portalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun1.png");
                CustomObjectData portalGun1 = CustomObjectData.newObject("PortalGun1Id", portalGunTexture, Color.White, "Portal Gun",
                    "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Portal Gun", "388 1"));

                Texture2D bluePortalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun2.png");
                CustomObjectData portalGun2 = CustomObjectData.newObject("PortalGun2Id", bluePortalGunTexture, Color.White, "Blue Portal Gun",
                    "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "");
            }


            //LoadPortalGunObjects();
        }

        private void LoadPortalGunObjects()
        {
            // create portalGun objects
            
            Texture2D portalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun1.png");
            CustomObjectData portalGun1 = CustomObjectData.newObject("PortalGun1Id", portalGunTexture, Color.White, "Portal Gun",
                "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Portal Gun", "388 1"));

            Texture2D bluePortalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun2.png");
            CustomObjectData portalGun2 = CustomObjectData.newObject("PortalGun2Id", bluePortalGunTexture, Color.White, "Blue Portal Gun",
                "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Blue Portal Gun", "388 1"));

            Texture2D greenPortalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun3.png");
            CustomObjectData portalGun3 = CustomObjectData.newObject("PortalGun3Id", greenPortalGunTexture, Color.White, "Green Portal Gun",
                "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Green Portal Gun", "388 1"));

            Texture2D orangePortalGunTexture = this.Helper.Content.Load<Texture2D>("PortalGun4.png");
            CustomObjectData portalGun4 = CustomObjectData.newObject("PortalGunId4", orangePortalGunTexture, Color.White, "Orange Portal Gun",
                "Property of Aperture Science Inc.", 0, "", "Basic", 1, -300, "", craftingData: new CraftingData("Orange Portal Gun", "388 1"));
        }
    }
}
