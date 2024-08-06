using UnityEngine;
using UIComponents.Modals;
using System.Collections;
using System.Collections.Generic;

namespace MiniRealisticAirways
{
    public static class Tutorial
    {
        public static IEnumerator ShowModHintCoroutine()
        {
            yield return new WaitForSeconds(1);
            yield return new WaitUntil(() => ModalManager.Instance != null);
            modal = ModalManager.NewModalWithButtonStatic(PluginInfo.PLUGIN_GUID.ToString() + PluginInfo.PLUGIN_VERSION.ToString());
            ShowModHint();
            yield return new WaitUntil(() => modal == null);
            ShowModTutorial();
        }

        private static void ShowModHint()
        {
            modal.SetTitle("Mini Realisitc Airways");
            modal.SetHeading("Thanks for playing \"Mini Realisitc Airways\"! Before you start, please go over the following tutorial.");
            modal.SetDescription("For more information, refer to the <b><u><link=\"ENG\">Quick reference handbook</link></u></b>");
            modal.description.gameObject.AddComponent<LinkHandler>().url = "https://m0pt5uret4t.feishu.cn/docx/VURHdwhonozWZcxJAaHcG5tPnUg?from=from_copylink";
            modal.SetButtonText("中文");
            modal.SetButtonOnClick(() => {  
                showTutorialInEn = false;
                modal.SetTitle("真实迷你空管");
                modal.SetHeading("感谢您游玩\"真实迷你空管\"!\n在开始之前，请查看接下来的简易教程。");
                modal.SetDescription("请随时参阅<b><u><link=\"CHS\">快速参考手册</link></u></b>");
                modal.description.gameObject.GetComponent<LinkHandler>().url = "https://m0pt5uret4t.feishu.cn/docx/VaghdGDiEokiJmxeVRocJJonnhh?from=from_copylink";
            });
            modal.Show();
        }

        private static void ShowModTutorial()
        {
            modal = ModalManager.NewModalWithButtonStatic(PluginInfo.PLUGIN_GUID.ToString() + PluginInfo.PLUGIN_VERSION.ToString());
            SetTitle(modal);
            SetHeading(modal, enHeading[tutorialPage], cnHeading[tutorialPage]);
            SetDescription(modal , enDescription[tutorialPage], cnDescription[tutorialPage]);
            SetButton(modal);
            modal.Show();
        }

        private static void SetTitle(ModalWithButton modal)
        {
            modal.SetTitle(showTutorialInEn ? "Tutorial" : "教程");
        }

        private static void SetHeading(ModalWithButton modal, string en, string cn)
        {
            modal.SetHeading(showTutorialInEn ? en : cn);
        }

        private static void SetDescription(ModalWithButton modal, string en, string cn)
        {
            modal.SetDescription(showTutorialInEn ? en : cn);
        }

        private static void SetButton(ModalWithButton modal)
        {
            // Do not show NEXT on the last page.
            if (tutorialPage == enHeading.Count - 1)
            {
                modal.button.gameObject.SetActive(false);
            }
            modal.SetButtonText(showTutorialInEn ? "Next" : "下一页");
            modal.SetButtonOnClick(() => {
                ++tutorialPage;
                modal.PostHide();
                ShowModTutorial();
            });
        }

        private static ModalWithButton modal;
        private static bool showTutorialInEn = true;
        private static int tutorialPage = 0;
        private static List<string> enHeading = new List<string> {
            "Altitude",
            "Speed",
            "Type",
            "Events",
            "Wind",
            "Finally"
        };
        private static List<string> enDescription = new List<string> {
            @"Aircraft may be in one of the three altitudes, <b>low</b>, <b>normal</b>, and <b>high</b>. The current altitude of an aircraft is displayed on the left of aircraft icon and is displayed under aircraft as text.
            Aircraft arrives at <b>high</b> altitude and can only land when it is at <b>low</b> altitude.
            Aircraft takeoffs at <b>low</b> altitude and need to be at <b>normal</b> or <b>high</b> altitude when they reach departure waypoint.
            Terrain (red area) will not affect aircraft in <b>high</b> altitude.
            Press <b>W</b> or <b>Scroll Up</b> while hovering mouse over or commanding an aircraft will increase the altitude of aircraft/waypoint.
            Press <b>S</b> or <b>Scroll Down</b> while hovering mouse over or commanding an aircraft will decrease its the altitude of aircraft/waypoint.",
            @"Aircraft in the air may be in one of the three speeds,<b>slow</b>, <b>normal</b>, and <b>fast</b>. The current speed of an aircraft is displayed on the right of aircraft icon and is displayed under aircraft as text.
            Aircraft arrives at <b>normal</b> speed and can only land when it is at <b>slow</b> or <b>normal</b> speed.
            Aircraft lifts off at <b>normal</b> speed.
            Press <b>D</b> or hold <b>left shift</b> while <b>Scroll Up</b> while hovering mouse over or commanding an aircraft will increase the speed of aircraft/waypoint.
            Press <b>A</b> or hold <b>left shift</b> while <b>Scroll Down</b> while hovering mouse over or commanding an aircraft will decrease the speed of aircraft/waypoint.",
            @"Aircraft will have the following three types: <b>Light</b>, <b>Medium</b>, and <b>Heavy</b>. Each arrival aircraft type carries a different but limited amount of fuel. You can tell their remaining fuel level by the droplet-shaped fuel gauge located on the top-right of each arrival aircraft.
            <b>Light</b> aircraft's plane icon size is small. It will only have speed of <b>slow</b>, <b>normal</b>, and can only land with <b>slow</b>. It has 50% faster turning speed and accounts for 2.5% of all aircraft. <b>Light<b> aircraft has 3 in-game days worth of fuel.
            <b>Medium</b> aircraft has 3.5 in-game days worth of fuel.
            <b>Heavy</b> aircraft's plane icon size is large. It accounts for 30% of all random aircraft (arrival & departure) spawn and has 4 in-game days worth of fuel.",
            @"Sometimes, accidents do happen. These rare events show up on average every 6 days:
            Aircraft may arrive with emergency fuel, diverted from a nearby airport, and they need to land immediately.
            Runway excursion happened leading to a runway closure. The runway will be colored red, and all landing aircraft will automatically go around prior to touching down, and aircraft cannot take off from this runway. Note that if the stopped aircraft partially blocked another runway, it effectively closed the other one as well.
            Weather patterns can also show up in some areas, forcing all aircraft to go to <b>high</b> to avoid bad weather. If an aircraft enters the weather cell, it would count as a restricted area violation.
            Aircraft had suffered from an engine failure, you need to bring it back to the field immediately.",
            @"Wind can affect aircraft's takeoff / landing performance. When aircraft are landing in a tailwind, the go-around chance increases significantly. When aircraft takes off in a tailwind, the reject takeoff chance increases significantly. Wind direction is displayed as the arrow direction on the top-left corner of the screen. 
            When aircraft are landing/taking off with a full tailwind, the go-around/reject takeoff probability is very high.
            Reject takeoff / go-around probability drops to 0% when the wind direction is at or below 90 degrees of the runway (full cross-wind).",
            @"
            Use <b>Tab</b> to hide/show all the in-game text.
            When two aircraft are about to crash, TCAS will command one to climb and another to descend when possible. When aircraft is about to crash into terrain (Red), GPWS will command aircraft to climb. Landing aircraft are not commanded by TCAS or GPWS.
            You now starts with 3 waiting area upgrades.
            You now get upgrades twice as fast.
            Aircraft flying out-of-bound now count as restricted area violations instead of an instant game-over."
        };
        private static List<string> cnHeading = new List<string> {
            "高度",
            "速度",
            "机型",
            "特情",
            "风向",
            "最后",
        };
        private static List<string> cnDescription = new List<string> {
            @"飞机会处于以下三种高度：<b>低</b>、<b>中</b>和<b>高</b>。飞机的当前高度会在飞机图标左边以及下面的信息区显示：
            飞机会以<b>高</b>进场，且只有在<b>低</b>时才能降落。
            飞机将从<b>低</b>起飞，且只有再<b>中</b>或<b>高</b>到达离场航点时才能触发离场。
            地形（红色区域）不会影响<b>高</b>的飞机。
            在指挥飞机或鼠标悬浮于飞机上时按<b>W</b>或滚轮<b>scroll up</b>会增加飞机、航点高度。
            在指挥飞机或鼠标悬浮于飞机上时按<b>S</b>或滚轮<b>scroll down</b>会降低飞机、航点高度。",
            @"飞机会处于以下三种速度：<b>慢速</b>、<b>中速</b>和<b>快速</b>。飞机的当前速度会在飞机图标右边以及下面的信息区显示：
            飞机会以<b>中速</b>进场，且只有在<b>慢速</b>或<b>中速</b>时才能降落。
            飞机会以<b>中速</b>起飞。
            指挥飞机或鼠标悬浮于飞机上时按<b>D</b>或滚轮<b>scroll up</b>并按住<b>left shift</b>会增加飞机、航点速度。
            指挥飞机或鼠标悬浮于飞机上时按<b>A</b>或滚轮<b>scroll down</b>并按住<b>left shift</b>会降低飞机、航点速度。",
            @"飞机会属于以下三种机型：<b>轻型</b>、<b>中型</b>、<b>重型</b>，可通过图标大小判断。进场飞机会拥有燃油限制，可以通过右上角的水滴图标判断剩余燃料。
            <b>轻型</b>飞机最大速度为<b>中速</b>且只有在速度为<b>慢速</b>时才能降落。其转弯速度比其他类型快50%，且进场时携带游戏内3天的燃料。<b>轻型</b>飞机占所有飞机的2.5%。
            <b>中型</b>飞机进场时携带游戏内3.5天的燃料。
            <b>重型</b>飞机进场时携带游戏内4天的燃料，且占所有飞机的30%。",
            @"以下特情平均每6天发生一次：
            一架低油量飞机备降到本场，需要优先安排降落。
            跑道因各种原因关闭，此时跑道将变成红色，所有即将降落的飞机都会自动复飞，而且飞机也无法从该跑道起飞。要注意的是停在跑道上的飞机如果部分阻塞了另一条跑道，飞机降落到该跑道也会有碰撞判定。
            恶劣天气会使经过的飞机飞到<b>高</b>，飞机进入恶劣天气和飞入禁飞区拥有同样效果。
            一架离场飞机遭遇引擎故障，需要立即返场。",
            @"风向会影响飞机的起降性能。当飞机在顺风中降落时，复飞几率会提高。当飞机在顺风中起飞时，拒绝起飞几率也会提高。可以在屏幕左上角查看当前风向:
            当飞机顺风着陆、起飞时，复飞、中断起飞概率将变得非常高。
            当风向与跑道成 90 度（侧风）或以下时，中断起飞、复飞概率为零。",
            @"
            按<b>Tab</b>可以隐藏UI上的所有文字。
            当两架飞机即将碰撞时, TCAS会命令其中一架爬升，另一架下降。当飞机即将收到地形（红色区域）影响时，GWPS会命令飞机爬升。即将降落的飞机不受TCAS和GWPS影响。
            开场时自动获得3个等待区升级。
            升级现在每半天刷新一次。
            飞出屏幕和飞入禁飞区现在拥有同样效果。"
        };
    }
}
