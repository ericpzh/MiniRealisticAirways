using DG.Tweening;
using UnityEngine;

namespace MiniRealisticAirways
{
    public enum Weight
    {
        Light,
        Medium,
        Heavy
    }

    public class AircraftType : Altitude
    {
        override public string ToString()
        {
            switch(weight_)
            {
                case Weight.Light:
                    return "v";
                case Weight.Medium:
                    return "—";
                case Weight.Heavy:
                    return "^";
            }
            return "";
        }

        public Aircraft aircraft_;
        public Weight weight_;
        public Vector3 initScale_;

        private bool IsTakingOff() 
        {
            return aircraft_.state == Aircraft.State.ReadyToTakeOff ||
                   aircraft_.state == Aircraft.State.LineUp ||
                   aircraft_.state == Aircraft.State.TakingOff;
        }

        private bool IsLanding() 
        {
            return aircraft_.state == Aircraft.State.Landing ||
                   aircraft_.state == Aircraft.State.TouchedDown;
        }

        private Weight RandomWeight() 
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
        
        private float GetScaleFactor()
        {
            switch (weight_)
            {
                case Weight.Light:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) 
                    {
                        if (IsLanding()) 
                        {
                            return 0.5f * 0.25f;
                        } 
                        else 
                        {
                            return 0.5f;
                        }
                    } 
                    else 
                    {
                        if (IsTakingOff()) 
                        {
                            return 0.57f * 0.7f;
                        }
                        else
                        {
                            return 0.7f;
                        }
                    }
                case Weight.Heavy:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) 
                    {
                        if (IsLanding()) 
                        {
                            return 1.25f * 0.25f;
                        } 
                        else 
                        {
                            return 1.25f;
                        }
                        
                    } 
                    else 
                    {
                        if (IsTakingOff())
                        {
                            return 1.75f * 0.7f;
                        }
                        else
                        {
                            return 1.75f;
                        }
                    }
            }
            return 1f;
        }

        void UpdateSize()
        {
            if (IsTakingOff())
            {
                // ShortcutExtensions.DOScale(aircraft_.AP.transform, GetScaleFactor(), Aircraft.TakeOffTime * Runway.MinimumRunwayLengthMultiplier);
            }
            else if (IsLanding())
            {
                // ShortcutExtensions.DOScale(aircraft_.AP.transform, GetScaleFactor(), 3f * Runway.MinimumRunwayLengthMultiplier);
            }
            else
            {
                Vector3 scale = new Vector3(initScale_.x, initScale_.y, initScale_.z);
                aircraft_.AP.gameObject.transform.localScale = scale * GetScaleFactor();
            }
        }

        private void Start()
        {
            initScale_ = aircraft_.AP.gameObject.transform.localScale;
            weight_ = RandomWeight();
        }

        private void Update()
        {
            if (aircraft_ == null)
            {
                Destroy(gameObject);
                return;
            }

            UpdateSize();

            if (weight_ == Weight.Light)
            {
                // Light aircraft has smaller turning radius.
                // aircraft_.SetFieldValue<float>("TurningRadius", aircraft_.TurningRadius / 5);

            }
            if (weight_ == Weight.Heavy)
            {
                // Light aircraft has smaller turning radius.
                // aircraft_.SetFieldValue<float>("TurningRadius", aircraft_.TurningRadius * 5);
            }
        }
    }
}