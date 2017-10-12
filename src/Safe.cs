using System;
using System.Linq;
using System.Timers;
using System.Collections.Generic;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using GrandTheftMultiplayer.Shared;
using GrandTheftMultiplayer.Shared.Math;
using Newtonsoft.Json;

namespace SafeCracker
{
    public class Safe
    {
        public Guid ID { get; private set; }
        public Vector3 Position { get; private set; }
        public float Rotation { get; private set; }

        [JsonIgnore]
        public bool IsOpen { get; private set; }

        [JsonIgnore]
        public int LockAngle { get; private set; }

        [JsonIgnore]
        public Client Occupier { get; set; }

        [JsonIgnore]
        public GrandTheftMultiplayer.Server.Elements.Object Object { get; private set; }

        [JsonIgnore]
        private GrandTheftMultiplayer.Server.Elements.Object DoorObject;

        [JsonIgnore]
        private TextLabel Label;

        [JsonIgnore]
        private ColShape ColShape;

        [JsonIgnore]
        private List<SafeLootItem> SafeLoot = new List<SafeLootItem>();

        [JsonIgnore]
        private int RemainingSeconds;

        [JsonIgnore]
        private Timer Timer;

        public Safe(Guid id, Vector3 position, float rotation)
        {
            ID = id;
            Position = position;
            Rotation = rotation;
        }

        public void Create()
        {
            Object = API.shared.createObject(API.shared.getHashKey("v_ilev_gangsafe"), Position, new Vector3(0.0, 0.0, Rotation));
            DoorObject = API.shared.createObject(API.shared.getHashKey("v_ilev_gangsafedoor"), Position, new Vector3(0.0, 0.0, Rotation));
            ColShape = API.shared.createCylinderColShape(Position, 1.25f, 1.0f);

            Label = API.shared.createTextLabel("~g~Safe", Position, 10f, 0.65f, false);
            Label.attachTo(Object.handle, null, new Vector3(-0.35, 0.25, 1.05), new Vector3(0.0, 0.0, 0.0));

            LockAngle = Main.SafeRNG.Next(0, 361);

            ColShape.onEntityEnterColShape += (shape, entity) =>
            {
                Client player;

                if ((player = API.shared.getPlayerFromHandle(entity)) != null)
                {
                    player.triggerEvent("SetSafeNearby", true);
                    player.setData("temp_SafeID", ID);
                }
            };

            ColShape.onEntityExitColShape += (shape, entity) =>
            {
                Client player;

                if ((player = API.shared.getPlayerFromHandle(entity)) != null)
                {
                    if (player == Occupier) Occupier = null;

                    player.triggerEvent("SetSafeNearby", false);
                    player.triggerEvent("SetDialInfo", 0.0, false);
                    player.resetData("temp_SafeID");
                }
            };
        }

        public void GenerateLoot(int amount = 0)
        {
            DestroyLoot();

            if (amount < 1 || amount > Main.SafeMoneyOffset.Count) amount = Main.SafeRNG.Next(1, Main.SafeMoneyOffset.Count + 1);
            for (int i = 0; i < amount; i++) SafeLoot.Add(new SafeLootItem(this, Main.SafeMoneyOffset[i]));
        }

        public void Loot(Client player)
        {
            if (SafeLoot.Count < 1) return;

            SafeLootItem loot = SafeLoot.FirstOrDefault();
            if (loot == null) return;

            player.sendNotification("Safe", string.Format("~g~+${0:n0}", loot.Amount));
            API.shared.exported.MoneyAPI.ChangeMoney(player, loot.Amount);
            API.shared.playSoundFrontEnd(player, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            loot.Destroy();
            SafeLoot.Remove(loot);
        }

        public void Countdown()
        {
            RemainingSeconds--;

            if (RemainingSeconds < 1)
            {
                Label.text = "~g~Safe";

                LockAngle = Main.SafeRNG.Next(0, 361);
                SetDoorOpen(false);
            }
            else
            {
                TimeSpan time = TimeSpan.FromSeconds(RemainingSeconds);
                Label.text = string.Format("~r~Safe ~n~~w~{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            }
        }

        public void SetDoorOpen(bool is_open)
        {
            IsOpen = is_open;
            DoorObject.rotation = new Vector3(0.0, 0.0, (is_open) ? Rotation + 105.0 : Rotation);

            if (is_open)
            {
                RemainingSeconds = Main.SafeRespawnTime;

                Timer = API.shared.startTimer(1000, false, () => {
                    Countdown();
                });
            }
            else
            {
                DestroyLoot();

                if (Timer != null) API.shared.stopTimer(Timer);
                Timer = null;
            }
        }

        public void DestroyLoot()
        {
            foreach (SafeLootItem item in SafeLoot) item.Destroy();
            SafeLoot.Clear();
        }

        public void Destroy(bool check_players = false)
        {
            if (check_players)
            {
                foreach (NetHandle handle in ColShape.getAllEntities())
                {
                    if (API.shared.getEntityType(handle) != EntityType.Player) continue;

                    Client player = API.shared.getPlayerFromHandle(handle);
                    if (player == null) continue;

                    player.triggerEvent("SetSafeNearby", false);
                    player.triggerEvent("SetDialInfo", 0.0, false);
                    player.resetData("temp_SafeID");
                }
            }

            DestroyLoot();

            Object.delete();
            DoorObject.delete();
            Label.delete();

            API.shared.deleteColShape(ColShape);
            if (Timer != null) API.shared.stopTimer(Timer);
        }
    }
}
