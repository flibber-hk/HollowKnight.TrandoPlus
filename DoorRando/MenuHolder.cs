﻿using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;
using RandomizerMod.Menu;
using UnityEngine.SceneManagement;

namespace DoorRando
{
    public class MenuHolder
    {
        internal MenuPage MainPage;
        internal MenuElementFactory<GlobalSettings> doorMEF;
        internal VerticalItemPanel doorVIP;

        internal SmallButton JumpToDRPage;

        private static MenuHolder _instance = null;
        internal static MenuHolder Instance => _instance ?? (_instance = new MenuHolder());

        public static void OnExitMenu(Scene from, Scene to)
        {
            if (from.name == "Menu_Title") _instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(Instance.ConstructMenu, Instance.HandleButton);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnExitMenu;
        }

        private bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            JumpToDRPage = new(landingPage, "Door Rando");
            JumpToDRPage.AddHideAndShowEvent(landingPage, MainPage);
            button = JumpToDRPage;
            return true;
        }

        private void ConstructMenu(MenuPage landingPage)
        {
            MainPage = new MenuPage("Door Rando", landingPage);
            doorMEF = new(MainPage, DoorRando.GS);
            doorVIP = new(MainPage, new(0, 300), 50f, false, doorMEF.Elements);
        }
    }
}