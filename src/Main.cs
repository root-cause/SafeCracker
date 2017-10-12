using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Shared.Math;
using Newtonsoft.Json;

namespace SafeCracker
{
    public class Main : Script
    {
        // config, use meta.xml instead
        public static int SafeRespawnTime = 300;
        public static int SafeMinLoot = 150;
        public static int SafeMaxLoot = 500;
        public static string SafeDir = "Safes";

        // other variables
        public static List<Vector3> SafeMoneyOffset = new List<Vector3>
        {
            new Vector3(-0.1, 0.25, 0.085),
            new Vector3(-0.35, 0.25, 0.085),
            new Vector3(-0.6, 0.25, 0.085),
            new Vector3(-0.6, 0.5, 0.085),
            new Vector3(-0.35, 0.5, 0.085),
            new Vector3(-0.1, 0.5, 0.085)
        };

        public static List<Safe> Safes = new List<Safe>();
        public static Random SafeRNG = new Random();

        public Main()
        {
            API.onResourceStart += SafeCracker_Init;
            API.onClientEventTrigger += SafeCracker_ClientEvent;
            API.onPlayerDisconnected += SafeCracker_Disconnect;
            API.onResourceStop += SafeCracker_Exit;
        }

        #region Methods
        public static bool IsIDInUse(Guid ID)
        {
            return (Safes.FirstOrDefault(s => s.ID == ID) != null);
        }

        public static Vector3 XYInFrontOfPoint(Vector3 pos, float angle, float distance)
        {
            angle *= (float)Math.PI / 180;
            pos.X += (distance * (float)Math.Sin(-angle));
            pos.Y += (distance * (float)Math.Cos(-angle));
            return pos;
        }
        #endregion

        #region Safe Methods
        public static void CreateSafe(Vector3 position, float rotation)
        {
            // generate id
            Guid ID;

            do
            {
                ID = Guid.NewGuid();
            } while (IsIDInUse(ID));

            // create entity
            Safe new_safe = new Safe(ID, position, rotation);
            Safes.Add(new_safe);

            new_safe.Create();

            // create file
            File.WriteAllText(SafeDir + Path.DirectorySeparatorChar + ID + ".json", JsonConvert.SerializeObject(new_safe, Formatting.Indented));
        }

        public static void RemoveSafe(Guid ID)
        {
            // verify safe
            Safe safe = Safes.FirstOrDefault(s => s.ID == ID);
            if (safe == null) return;

            // destroy entity
            safe.Destroy(true);
            Safes.Remove(safe);

            // delete file
            string file = SafeDir + Path.DirectorySeparatorChar + ID + ".json";
            if (File.Exists(file)) File.Delete(file);
        }
        #endregion

        #region Events
        public void SafeCracker_Init()
        {
            // load config
            if (API.hasSetting("safeDirName")) SafeDir = API.getSetting<string>("safeDirName");
            if (API.hasSetting("safeRespawnTime")) SafeRespawnTime = API.getSetting<int>("safeRespawnTime");
            if (API.hasSetting("safeMinLoot")) SafeMinLoot = API.getSetting<int>("safeMinLoot");
            if (API.hasSetting("safeMaxLoot")) SafeMaxLoot = API.getSetting<int>("safeMaxLoot");

            API.consoleOutput("SafeCracker Loaded");
            API.consoleOutput("-> Dir Name: {0}", SafeDir);
            API.consoleOutput("-> Safe Respawn Time: {0}", TimeSpan.FromSeconds(SafeRespawnTime).ToString(@"hh\:mm\:ss"));
            API.consoleOutput("-> Safe Loot: ${0:n0} - ${1:n0}", SafeMinLoot, SafeMaxLoot);

            // verify directory
            SafeDir = API.getResourceFolder() + Path.DirectorySeparatorChar + SafeDir;
            if (!Directory.Exists(SafeDir)) Directory.CreateDirectory(SafeDir);

            // load safes
            foreach (string file in Directory.GetFiles(SafeDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                Safe safe = JsonConvert.DeserializeObject<Safe>(File.ReadAllText(file));
                Safes.Add(safe);

                safe.Create();
            }

            API.consoleOutput("Loaded {0} safes.", Safes.Count);
        }

        public void SafeCracker_ClientEvent(Client player, string eventName, params object[] arguments)
        {
            switch (eventName)
            {
                case "OpenSafe":
                {
                    if (!player.hasData("temp_SafeID")) return;

                    Safe safe = Safes.FirstOrDefault(s => s.ID == player.getData("temp_SafeID"));
                    if (safe == null) return;

                    if ((int)arguments[0] != safe.LockAngle)
                    {
                        API.playSoundFrontEnd(player, "Drill_Pin_Break", "DLC_HEIST_FLEECA_SOUNDSET");

                        player.triggerEvent("SetDialInfo", safe.LockAngle, true);
                        player.sendNotification("Safe", "~r~Wrong code!");
                    }
                    else
                    {
                        safe.GenerateLoot();
                        safe.SetDoorOpen(true);
                        safe.Occupier = null;
                        
                        API.playSoundFrontEnd(player, "Drill_Pin_Break", "DLC_HEIST_FLEECA_SOUNDSET");
                        player.triggerEvent("SetDialInfo", 0.0, false);
                    }

                    break;
                }

                case "InteractSafe":
                {
                    if (!player.hasData("temp_SafeID")) return;

                    Safe safe = Safes.FirstOrDefault(s => s.ID == player.getData("temp_SafeID"));
                    if (safe == null) return;

                    if (safe.IsOpen)
                    {
                        safe.Loot(player);
                    }
                    else
                    {
                        if (safe.Occupier != null && API.getPlayerFromHandle(safe.Occupier) != null)
                        {
                            player.sendNotification("Safe", "~r~This safe is occupied.");
                            return;
                        }

                        safe.Occupier = player;
                        player.triggerEvent("SetDialInfo", safe.LockAngle, true);
                    }

                    break;
                }
            }
        }

        public void SafeCracker_Disconnect(Client player, string reason)
        {
            if (!player.hasData("temp_SafeID")) return;

            Safe safe = Safes.FirstOrDefault(s => s.ID == player.getData("temp_SafeID"));
            if (safe == null) return;

            safe.Occupier = null;
        }

        public void SafeCracker_Exit()
        {
            foreach (Safe safe in Safes) safe.Destroy();
            Safes.Clear();
        }
        #endregion
    }
}