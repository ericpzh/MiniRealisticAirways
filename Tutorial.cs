using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UIComponents.Modals;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace MiniRealisticAirways
{
    public class TutorialPage
    {
        public TutorialPage(string enHeading, string enDescription, string cnHeading, string cnDescription)
        {
            enHeading_ = enHeading;
            cnHeading_ = cnHeading;
            enDescription_ = enDescription;
            cnDescription_ = cnDescription;
        }

        public string enHeading_;
        public string cnHeading_;
        public string enDescription_;
        public string cnDescription_;
    }

    public static class Tutorial
    {
        public static IEnumerator ShowTutorialCoroutine(bool manualTrigger = false)
        {
            // Hide QRH button in main menu.
            if (manualTrigger)
            {
                tutorialButton.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(1);
            }

            yield return new WaitUntil(() => ModalManager.Instance != null);

            ShowModTutorial(manualTrigger);

            yield return new WaitUntil(() => modal == null);
            // Reset page.
            tutorialPage = 0;

            // Show QRH button in main menu.
            if (manualTrigger)
            {
                tutorialButton.gameObject.SetActive(true);
            }
        }

        public static void SetQRHText()
        {
            if (tutorialButton == null)
            {
                return;
            }
            TMP_Text text = tutorialButton.GetComponentInChildren<TMP_Text>();
            if (text == null)
            {
                return;
            }
            text.text = ShowEnLocale() ? "QRH" : "快速检查单";
        }

        public static bool ShowEnLocale()
        {
            if (LocalizationSettings.SelectedLocale == null || LocalizationSettings.SelectedLocale.LocaleName == null)
            {
                return true;
            }
            return !LocalizationSettings.SelectedLocale.LocaleName.Contains("Chinese");
        }

        private static void ShowModTutorial(bool manualTrigger)
        {
            string id = PluginInfo.PLUGIN_GUID.ToString() + PluginInfo.PLUGIN_VERSION.ToString();
            modal = ModalManager.NewModalWithButtonStatic(manualTrigger ? id + UnityEngine.Random.value : id);
            SetTitle(modal, "Mini Realistic Airways", "真实迷你空管");
            SetHeading(modal, tutorialPages[tutorialPage].enHeading_, tutorialPages[tutorialPage].cnHeading_);
            SetDescription(modal, tutorialPages[tutorialPage].enDescription_, tutorialPages[tutorialPage].cnDescription_);
            SetButton(modal, manualTrigger);
            modal.SetDescriptionTextAlign(TMPro.TextAlignmentOptions.Left);
            modal.Show();

            DontShowAgainToggle toggle = modal.GetComponentInChildren<DontShowAgainToggle>();
            toggle?.gameObject.SetActive(IsLastPage() && !manualTrigger);

            CloseButton closeButton = modal.GetComponentInChildren<CloseButton>();
            closeButton?.gameObject.SetActive(IsLastPage() || manualTrigger);
        }

        private static void SetTitle(ModalWithButton modal, string en, string cn)
        {
            modal.SetTitle(ShowEnLocale() ? en : cn);
        }

        private static void SetHeading(ModalWithButton modal, string en, string cn)
        {
            modal.SetHeading(ShowEnLocale() ? en : cn);
        }

        private static void SetDescription(ModalWithButton modal, string en, string cn)
        {
            modal.SetDescription(ShowEnLocale() ? en : cn);
        }

        private static void SetButton(ModalWithButton modal, bool manualTrigger)
        {
            // Do not show NEXT on the last page.
            if (IsLastPage())
            {
                modal.button.gameObject.SetActive(false);
                modal.description.gameObject.AddComponent<LinkHandler>().url = ShowEnLocale() ? 
                "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#mini-realistic-airways" : 
                "https://github.com/ericpzh/MiniRealisticAirways?tab=readme-ov-file#%E8%BF%B7%E4%BD%A0%E7%9C%9F%E5%AE%9E%E7%A9%BA%E7%AE%A1";
            }
            else
            {
                modal.SetButtonText(ShowEnLocale() ? "Next" : "下一页");
                modal.SetButtonOnClick(() => {
                    ++tutorialPage;
                    modal.PostHide();
                    ShowModTutorial(manualTrigger);
                });
            }

        }

        private static bool IsLastPage()
        {
            return tutorialPage == tutorialPages.Count - 1;
        }

        public static Button tutorialButton;
        private static ModalWithButton modal;
        private static int tutorialPage = 0;
        private static List<TutorialPage> tutorialPages = new List<TutorialPage> {
            new TutorialPage (
                "Altitude", 
                @"Aircraft operates in <b><u>low</u></b>, <b><u>normal</u></b>, or <b><u>high</u></b> altitude. The current altitude of an aircraft is displayed in the bottom-left corner of the aircraft.
                Aircraft arrives at <b><u>high</u></b> altitude and can only land with <b><u>low</u></b> altitude.
                Aircraft takeoffs at <b><u>low</u></b> altitude and can only depart in <b><u>normal</u></b> or <b><u>high</u></b> altitude.
                Press <b><u>W</u></b> or <b><u>Scroll Up</u></b> to increase the altitude of aircraft/waypoint.
                Press <b><u>S</u></b> or <b><u>Scroll Down</u></b> to decrease the altitude of aircraft/waypoint.
                Terrain will not affect aircraft in <b><u>high</u></b> altitude.",
                "高度",
                @"飞机会处于<b><u>低</u></b>、<b><u>中</u></b>、<b><u>高</u></b>三种高度，当前高度会在飞机图标的左下角显示：
                飞机将以<b><u>高</u></b>进场，且只有在<b><u>低</u></b>时才能降落。
                飞机将从<b><u>低</u></b>起飞，且必须以<b><u>中</u></b>或<b><u>高</u></b>离场。
                按<b><u>W</u></b>或<b><u>滚轮上</u></b>会增加飞机、航点高度。
                按<b><u>S</u></b>或<b><u>滚轮下</u></b>会降低飞机、航点高度。
                地形不会影响<b><u>高</u></b>的飞机。"
            ),
            new TutorialPage (
                "Speed",
                @"Aircraft operates in <b><u>slow</u></b>, <b><u>normal</u></b>, or <b><u>fast</u></b> speed. The current speed of an aircraft is displayed in the bottom-right corner of the aircraft.
                Aircraft arrives at <b><u>normal</u></b> speed and can only land when it is at <b><u>slow</u></b> or <b><u>normal</u></b> speed.
                Aircraft lifts off at <b><u>normal</u></b> speed.
                Press <b><u>D</u></b> or hold <b><u>Left Shift</u></b> while <b><u>Scroll Up</u></b> to increase the speed of aircraft/waypoint.
                Press <b><u>A</u></b> or hold <b><u>Left Shift</u></b> while <b><u>Scroll Down</u></b> to decrease the speed of aircraft/waypoint.",
                "速度",
                @"飞机会处于<b><u>慢</u></b>、<b><u>中</u></b>和<b><u>快</u></b>三种速度，飞机的当前速度会在飞机图标的右下角显示：
                飞机会以<b><u>中速</u></b>进场，且只有在<b><u>慢速</u></b>或<b><u>中速</u></b>时才能降落。
                飞机会以<b><u>中速</u></b>起飞。
                按<b><u>D</u></b>或滚轮<b><u>滚轮上</u></b>并按住<b><u>左SHIFT</u></b>会增加飞机、航点速度。
                按<b><u>A</u></b>或滚轮<b><u>滚轮下</u></b>并按住<b><u>左SHIFT</u></b>会降低飞机、航点速度。"
            ),
            new TutorialPage (
                "Type",
                @"Aircraft can be <b><u>Light</u></b>, <b><u>Medium</u></b>, or <b><u>Heavy</u></b> type. Arrival aircraft carry a limited amount of fuel, fuel level is displayed in the top-right corner of the aircraft.
                <b><u>Light</u></b> aircraft's plane icon size is small. It will only have a max speed of <b><u>normal</u></b>, and can only land with <b><u>slow</u></b>. It has 50% faster turning speed and accounts for 2.5% of all aircraft. <b><u>Light</b></u> aircraft has 3 days worth of fuel.
                <b><u>Medium</u></b> aircraft has 3.5 days worth of fuel.
                <b><u>Heavy</u></b> aircraft's plane icon size is large. It accounts for 30% of all aircraft and has 4 days worth of fuel.",
                "机型",
                @"飞机属于<b><u>轻</u></b>、<b><u>中</u></b>、<b><u>重</u></b>三种机型，可通过图标大小判断。进场飞机会拥有油量限制，剩余油量显示在飞机图标右上角：
                <b><u>轻型</u></b>飞机最大速度为<b><u>中速</u></b>，且只有在速度为<b><u>慢速</u></b>时才能降落。其转弯速度比其他类型快50%，且进场时携带3天的燃料。<b><u>轻型</u></b>飞机占所有飞机的2.5%。
                <b><u>中型</u></b>飞机进场时携带3.5天的燃料。
                <b><u>重型</u></b>飞机进场时携带4天的燃料，且占所有飞机的30%。"
            ),
            new TutorialPage (
                "Events",
                @"Sometimes, accidents do happen.
                Runway excursion happened leading to a runway closure.
                Aircraft may arrive with emergency fuel and need to land immediately.
                Aircraft may have an engine failure and need to return to field immediately.
                Weather pattern shows up in some areas, turning <b><u>low</u></b>, <b><u>normal</u></b> into a restricted area.",
                "特情",
                @"意外随时有可能发生：
                跑道意外关闭。
                一架低油量飞机备降到本场，需要优先安排降落。
                一架离场飞机遭遇引擎故障，需要立即返场。
                恶劣天气会使部分<b><u>中</u></b><b><u>低</u></b>空域变为禁飞区。"
            ),
            new TutorialPage (
                "Wind",
                @"Wind can affect aircraft's takeoff/landing performance. Wind direction is displayed in the top-left corner of the screen. 
                When aircraft are landing/taking off with a full tailwind, the go-around/reject takeoff probability is high.
                Reject takeoff/go-around probability drops to 0% when the wind direction is at or below 90 degrees of the runway.",
                "风向",
                @"风向会影响飞机的起降性能。可以在屏幕左上角查看当前风向:
                当飞机顺风着陆、起飞时，复飞、中断起飞概率将变高。
                当风向与跑道成 90 度或以下时，复飞、中断起飞概率为零。"
            ),
            new TutorialPage (
                "Last",
                @"
                Use <b><u>Tab</u></b> to hide/show all the in-game text.
                When two aircraft are about to crash, TCAS will command one to climb and the other to descend.
                When aircraft is about to crash into terrain, GPWS will command aircraft to climb.
                You now start with 3 waiting area upgrades.
                You now get upgrades twice as fast.
                Aircraft flying out-of-bound now count as restricted area violations.",
                "最后",
                @"
                可以按<b><u>Tab</u></b>隐藏文字信息。
                TCAS会命令即将碰撞的飞机爬升或下降。
                GPWS会命令即将即将撞山的飞机爬升。
                开场时自动获得3个等待区升级。
                升级每半天刷新一次。
                飞出屏幕相当于飞入禁飞区。"
            ),
            new TutorialPage (
                "Thanks for playing \"Mini Realistic Airways\"!",
                "For more information, refer to the <b><u><link=\"ENG\">Quick Reference Handbook</link></u></b>", 
                "感谢您游玩\"真实迷你空管\"!\n",
                "请随时参阅<b><u><link=\"CHS\">快速检查单</link></u></b>"
            )
        };
    }

    [HarmonyPatch]
    public class MainMenuManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenuManager), "Start")]
        public static void StartPostfix(ref Button ___EditorButton)
        {
            Tutorial.tutorialButton = GameObject.Instantiate(___EditorButton.gameObject, ___EditorButton.transform.parent).GetComponent<Button>();
            Tutorial.SetQRHText();
            Tutorial.tutorialButton.transform.localPosition = new Vector3(1280f, -430f, 0);
            Tutorial.tutorialButton.onClick.RemoveAllListeners();
            Tutorial.tutorialButton.onClick = new Button.ButtonClickedEvent();
            Tutorial.tutorialButton.onClick.AddListener(() => { AudioManager.instance.StartCoroutine(Tutorial.ShowTutorialCoroutine(manualTrigger: true)); });
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenuManager), "Update")]
        public static void UpdatePostfix()
        {
            Tutorial.SetQRHText();
        }
    }
}
