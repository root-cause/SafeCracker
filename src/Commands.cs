using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using GrandTheftMultiplayer.Shared.Math;

namespace SafeCracker
{
    public class Commands : Script
    {
        [Command("createsafe")]
        public void CMD_CreateSafe(Client player, float distance)
        {
            if (API.getPlayerAclGroup(player) != "Admin")
            {
                player.sendChatMessage("~r~ERROR: ~w~Only admins can use this command.");
                return;
            }

            Vector3 position = Main.XYInFrontOfPoint(player.position, player.rotation.Z, distance) - new Vector3(0.0, 0.0, 0.25);
            Main.CreateSafe(position, player.rotation.Z);
        }

        [Command("removesafe")]
        public void CMD_RemoveSafe(Client player)
        {
            if (API.getPlayerAclGroup(player) != "Admin")
            {
                player.sendChatMessage("~r~ERROR: ~w~Only admins can use this command.");
                return;
            }

            if (!player.hasData("temp_SafeID"))
            {
                player.sendChatMessage("~r~ERROR: ~w~You're not near a safe.");
                return;
            }

            Main.RemoveSafe(player.getData("temp_SafeID"));
        }
    }
}