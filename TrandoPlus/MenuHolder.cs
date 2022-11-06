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

        internal static MenuHolder Instance { get; private set; }

        public static void OnExitMenu()
        {
            Instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.JumpToTPPage;
            return true;
        }

        private void UpdateSmallButtonColours()
        {
            if (JumpToTPPage != null)
            {
                JumpToTPPage.Text.color = TrandoPlus.GS.IsEnabled() ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            }
            if (JumpToLrrPage != null)
            {
                JumpToLrrPage.Text.color = TrandoPlus.GS.LimitedRoomRandoConfig.AnySceneRemoval ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            }
        }


        private static void ConstructMenu(MenuPage landingPage) => Instance = new(landingPage);

        private MenuHolder(MenuPage landingPage)
        {
            MainPage = new MenuPage(Localize("TrandoPlus"), landingPage);
            tpMEF = new(MainPage, TrandoPlus.GS);
            foreach (IValueElement e in tpMEF.Elements)
            {
                e.SelfChanged += obj => UpdateSmallButtonColours();
            }

            IMenuElement[] elements = tpMEF.Elements;

            // Create submenu for LRR, regardless of whether RandoPlus is installed
            {
                lrrPage = new(Localize("Limited Room Rando"), MainPage);
                lrrMEF = new(lrrPage, TrandoPlus.GS.LimitedRoomRandoConfig);
                lrrVIP = new(lrrPage, new(0, 300), 75f, true, lrrMEF.Elements);
                Localize(lrrMEF);
                JumpToLrrPage = new(MainPage, Localize("Limited Room Rando"));
                JumpToLrrPage.AddHideAndShowEvent(MainPage, lrrPage);

                foreach (IValueElement e in lrrMEF.Elements)
                {
                    e.SelfChanged += obj => UpdateSmallButtonColours();
                }

                elements = elements.Append(JumpToLrrPage).ToArray();
            }
            
            UpdateSmallButtonColours();

            tpVIP = new(MainPage, new(0, 300), 50f, true, elements);
            Localize(tpMEF);

            JumpToTPPage = new(landingPage, Localize("TrandoPlus"));
            JumpToTPPage.AddHideAndShowEvent(landingPage, MainPage);
            UpdateSmallButtonColours();
        }

        internal void LoadFrom(GlobalSettings gs)
        {
            tpMEF.SetMenuValues(gs);
            lrrMEF.SetMenuValues(gs.LimitedRoomRandoConfig);

            UpdateSmallButtonColours();
        }
    }
}