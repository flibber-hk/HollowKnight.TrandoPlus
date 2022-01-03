using Modding;

namespace TrandoPlus
{
    public class TrandoPlus : Mod, IGlobalSettings<GlobalSettings>
    {
        internal static TrandoPlus instance;

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings s) => GS = s;
        public GlobalSettings OnSaveGlobal() => GS;

        public TrandoPlus() : base(null)
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
            if (rando) DropRandoAdder.Hook();
            if (rando) ConditionManager.Hook();
        }
    }
}