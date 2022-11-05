using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace TrandoPlus
{
    internal static class RandoSettingsManagerInterop
    {
        public static void Hook()
        {
            RandoSettingsManagerMod.Instance.RegisterConnection(new TrandoPlusSettingsProxy());
        }
    }

    internal class TrandoPlusSettingsProxy : RandoSettingsProxy<GlobalSettings, string>
    {
        public override string ModKey => TrandoPlus.instance.GetName();

        public override VersioningPolicy<string> VersioningPolicy { get; }
            = new EqualityVersioningPolicy<string>(TrandoPlus.instance.GetVersion());

        public override void ReceiveSettings(GlobalSettings settings)
        {
            MenuHolder.Instance.LoadFrom(settings ?? new());
        }

        public override bool TryProvideSettings(out GlobalSettings settings)
        {
            settings = TrandoPlus.GS;
            return settings.IsEnabled();
        }
    }
}