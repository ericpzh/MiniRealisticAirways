# Mini Realistic Airways

A mini-realisitc mod for Mini Airways.

# How to Install

- Switch the game to `mod_feat - Mod` test version on steam.
- Install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) into the game
- Copy [MiniRealisticAirways.dll](https://github.com/ericpzh/MiniRealisticAirways/blob/main/bin/Debug/netstandard2.1/MiniRealisticAirways.dll) into `<path_to_game>\Mini Airways Playtest\BepInEx\plugins`

# Altitude

Aircraft in the air may be in one of the three altitudes, low (`v`), normal (`—`), and high (`^`). The current altitude of an aircraft is displayed as: `ALT: ^`. Aircraft and upgrades interact with altitude in the following ways:
- Arrival aircraft from outside of the screen arrives at high (`^`) altitude.
- Arrival aircraft can only land when it is at low (`v`) altitude.
- Departure aircraft will start at low (`v`) altitude after take-off sequence finishes.
- Departure aircraft will only be able to reach departure (colored) waypoint at normal (`—`) or high (`^`) altitude.
- Landing waypoint will instruct aircraft to first reach low (`v`) altitude and issue the landing clearance.
- Terrain (Red) will not affect aircraft in high (`^`) altitude. Restricted area (yellow), however, will.

You can control the altitude of the aircraft by:
- Press `W` while hovering mouse over or commanding an aircraft will increase its altitude, when animation is completed.
- Press `S` while hovering mouse over or commanding an aircraft will decrease its altitude, when animation is completed.
- `Scroll Up` while hovering mouse over an aircraft will increase its altitude, when animation is completed.
- `Scroll Down` while hovering mouse over an aircraft will decrease its altitude, when animation is completed.

Waypoint command aircraft's altitude. You can control the altitude of the waypoint by:
- Press `W` or `Scroll Up` and holding a waypoint will increase its altitude.
- Press `S` or `Scroll Down` and holding a waypoint will decrease its altitude.

# Speed

Aircraft in the air may be in one of the three speeds, slow (`<`), normal (`|`), and fast (`>`). The current speed of an aircraft is displayed as: `SPD: >`. Aircraft and upgrades interact with altitude in the following ways:
- Arrival aircraft from outside of the screen arrives at normal (`|`) speed.
- Arrival aircraft can land when it is in slow (`<`) or normal (`|`) speed.
- Arrival aircraft going around will lift-off with normal (`|`) speed.
- Departure aircraft will start at normal (`|`) speed after take-off sequence finishes.
- Landing waypoint will instruct aircraft to first reach normal (`|`) speed if the current speed is fast (`>`) and then issue the landing clearance.

You can control the altitude of the aircraft by:
- Press `D` while hovering mouse over or commanding an aircraft will increase its speed, when animation is completed.
- Press `A` while hovering mouse over or commanding an aircraft will decrease its speed, when animation is completed.
- Hold `left shift` while `Scroll Up` and hovering mouse over an aircraft will increase its speed, when animation is completed.
- Hold `left shift` while `Scroll Down` and hovering mouse over an aircraft will decrease its speed, when animation is completed.

Waypoint can command aircraft's speeds. You can control the altitude of the waypoint by:
- Press `D` or hold `left shift` and `Scroll Up` while holding a waypoint will increase its speed.
- Press `A` or hold `left shift` and `Scroll Down` while holding a waypoint will decrease its speed.

# Aircraft Type

Aircraft will have the following three types: Light, Medium, and Heavy.

Light aircraft have the following behavior:
- Plane icon size is small.
- Will only have speed of slow (`<`), normal (`|`). If passing through a waypoint with fast (`>`), it will only go up to normal (`|`).
- 5% of all random aircraft (arrival & departure) spawn.

Heavy aircraft have the following behavior:
- Plane icon size is large.
- 30% of all random aircraft (arrival & departure) spawn.

Medium aircraft have the following behavior:
- 65% of all random aircraft (arrival & departure) spawn.

# Other Changes
- You now get upgrades twice as fast.
- You now get 2 waypoints per upgrade selection. (x)

***

# 迷你真实空管

这是一个既迷你又真实的迷你空管Mod。

# 安装

- 右键库中的Mini Airways Playtest，属性 - 测试版 - mod_feat, 更新。
- 下载安装 [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html)。
- 复制 [MiniRealisticAirways.dll](https://github.com/ericpzh/MiniRealisticAirways/blob/main/bin/Debug/netstandard2.1/MiniRealisticAirways.dll) 到 `<path_to_game>\Mini Airways Playtest\BepInEx\plugins`。

# 高度系统

飞机会处于以下三种高度：低（`v`）、正常（`—`）和高（`^`）。飞机的当前高度会显示为：`ALT: ^`。飞机有以下的高度特性：
- 屏幕外进场的飞机会以高（`^`）进场。
- 进场的飞机只有在低（`v`）时才能降落。
- 离场飞机将从低（`v`）起飞。
- 离场飞机只有正常（`—`）或高（`^`）到达离场（彩色）航点时触发离场。
- 降落航点将指示飞机首先到达低（`v`）并发出降落许可。
- 地形（红色区域）不会影响高（`^`）的飞机。但是，限制区（黄色区域）会影响。

可以通过以下方式控制飞机的高度:
- 在指挥飞机或鼠标悬浮于飞机上时按`W`会增加其高度。
- 在指挥飞机或鼠标悬浮于飞机上时按`S`会降低其高度。
- 在鼠标悬浮于飞机上时滚轮`scroll up`会增加其高度。
- 在鼠标悬浮于飞机上时滚轮`scroll down`会降低其高度。

航点可以控制飞机改变高度：
- 在放置航点时按`W`或滚轮`scroll up`会增加其高度。
- 在放置航点时按`S`或滚轮`scroll down`会降低其高度。

# 速度系统

飞机会处于以下三种速度：慢速（`<`）、正常（`|`）和快速（`>`）。飞机的当前速度会显示为：`SPD: >`。飞机有以下的速度特性：
- 屏幕外进场的飞机会以正常（`|`）进场。
- 进场的飞机只有在慢速（`<`）或正常（`|`）时才能降落。
- 复飞的飞机会以正常（`|`）起飞。
- 离场飞机将以正常（`|`）起飞。
- 如果当前速度为快速（`>`），降落航点将指示飞机首先达到正常（`|`）并发出降落许可。

可以通过以下方式控制飞机的速度:
- 指挥飞机或鼠标悬浮于飞机上时按`D`会增加其速度。
- 指挥飞机或鼠标悬浮于飞机上时按`A`会降低其速度。
- 在鼠标悬浮于飞机上时滚轮`scroll up`并按住`left shift`会增加其速度。
- 在鼠标悬浮于飞机上时滚轮`scroll down`并按住`left shift`会降低其速度。

航点可以控制飞机改变速度：
- 在放置航点时按`D`或滚轮`scroll up`并按住`left shift`会增加其速度。
- 在放置航点时按`A`或滚轮`scroll down`并按住`left shift`会降低其速度。

# 机型系统

飞机会属于以下三种机型：轻、中、重。

轻型飞机拥有以下特性:
- 飞机图标尺寸变小。
- 最大速度为正常（`|`）。如果通过具有快速（`>`）的航点，速度也只会变为正常（`|`）。
- 占所有飞机的5%。

重型飞机具有以下特性：
- 飞机图标尺寸较大。
- 占所有飞机的30%。

中型飞机具有以下特性：
- 占所有飞机的65%。

# 其他特性
- 升级现在每半天刷新一次。