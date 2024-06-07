# Mini Realistic Airways

A mini-realisitc mod for Mini Airways.

# How to Install

- Switch the game to `mod_feat - Mod` test version on steam.
- Install BepInEx into the game (https://docs.bepinex.dev/articles/user_guide/installation/index.html)
- Copy `MiniRealisticAirways.dll` into `<path_to_game>\Mini Airways Playtest\BepInEx\plugins`

# Altitude

Aircraft in the air may be in one of the three altitudes, low (`v`), normal (`—`), and high (`^`). The current altitude of an aircraft is displayed as: `ALT: ^`. Aircraft and upgrades interact with altitude in the following ways:
- Arrival aircraft from outside of the screen arrives at high (`^`) altitude.
- Arrival aircraft can only land when it is at low (`v`) altitude.
- Departure aircraft will start at low (`v`) altitude after take-off sequence finishes.
- Departure aircraft will only be able to reach departure (colored) waypoint at normal (`—`) or high (`^`) altitude.
- Landing waypoint will instruct aircraft to first reach low (`v`) altitude and issue the landing clearance.
- Terrain (Red) will not affect aircraft in high (`^`) altitude. Restricted area (yellow), however, will.

You can control the altitude of the aircraft by 
- Holding `W` while commanding an aircraft will increase its altitude, when animation is completed.
- Holding `S` while commanding an aircraft will decrease its altitude, when animation is completed.

# Speed

Aircraft in the air may be in one of the three speeds, slow (`<`), normal (`|`), and fast (`>`). The current speed of an aircraft is displayed as: `SPD: >`. Aircraft and upgrades interact with altitude in the following ways:
- Arrival aircraft from outside of the screen arrives at normal (`|`) speed.
- Arrival aircraft can land when it is in slow (`<`) or normal (`|`) speed.
- Departure aircraft will start at normal (`|`) speed after take-off sequence finishes.
- Landing waypoint will instruct aircraft to first reach normal (`|`) speed if the current speed is fast (`>`) and then issue the landing clearance.

You can control the altitude of the aircraft by 
- Holding `D` while commanding an aircraft will increase its speed.
- Holding `A` while commanding an aircraft will decrease its speed.

***
***

# 迷你真实空管

这是一个既迷你又真实的迷你空管Mod。

# 安装

- 右键库中的Mini Airways Playtest，属性 - 测试版 - mod_feat, 更新。
- 下载安装 BepInEx (https://docs.bepinex.dev/articles/user_guide/installation/index.html)。
- 复制 `MiniRealisticAirways.dll` 到 `<path_to_game>\Mini Airways Playtest\BepInEx\plugins`。

# 高度系统

飞机会处于以下三种高度：低（`v`）、正常（`—`）和高（`^`）。飞机的当前高度会显示为：`ALT: ^`。飞机有以下的高度特性：
- 屏幕外进场的飞机会以高（`^`）进场。
- 进场的飞机只有在低（`v`）时才能降落。
- 离场飞机将从低（`v`）起飞。
- 离场飞机只有正常（`—`）或高（`^`）到达离场（彩色）航路点时触发离场。
- 着陆航点将指示飞机首先到达低（`v`）并发出着陆许可。
- 地形（红色区域）不会影响高（`^`）的飞机。但是，禁区（黄色区域）会影响。

您可以通过以下方式控制飞机的高度
- 在指挥飞机时按住`W`会增加飞机的高度。
- 在指挥飞机时按住`S`会降低飞机的高度。

# 速度系统

空中的飞机会处于以下三种速度：慢速（`<`）、正常（`|`）和快速（`>`）。飞机的当前速度会显示为：`SPD: >`。飞机有以下的速度特性：
- 屏幕外进场的飞机会以正常（`|`）进场。
- 进场的飞机只有在慢速（`<`）或正常（`|`）时才能着陆。
- 离场飞机将以正常（`|`）起飞。
- 如果当前速度为快速（`>`），着陆航路点将指示飞机首先达到正常（`|`）并发出着陆许可。

您可以通过以下方式控制飞机的高度
- 指挥飞机时按住`D`会增加其速度。
- 指挥飞机时按住`A`会降低其速度。
