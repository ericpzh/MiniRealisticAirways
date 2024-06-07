using HarmonyLib;
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

    public class AircraftType : Altitude
    {
        override public string ToString() {
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
        public Vector3 init_scale_;

        private Weight RandomWeight() {
            float rand = UnityEngine.Random.value;
            if (rand <= 0.05f) { // 5% Light aircrafts.
                return Weight.Light;
            } else if (rand >= 0.3f) { // 30% Heavy aircrafts.
                return Weight.Heavy;
            }
            return Weight.Medium;
        }
        
        void UpdateSize()
        {
            Vector3 scale_ = new Vector3(init_scale_.x, init_scale_.y, init_scale_.z);
            switch (weight_)
            {
                case Weight.Light:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) {
                        aircraft_.AP.gameObject.transform.localScale = scale_ * 0.5f;
                    } else {
                        aircraft_.AP.gameObject.transform.localScale = scale_ * 0.7f;
                    }
                    return;
                case Weight.Heavy:
                    if (aircraft_.direction == Aircraft.Direction.Inbound) {
                        aircraft_.AP.gameObject.transform.localScale = scale_ * 1.25f;
                    } else {
                        aircraft_.AP.gameObject.transform.localScale = scale_ * 1.75f;
                    }
                    return;
            }
        }

        private void Start()
        {
            init_scale_ = aircraft_.AP.gameObject.transform.localScale;
            weight_ = RandomWeight();
        }

        private void Update()
        {
            if (aircraft_ == null){
                Destroy(gameObject);
                return;
            }

            UpdateSize();
        }
    }
}