using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;
using RandomizerMod.Menu;
using UnityEngine.SceneManagement;
using static RandomizerMod.Localization;

namespace TrandoPlus
{
    public class MenuHolder
    {
        internal MenuPage MainPage;
        internal MenuElementFactory<GlobalSettings> doorMEF;
        internal VerticalItemPanel doorVIP;

        internal SmallButton JumpToDRPage;

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
            JumpToDRPage = new(landingPage, Localize("TrandoPlus"));
            JumpToDRPage.AddHideAndShowEvent(landingPage, MainPage);
            button = JumpToDRPage;
            return true;
        }

        private void ConstructMenu(MenuPage landingPage)
        {
            MainPage = new MenuPage(Localize("TrandoPlus"), landingPage);
            doorMEF = new(MainPage, TrandoPlus.GS);
            doorVIP = new(MainPage, new(0, 300), 50f, false, doorMEF.Elements);
            Localize(doorMEF);
        }
    }
}