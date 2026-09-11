using System;
using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

public class AircraftType : BaseAircraftType
{
	private RendererTint emergencyAPTint_;
	private RendererTint emergencyPanelTint_;
	public Aircraft aircraft_;

	public bool windChecked_ = false;

	public Vector3 initScale_;

	public const float LIGHT_TURN_FACTOR = 1.5f;

	public float takeoffLandingStartTime_ = 0f;

	public int percentFuelLeft_ = -1;

	public const int LOW_FUEL_WARNING_PERCENT = 40;

	private FuelGauge fuelGauge_;

	private const float ON_GROUND_THRES = 0.5f;

	private const float INIT_TAKEOFF_SCALE = 0.57f;

	private const float finalLandingScale_ = 0.5f;

	private Vector3 lastAppliedScale_;

	private bool hasAppliedScale_;

	public IEnumerator FuelManagementCoroutine()
	{
		while (aircraft_ == null && this != null)
		{
			yield return new WaitForFixedUpdate();
		}
		if (aircraft_ == null)
		{
			yield break;
		}
		fuelGauge_ = aircraft_.GetComponent<FuelGauge>();
		if (fuelGauge_ == null)
		{
			fuelGauge_ = aircraft_.gameObject.AddComponent<FuelGauge>();
		}
		fuelGauge_.aircraft_ = aircraft_;
		IEnumerator blinkCoroutine = null;
		IEnumerator emergencyCoroutine = null;
		float fuelOutTime = GetFuelTime();
		WaitForSeconds fuelTickDelay = new WaitForSeconds(Mathf.Max(0.01f, fuelOutTime / 100f));
		while (aircraft_ != null && percentFuelLeft_ >= 0)
		{
			if (percentFuelLeft_ <= LOW_FUEL_WARNING_PERCENT && blinkCoroutine == null && fuelGauge_ != null && fuelGauge_.spriteRenderer_ != null)
			{
				blinkCoroutine = Animation.BlinkCoroutine(fuelGauge_.spriteRenderer_);
				StartCoroutine(blinkCoroutine);
				Plugin.Log.LogInfo("Fuel is low, started blinkCoroutine.");
			}
			if (percentFuelLeft_ <= 20 && blinkCoroutine != null && emergencyCoroutine == null && fuelGauge_ != null && fuelGauge_.spriteRenderer_ != null)
			{
				if (blinkCoroutine != null)
				{
					StopCoroutine(blinkCoroutine);
				}
				blinkCoroutine = Animation.BlinkFastCoroutine(fuelGauge_.spriteRenderer_);
				StartCoroutine(blinkCoroutine);
				emergencyCoroutine = EmergencyCoroutine(aircraft_);
				StartCoroutine(emergencyCoroutine);
				Plugin.Log.LogInfo("Fuel is super low, started emergencyCoroutine.");
			}
			if (percentFuelLeft_ >= 0 && fuelGauge_ != null && fuelGauge_.spriteRenderer_ != null && FuelGaugeTextures.fuelTextures_ != null && FuelGaugeTextures.fuelTextures_.Count > percentFuelLeft_)
			{
				Sprite fuelSprite = FuelGaugeTextures.GetSprite(percentFuelLeft_);
				if (fuelSprite != null)
				{
					fuelGauge_.spriteRenderer_.sprite = fuelSprite;
				}
			}
			yield return fuelTickDelay;
			if (percentFuelLeft_ == 0) break;
			percentFuelLeft_--;
		}
		if (blinkCoroutine != null)
		{
			StopCoroutine(blinkCoroutine);
			if (fuelGauge_ != null && fuelGauge_.spriteRenderer_ != null)
			{
				fuelGauge_.spriteRenderer_.enabled = true;
			}
		}
		if (emergencyCoroutine != null)
		{
			StopCoroutine(emergencyCoroutine);
		}
		if (aircraft_ != null && !IsTouchedDown() && LevelManager.Instance != null)
		{
			LevelManager.Instance.CrashGameOver(aircraft_, null);
		}
	}

	public IEnumerator DisableFuelGaugeCoroutine()
	{
		while (aircraft_ != null && (fuelGauge_ == null || fuelGauge_.spriteRenderer_ == null))
		{
			yield return new WaitForFixedUpdate();
		}
		if (aircraft_ != null && fuelGauge_ != null && fuelGauge_.spriteRenderer_ != null)
		{
			fuelGauge_.spriteRenderer_.enabled = false;
		}
	}


	public bool IsTakingOff()
	{
		return aircraft_ != null && aircraft_.state == Aircraft.State.TakingOff;
	}

	public bool IsTouchedDown()
	{
		return aircraft_ != null && aircraft_.state == Aircraft.State.TouchedDown;
	}

	public void UpdateSprite()
	{
		if (aircraft_ == null || aircraft_.AP == null)
		{
			return;
		}
		if (weight_ == Weight.Heavy && EventManager.b747Sprite_ != null)
		{
			SpriteRenderer spriteRenderer = aircraft_.AP.GetComponent<SpriteRenderer>();
			if (!(spriteRenderer == null))
			{
				spriteRenderer.sprite = EventManager.b747Sprite_;
			}
		}
		else if (weight_ == Weight.Light && EventManager.f16Sprite_ != null)
		{
			SpriteRenderer spriteRenderer2 = aircraft_.AP.GetComponent<SpriteRenderer>();
			if (!(spriteRenderer2 == null))
			{
				spriteRenderer2.sprite = EventManager.f16Sprite_;
			}
		}
	}

	public override float GetScaleFactor()
	{
		switch (weight_)
		{
		case Weight.Light:
			if (aircraft_.direction == Aircraft.Direction.Inbound)
			{
				return 0.5f;
			}
			return 0.7f;
		case Weight.Medium:
			if (aircraft_.direction == Aircraft.Direction.Inbound)
			{
				return 0.9f;
			}
			return 1.1f;
		case Weight.Heavy:
			if (aircraft_.direction == Aircraft.Direction.Inbound)
			{
				return 1.5f;
			}
			return 1.7f;
		default:
			return 1f;
		}
	}

	private IEnumerator EmergencyCoroutine(Aircraft aircraft)
	{
		if (aircraft == null || aircraft.AP == null || aircraft.Panel == null)
		{
			yield break;
		}
		Renderer apRenderer = aircraft.AP.GetComponent<Renderer>();
		Renderer panelRenderer = aircraft.Panel.GetComponent<Renderer>();
		if (apRenderer == null || panelRenderer == null)
		{
			yield break;
		}
		emergencyAPTint_ = new RendererTint(apRenderer);
		emergencyPanelTint_ = new RendererTint(panelRenderer);
		if (aircraft.callsignStatText != null)
		{
			aircraft.callsignStatText.gameObject.SetActive(value: true);
			aircraft.callsignStatText.text = "EM";
			aircraft.callsignStatText.color = Color.red;
		}
		while (aircraft != null && this != null)
		{
			emergencyAPTint_.Set(Color.red);
			emergencyPanelTint_.Set(Color.red);
			yield return new WaitForSecondsRealtime(0.2f);
			if (aircraft == null)
			{
				yield break;
			}
			emergencyAPTint_.Restore();
			emergencyPanelTint_.Restore();
			yield return new WaitForSecondsRealtime(0.2f);
		}
	}

	private float TakeoffLandingProgress()
	{
		float duration = Aircraft.TakeOffTime * Runway.MinimumRunwayLengthMultiplier;
		return duration > 0f ? Math.Min(1.5f, (Time.time - takeoffLandingStartTime_) / duration) : 1.5f;
	}

	private void UpdateSize()
	{
		if (aircraft_ == null || aircraft_.AP == null)
		{
			return;
		}
		if (IsTakingOff())
		{
			if (takeoffLandingStartTime_ == 0f)
			{
				takeoffLandingStartTime_ = Time.time;
				return;
			}
			Vector3 vector = new Vector3(initScale_.x, initScale_.y, initScale_.z);
			float num = TakeoffLandingProgress();
			if (num < 0.5f)
			{
				ApplyAircraftScale(vector * GetScaleFactor() * 0.57f);
			}
			else
			{
				ApplyAircraftScale(vector * GetScaleFactor() * (0.57f + 0.43f * (num - 0.5f)));
			}
		}
		else if (IsTouchedDown())
		{
			if (takeoffLandingStartTime_ == 0f)
			{
				takeoffLandingStartTime_ = Time.time;
				return;
			}
			Vector3 vector2 = new Vector3(initScale_.x, initScale_.y, initScale_.z);
			float num2 = TakeoffLandingProgress();
			if (num2 > 1f)
			{
				ApplyAircraftScale(vector2 * GetScaleFactor() * 0.5f);
			}
			else
			{
				ApplyAircraftScale(vector2 * GetScaleFactor() * (0.5f + 0.5f * (1f - num2)));
			}
		}
		else
		{
			takeoffLandingStartTime_ = 0f;
			Vector3 vector3 = new Vector3(initScale_.x, initScale_.y, initScale_.z);
			ApplyAircraftScale(vector3 * GetScaleFactor());
		}
	}

	private void ApplyAircraftScale(Vector3 targetScale)
	{
		if (aircraft_ == null || aircraft_.AP == null)
		{
			return;
		}
		Transform transform = aircraft_.AP.transform;
		if (!hasAppliedScale_ || (lastAppliedScale_ - targetScale).sqrMagnitude > 0.00000001f || (transform.localScale - targetScale).sqrMagnitude > 0.00000001f)
		{
			transform.localScale = targetScale;
			lastAppliedScale_ = targetScale;
			hasAppliedScale_ = true;
		}
	}

	public float GetFuelTime()
	{
		float num = 1f;
		switch (weight_)
		{
		case Weight.Light:
			num = 3f;
			break;
		case Weight.Medium:
			num = 3.5f;
			break;
		case Weight.Heavy:
			num = 4f;
			break;
		}
		return num * 300f;
	}

	public string GetFuelString()
	{
		return ModLocalization.GetFuelString(percentFuelLeft_);
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		emergencyAPTint_?.Restore();
		emergencyPanelTint_?.Restore();
	}

	private void Start()
	{
		if (aircraft_ != null && aircraft_.AP != null)
		{
			initScale_ = aircraft_.AP.gameObject.transform.localScale;
		}
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		if (aircraft_ == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		UpdateSize();
		if (AircraftState.DisableStateOnTouchedDown(aircraft_) && fuelGauge_ != null && fuelGauge_.spriteRenderer_ != null)
		{
			fuelGauge_.spriteRenderer_.enabled = false;
		}
	}
}
