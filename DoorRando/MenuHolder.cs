using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;
using UnityEngine.SceneManagement;

namespace DoorRando
{
    public class MenuHolder
    {
        public static MenuHolder Instance { get; private set; }

        public MenuPage MainPage;
        public SmallButton JumpButton;
        public VerticalItemPanel Panel;
        public IMenuElement DoorRandoButton;

        public static void OnExitMenu(Scene from, Scene to)
        {
            if (from.name == "Menu_Title") Instance = null;
        }

        public static void ConstructMenu(MenuPage connectionsPage)
        {
            Instance ??= new();
            Instance.OnMenuConstruction(connectionsPage);
        }

        public void OnMenuConstruction(MenuPage connectionsPage)
        {
            MainPage = new("Door Rando Main Menu", connectionsPage);
            JumpButton = new(connectionsPage, "Door Rando");
            JumpButton.AddHideAndShowEvent(MainPage);
            DoorRandoButton = CreateToggle(MainPage);
            Panel = new(MainPage, new(0, 300), 50f, false, DoorRandoButton);
        }

        public IMenuElement CreateToggle(MenuPage page)
        {
            ToggleButton button = new(page, "Randomize Doors");
            button.SetValue(DoorRando.GS.RandomizeDoors);
            button.ValueChanged += b => DoorRando.GS.RandomizeDoors = b;
            return button;
        }

        public static bool TryGetMenuButton(MenuPage connectionsPage, out SmallButton button)
        {
            return Instance.TryGetJumpButton(connectionsPage, out button);
        }

        public bool TryGetJumpButton(MenuPage connectionsPage, out SmallButton button)
        {
            button = JumpButton;
            return true;
        }
    }
}