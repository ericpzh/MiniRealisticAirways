using System;
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
            if (rand <= 0.05f) 
            { // 5% Light aircrafts.
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

        public void PatchTurnSpeed()
        {
            if (weight_ == Weight.Light)
            {
                // Light aircraft turns faster.
                Aircraft.TurnSpeed *= LIGHT_TURN_FACTOR;
            }
        }

        private bool IsTakingOff() 
        {
            return aircraft_.state == Aircraft.State.TakingOff;
        }

        private bool IsTouchedDown() 
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
            return Math.Min(1f + onGroundPrecent_, (UnityEngine.Time.time - takeoffLandingStartTime_) / (Aircraft.TakeOffTime * Runway.MinimumRunwayLengthMultiplier));
        }

        private void UpdateSize()
        {
            if (IsTakingOff())
            {
                if (takeoffLandingStartTime_ == 0)
                {
                    takeoffLandingStartTime_  = UnityEngine.Time.time;
                }
                else
                {
                    Vector3 scale = new Vector3(initScale_.x, initScale_.y, initScale_.z);
                    float progess = TakeoffLandingProgress();
                    if (progess < onGroundPrecent_)
                    {
                        // Constant size.
                        aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor() * initTakeoffScale_;
                    }
                    else
                    {
                        aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor() * (initTakeoffScale_ + (1f - initTakeoffScale_) * (progess - onGroundPrecent_));
                    }
                }
            }
            else if (IsTouchedDown())
            {
                if (takeoffLandingStartTime_ == 0)
                {
                    takeoffLandingStartTime_  = UnityEngine.Time.time;
                }
                else
                {
                    Vector3 scale = new Vector3(initScale_.x, initScale_.y, initScale_.z);
                    float progess = TakeoffLandingProgress();
                    if (progess > 1)
                    {
                        // Constant size.
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

        private void Start()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
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
        public Vector3 initScale_;
        public const float LIGHT_TURN_FACTOR = 1.5f;
        public float takeoffLandingStartTime_ = 0;
        private const float onGroundPrecent_ = 0.5f;
        private const float initTakeoffScale_ = 0.57f;
        private const float finalLandingScale_ = 0.5f;
    }
}