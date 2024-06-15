using System;
using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways
{
    public enum Weight
    {
        Light,
        Medium,
        Heavy
    }

    public class BaseAircraftType : MonoBehaviour
    {
        public Weight weight_;

        override public string ToString()
        {
            switch(weight_)
            {
                case Weight.Light:
                    return ".";
                case Weight.Medium:
                    return "-";
                case Weight.Heavy:
                    return "=";
            }
            return "";
        }

        public static Weight RandomWeight() 
        {
            float rand = UnityEngine.Random.value;
            if (rand <= 0.025f) 
            { // 2.5% Light aircrafts.
                return Weight.Light;
            } else if (rand >= 0.7f) 
            { // 30% Heavy aircrafts.
                return Weight.Heavy;
            }
            return Weight.Medium;
        }

        virtual public float GetScaleFactor()
        {
            switch (weight_)
            {
                case Weight.Light:
                    return 0.6f;
                case Weight.Medium:
                    return 1.25f;
                case Weight.Heavy:
                    return 2f;
            }
            return 1f;
        }
    }

    public class ActiveAircraftType : BaseAircraftType
    {
        public bool active_ = false;
    }

    public class AircraftType : BaseAircraftType
    {
        public IEnumerator FuelManagementCoroutine()
        {
            while (aircraft_ == null)
            {
                yield return new WaitForFixedUpdate();
            }

            fuelGauge_ = aircraft_.gameObject.AddComponent<FuelGauge>();
            fuelGauge_.aircraft_ = aircraft_;

            IEnumerator blinkCoroutine = null;

            float fuelOutTime = GetFuelTime();
            for (percentFuelLeft_ = 99; percentFuelLeft_ >= 0; percentFuelLeft_--)
            {
                if (percentFuelLeft_ <= LOW_FUEL_WARNING_PERCENT && blinkCoroutine == null && fuelGauge_.spriteRenderer_ != null)
                {
                    // Blink when fuel is slow.
                    blinkCoroutine = Animation.BlinkCoroutine(fuelGauge_.spriteRenderer_);
                    StartCoroutine(blinkCoroutine);
                }

                if (percentFuelLeft_ == LOW_FUEL_WARNING_PERCENT / 2 && blinkCoroutine != null && fuelGauge_.spriteRenderer_ != null)
                {
                    // Blink faster when fuel is super low.
                    if (blinkCoroutine != null)
                    {
                        StopCoroutine(blinkCoroutine);
                    }
                    blinkCoroutine = Animation.BlinkFastCoroutine(fuelGauge_.spriteRenderer_);
                    StartCoroutine(blinkCoroutine);
                }

                if (percentFuelLeft_ % (100 / FuelGaugeTextures.REFRESH_GRADIENT) == 0 && 
                    fuelGauge_.spriteRenderer_ != null && FuelGaugeTextures.fuelTextures_.Count >= percentFuelLeft_)
                {
                    // Re-render fuel gauge to update the fuel amount.
                    Destroy(fuelGauge_.spriteRenderer_.sprite);
                    fuelGauge_.spriteRenderer_.sprite = Sprite.Create(FuelGaugeTextures.fuelTextures_[percentFuelLeft_],
                                                                        FuelGaugeTextures.rect_, Vector2.zero);
                }

                yield return new WaitForSeconds(fuelOutTime / 100f);
            }

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                fuelGauge_.spriteRenderer_.enabled = true;
            }

            if (!IsTouchedDown())
            {
                // Using reflex for __instance.Invoke("AircraftTerrainGameOver", 0); will crash the game.
                // MethodInfo AircraftTerrainGameOver = __instance.GetType().GetMethod("AircraftTerrainGameOver", 
                //     BindingFlags.NonPublic | BindingFlags.Instance);
                // AircraftTerrainGameOver.Invoke(__instance, new object[] { __instance });
                LevelManager.Instance.CrashGameOver(aircraft_, null);
            }
        }

        public void PatchTurnSpeed()
        {
            if (weight_ == Weight.Light)
            {
                // Light aircraft turns faster.
                Aircraft.TurnSpeed *= LIGHT_TURN_FACTOR;
            }
        }

        public bool IsTakingOff() 
        {
            return aircraft_.state == Aircraft.State.TakingOff;
        }

        public bool IsTouchedDown() 
        {
            return aircraft_.state == Aircraft.State.TouchedDown;
        }

        override public float GetScaleFactor()
        {
            switch (weight_)
            {
                case Weight.Light:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) 
                    {
                        return 0.5f;
                    } 
                    else 
                    {
                        return 0.7f;
                    }
                case Weight.Medium:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) 
                    {
                        return 0.9f;
                        
                    } 
                    else 
                    {
                        return 1.1f;
                    }
                case Weight.Heavy:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) 
                    {
                        return 1.5f;
                        
                    } 
                    else 
                    {
                        return 1.5f;
                    }
            }
            return 1f;
        }

        private float TakeoffLandingProgress()
        {
            return Math.Min(1f + ON_GROUND_THRES, (Time.time - takeoffLandingStartTime_) / (Aircraft.TakeOffTime * Runway.MinimumRunwayLengthMultiplier));
        }

        private void UpdateSize()
        {
            if (IsTakingOff())
            {
                if (takeoffLandingStartTime_ == 0)
                {
                    takeoffLandingStartTime_  = Time.time;
                }
                else
                {
                    Vector3 scale = new Vector3(initScale_.x, initScale_.y, initScale_.z);
                    float progess = TakeoffLandingProgress();
                    if (progess < ON_GROUND_THRES)
                    {
                        // Constant SIZE.
                        aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor() * INIT_TAKEOFF_SCALE;
                    }
                    else
                    {
                        aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor() * (INIT_TAKEOFF_SCALE + (1f - INIT_TAKEOFF_SCALE) * (progess - ON_GROUND_THRES));
                    }
                }
            }
            else if (IsTouchedDown())
            {
                if (takeoffLandingStartTime_ == 0)
                {
                    takeoffLandingStartTime_  = Time.time;
                }
                else
                {
                    Vector3 scale = new Vector3(initScale_.x, initScale_.y, initScale_.z);
                    float progess = TakeoffLandingProgress();
                    if (progess > 1)
                    {
                        // Constant SIZE.
                        aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor() * finalLandingScale_;
                    }
                    else
                    {
                        aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor() * (finalLandingScale_ + (1f - finalLandingScale_) * (1 - progess));
                    }
                }
            }
            else
            {
                takeoffLandingStartTime_ = 0;
                Vector3 scale = new Vector3(initScale_.x, initScale_.y, initScale_.z);
                aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor();
            }
        }

        public float GetFuelTime()
        {
            float fuel = 1f;
            switch (weight_)
            {
                case Weight.Light:
                    fuel = 3f;
                    break;
                case Weight.Medium:
                    fuel = 3.5f;
                    break;
                case Weight.Heavy:
                    fuel = 4f;
                    break;
            }
            return fuel * 300f /* Time per clock round */;
        }

        public string GetFuelString()
        {
            if (percentFuelLeft_ >= 0)
            {
                return "Fuel: " + percentFuelLeft_ + "%";
            }
            return "Fuel: ∞";
        }

        private void Start()
        {
            if (aircraft_ == null)
            {
                return;
            }

            initScale_ = aircraft_.AP.gameObject.transform.localScale;
        }

        private void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }

            UpdateSize();
        }

        public Aircraft aircraft_;
        public bool windChecked_ = false;
        public Vector3 initScale_;
        public const float LIGHT_TURN_FACTOR = 1.5f;
        public float takeoffLandingStartTime_ = 0;
        public int percentFuelLeft_ = -1;
        public const int LOW_FUEL_WARNING_PERCENT = 30;
        private FuelGauge fuelGauge_;
        private const float ON_GROUND_THRES = 0.5f;
        private const float INIT_TAKEOFF_SCALE = 0.57f;
        private const float finalLandingScale_ = 0.5f;
    }
}