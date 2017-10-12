using System;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Shared.Math;

namespace SafeCracker
{
    public class SafeLootItem
    {
        public int Amount;
        public GrandTheftMultiplayer.Server.Elements.Object Object;
        public TextLabel Label;

        public SafeLootItem(Safe safe, Vector3 offset)
        {
            Amount = Main.SafeRNG.Next(Main.SafeMinLoot, Main.SafeMaxLoot + 1);

            Object = API.shared.createObject(API.shared.getHashKey("bkr_prop_moneypack_01a"), safe.Position, new Vector3(0.0, 0.0, 0.0));
            Object.attachTo(safe.Object.handle, null, offset, new Vector3(0.0, 0.0, 0.0));

            Label = API.shared.createTextLabel("~g~$" + Amount, safe.Position, 7.5f, 0.65f, true);
            Label.attachTo(Object.handle, null, new Vector3(0, 0, 0.17), new Vector3(0, 0, 0));
        }

        public void Destroy()
        {
            Object.delete();
            Label.delete();
        }
    }
}