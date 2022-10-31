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
            UpdateSmallButtonColours();

            button = JumpToTPPage;
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


        private void ConstructMenu(MenuPage landingPage)
        {
            MainPage = new MenuPage(Localize("TrandoPlus"), landingPage);
            tpMEF = new(MainPage, TrandoPlus.GS);
            foreach (IValueElement e in tpMEF.Elements)
            {
                e.SelfChanged += obj => UpdateSmallButtonColours();
            }

            IMenuElement[] elements = tpMEF.Elements;

            if (Modding.ModHooks.GetMod("RandoPlus") is not null)
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
        }

        private void ResetMenu()
        {
            tpMEF.SetMenuValues(TrandoPlus.GS);
            lrrMEF?.SetMenuValues(TrandoPlus.GS.LimitedRoomRandoConfig);

            UpdateSmallButtonColours();
        }
    }
}