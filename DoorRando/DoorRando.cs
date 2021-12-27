using Modding;

namespace DoorRando
{
    public class DoorRando : Mod, IGlobalSettings<GlobalSettings>
    {
        internal static DoorRando instance;

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings s) => GS = s;
        public GlobalSettings OnSaveGlobal() => GS;

        public DoorRando() : base(null)
        {
            instance = this;
        }
        
        public override string GetVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }
        
        public override void Initialize()
        {
            Log("Initializing Mod...");
            bool rando = ModHooks.GetMod("Randomizer 4") is Mod;

            if (rando) MenuHolder.Hook();
            if (rando) DoorRandoAdder.Hook();
        }
    }
}