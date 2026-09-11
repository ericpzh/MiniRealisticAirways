using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways;

public class AircraftState : MonoBehaviour
{
	private static readonly Dictionary<Aircraft, AircraftState> states_ = new Dictionary<Aircraft, AircraftState>();

	public AircraftAltitude aircraftAltitude_;

	public AircraftSpeed aircraftSpeed_;

	public AircraftType aircraftType_;

	public Aircraft aircraft_;

	public PlaceableWaypoint commandingWaypoint_;

	public bool weatherAffected_ = false;

	private TMP_Text altitudeText_;

	private TMP_Text speedText_;

	private TMP_Text fuelText_;

	private TMP_Text weightText_;

	private bool textCacheInitialized_;

	private bool lastTextVisible_;

	private bool lastBlinkPhase_;

	private bool lastTransitioning_;

	private AltitudeLevel lastAltitude_;

	private AltitudeLevel lastTargetAltitude_;

	private SpeedLevel lastSpeed_;

	private SpeedLevel lastTargetSpeed_;

	private int lastFuel_;

	private Weight lastWeight_;

	private int lastLocaleRevision_ = -1;

	private AircraftHudLayoutController hudLayout_;

	public static bool DisableStateOnTouchedDown(Aircraft aircraft)
	{
		return aircraft != null && aircraft.direction == Aircraft.Direction.Inbound && aircraft.state == Aircraft.State.TouchedDown;
	}

	public static bool GetAircraftState(Aircraft aircraft, out AircraftState aircraftState)
	{
		aircraftState = null;
		if (aircraft == null) return false;
		if (states_.TryGetValue(aircraft, out aircraftState) && aircraftState != null) return true;
		aircraftState = aircraft.GetComponent<AircraftState>();
		if (aircraftState != null) states_[aircraft] = aircraftState;
		else states_.Remove(aircraft);
		return aircraftState != null;
	}

	public static bool GetAircraftStates(Aircraft aircraft, out AircraftAltitude aircraftAltitude, out AircraftSpeed aircraftSpeed, out AircraftType aircraftType)
	{
		aircraftAltitude = null;
		aircraftSpeed = null;
		aircraftType = null;
		if (!GetAircraftState(aircraft, out var aircraftState))
		{
			return false;
		}
		aircraftAltitude = aircraftState.aircraftAltitude_;
		aircraftSpeed = aircraftState.aircraftSpeed_;
		aircraftType = aircraftState.aircraftType_;
		return aircraftAltitude != null && aircraftSpeed != null && aircraftType != null;
	}

	public void Initialize()
	{
		if (!(aircraft_ == null))
		{
			states_[aircraft_] = this;
			aircraftAltitude_ = aircraft_.GetComponent<AircraftAltitude>();
			if (aircraftAltitude_ == null)
			{
				aircraftAltitude_ = aircraft_.gameObject.AddComponent<AircraftAltitude>();
			}
			aircraftAltitude_.aircraft_ = aircraft_;
			aircraftSpeed_ = aircraft_.GetComponent<AircraftSpeed>();
			if (aircraftSpeed_ == null)
			{
				aircraftSpeed_ = aircraft_.gameObject.AddComponent<AircraftSpeed>();
			}
			aircraftSpeed_.aircraft_ = aircraft_;
			aircraftType_ = aircraft_.GetComponent<AircraftType>();
			if (aircraftType_ == null)
			{
				aircraftType_ = aircraft_.gameObject.AddComponent<AircraftType>();
			}
			aircraftType_.aircraft_ = aircraft_;
		}
	}

	public IEnumerator DelayDestroyCoroutine()
	{
		yield return new WaitForSeconds(5f);
		if (aircraft_ != null)
		{
			aircraft_.ConditionalDestroy();
		}
	}

	public bool IsAirborne()
	{
		if (aircraftAltitude_ == null)
		{
			return false;
		}
		return aircraftAltitude_.altitude_ > AltitudeLevel.Ground;
	}

	private void StartText(ref TMP_Text text, float fontSize, float x, float y, float z)
	{
		GameObject gameObject = new GameObject("Text");
		text = gameObject.AddComponent<TextMeshPro>();
		text.fontSize = fontSize;
		text.horizontalAlignment = HorizontalAlignmentOptions.Left;
		text.verticalAlignment = VerticalAlignmentOptions.Top;
		text.rectTransform.sizeDelta = new Vector2(2f, 1f);
		gameObject.transform.SetParent(aircraft_.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(x, y, z);
	}

	private void Start()
	{
		if (!(aircraft_ == null))
		{
			Initialize();
			if (aircraftAltitude_ == null)
			{
				return;
			}
			StartText(ref altitudeText_, 2f, 0.4f, -3.6f, 5f);
			StartText(ref speedText_, 2f, 2.5f, -3.6f, 5f);
			StartText(ref fuelText_, 2f, 0f, 0f, 5f);
			StartText(ref weightText_, 2f, 0f, 0f, 5f);
			AircraftVisualSortingController visualSorting = AircraftVisualSortingController.GetOrCreate(aircraft_);
			if (visualSorting != null)
			{
				visualSorting.Initialize(aircraft_, aircraftAltitude_, altitudeText_, speedText_, fuelText_, weightText_);
			}
			hudLayout_ = AircraftHudLayoutController.GetOrCreate(aircraft_);
			if (hudLayout_ != null)
			{
				hudLayout_.Initialize(aircraft_, altitudeText_, speedText_, fuelText_, weightText_, visualSorting);
			}
		}
	}

	private void Update()
	{
		if (aircraft_ == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else if (!(altitudeText_ == null) && !(speedText_ == null) && !(weightText_ == null) && !(fuelText_ == null) && aircraftAltitude_ != null && aircraftSpeed_ != null && aircraftType_ != null)
		{
			AircraftAltitude aircraftAltitude = aircraftAltitude_;
			AircraftSpeed aircraftSpeed = aircraftSpeed_;
			AircraftType aircraftType = aircraftType_;
			bool airborne = IsAirborne();
			bool visible = Plugin.showText_ && !DisableStateOnTouchedDown(aircraft_) && airborne;
			bool transitioning = aircraftAltitude.altitude_ != aircraftAltitude.targetAltitude_ || Math.Abs(aircraft_.speed - aircraft_.targetSpeed) > Speed.SPEED_DELTA;
			bool blinkPhase = transitioning && Animation.Blink();
			if (textCacheInitialized_ && lastLocaleRevision_ == ModLocalization.Revision && visible == lastTextVisible_ && (!transitioning || (transitioning == lastTransitioning_ && blinkPhase == lastBlinkPhase_)) && (!visible || (aircraftAltitude.altitude_ == lastAltitude_ && aircraftAltitude.targetAltitude_ == lastTargetAltitude_ && aircraftSpeed.GetSpeed() == lastSpeed_ && Speed.ToModSpeed(aircraft_.targetSpeed) == lastTargetSpeed_ && aircraftType.percentFuelLeft_ == lastFuel_ && aircraftType.weight_ == lastWeight_)))
			{
				return;
			}
			textCacheInitialized_ = true;
			lastTextVisible_ = visible;
			lastBlinkPhase_ = blinkPhase;
			lastTransitioning_ = transitioning;
			lastAltitude_ = aircraftAltitude.altitude_;
			lastTargetAltitude_ = aircraftAltitude.targetAltitude_;
			lastSpeed_ = aircraftSpeed.GetSpeed();
			lastTargetSpeed_ = Speed.ToModSpeed(aircraft_.targetSpeed);
			lastFuel_ = aircraftType.percentFuelLeft_;
			lastWeight_ = aircraftType.weight_;
			lastLocaleRevision_ = ModLocalization.Revision;
			if (!visible)
			{
				SetText(altitudeText_, "");
				SetText(speedText_, "");
				SetText(fuelText_, "");
				SetText(weightText_, "");
			}
			else
			{
				// Catalogue entries from earlier versions included a trailing blank.
				// Keep spacing under the layout controller's value-column control so a
				// localized prefix cannot double the gap before the solid blocks.
				SetText(altitudeText_, ModLocalization.Get("hud.altitudePrefix").TrimEnd());
				SetText(speedText_, ModLocalization.Get("hud.speedPrefix").TrimEnd());
				SetText(fuelText_, aircraftType.GetFuelString());
				SetText(weightText_, ModLocalization.Format("hud.weight", ModLocalization.GetWeightName(aircraftType.weight_)));
			}
			if (hudLayout_ != null)
			{
				hudLayout_.Refresh(visible, aircraftAltitude.altitude_, aircraftSpeed.GetSpeed());
			}
		}
	}

	private static void SetText(TMP_Text text, string value)
	{
		if (text != null && text.text != value)
		{
			ChineseTypography.SetHudText(text, value, "Aircraft HUD " + text.name);
		}
	}

	private void OnDestroy()
	{
		if (!ReferenceEquals(aircraft_, null) && states_.TryGetValue(aircraft_, out var cached) && ReferenceEquals(cached, this)) states_.Remove(aircraft_);
		DestroyText(ref altitudeText_);
		DestroyText(ref speedText_);
		DestroyText(ref fuelText_);
		DestroyText(ref weightText_);
		hudLayout_ = null;
	}

	private static void DestroyText(ref TMP_Text text)
	{
		if (text != null)
		{
			UnityEngine.Object.Destroy(text.gameObject);
		}
		text = null;
	}
}
