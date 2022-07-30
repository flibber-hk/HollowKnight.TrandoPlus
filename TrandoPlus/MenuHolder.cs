using System.Linq;
using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;
using RandomizerMod.Menu;
using static RandomizerMod.Localization;

namespace TrandoPlus
{
    public class MenuHolder
    {
        internal MenuPage MainPage;
        internal MenuElementFactory<GlobalSettings> tpMEF;
        internal VerticalItemPanel tpVIP;
        internal SmallButton JumpToTPPage;

        internal MenuPage lrrPage;
        internal MenuElementFactory<LimitedRoomRandoConfig> lrrMEF;
        internal VerticalItemPanel lrrVIP;
        internal SmallButton JumpToLrrPage;


        private static MenuHolder _instance = null;
        internal static MenuHolder Instance => _instance ?? (_instance = new MenuHolder());

        public static void OnExitMenu()
        {
            _instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(Instance.ConstructMenu, Instance.HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            JumpToTPPage = new(landingPage, Localize("TrandoPlus"));
            JumpToTPPage.AddHideAndShowEvent(landingPage, MainPage);
            button = JumpToTPPage;
            return true;
        }

        private void ConstructMenu(MenuPage landingPage)
        {
            MainPage = new MenuPage(Localize("TrandoPlus"), landingPage);
            lrrPage = new(Localize("Limited Room Rando"), MainPage);
            lrrMEF = new(lrrPage, TrandoPlus.GS.LimitedRoomRandoConfig);
            lrrVIP = new(lrrPage, new(0, 300), 75f, true, lrrMEF.Elements);
            Localize(lrrMEF);

            JumpToLrrPage = new(MainPage, Localize("Limited Room Rando"));
            JumpToLrrPage.AddHideAndShowEvent(MainPage, lrrPage);

            tpMEF = new(MainPage, TrandoPlus.GS);
            tpVIP = new(MainPage, new(0, 300), 50f, true, tpMEF.Elements.Append<IMenuElement>(JumpToLrrPage).ToArray());
            Localize(tpMEF);
        }
    }
}