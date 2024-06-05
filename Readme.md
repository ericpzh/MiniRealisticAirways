# Mini Realistic Airways Mod


# How to Install


# Altitude

Aircraft in the air may be in one of the three altitude, low (`v`), mid (`-`), and high (`^`). The current altitude of an aircraft is displaied as part of call-sign as: `ALT: ^`. Aircraft and upgrades interact with altitude in the following ways:
- Arrival aircraft from outside of the screen arrives with high (`^`) altitude.
- Arrival aircraft can only commanded to land when it is in low (`v`) altitude.
- Departure aircraft will start in low (`v`) altitude after take-off sequence finishes.
- Departure (colored) waypoint is in high (`^`) altitude. Depature aircraft will only be able to reach the waypoint in high (`^`) altitude.
- Landing waypoint will instruct aircraft to first reach low (`v`) altitude and then issue the landing clearance.
- Terrain (Red) will not effect aircraft in high (`^`) altitude. Restricted area (yellow), however, will.

You can control the altitude of the aircraft by 
- Holding `W` while commanding an aircraft will increase it's altitude, when animation is completed.
- Holding `S` while commanding an aircraft will decrease it's altitude, when animation is completed.

# Speed

Aircraft in the air may be in one of the three speed, slow (`<`), mid (`|`), and fast (`>`). The current speed of an aircraft is displaied as part of call-sign as: `SPD: >`. Aircraft and upgrades interact with altitude in the following ways:
- Arrival aircraft from outside of the screen arrives with fast (`>`) speed.
- Arrival aircraft can only commanded to land when it is in slow (`<`) speed.
- Departure aircraft will start in slow (`<`) speed after take-off sequence finishes.
- Landing waypoint will instruct aircraft to first reach slow (`<`) speed and then issue the landing clearance.

You can control the altitude of the aircraft by 
- Holding `D` while commanding an aircraft will increase it's speed.
- Holding `A` while commanding an aircraft will decrease it's speed.

# Aircraft Type

As part of v2.0, the mod will introduce the concepts of aircraft types. Aircraft will have the following three types:

- Propeller
-- Will only have speed of slow (`<`), mid (`|`).
-- 5% of all random aircraft (arrival & departure) spwan.
-- As arrival, fuel level allow it to remain airborn for 2 in-game day.

- Heavy 
-- 30% of all random aircraft (arrival & departure) spwan.
-- Will generate wake turbulance, causing other aircraft type to go-around/refused to take-off when there is not enough spacing.
-- As arrival, fuel level allow it to remain airborn for 3 in-game day.

- Regular
-- 65% of all random aircraft (arrival & departure) spwan.
-- As arrival, fuel level allow it to remain airborn for 3 in-game day.
-- No extra effects.