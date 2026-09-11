using UnityEngine;

namespace MiniRealisticAirways;

public class FuelGauge : MonoBehaviour
{
	public Aircraft aircraft_;

	private GameObject gameObject_;

	public SpriteRenderer spriteRenderer_;

	private void Start()
	{
		if (aircraft_ != null && FuelGaugeTextures.fuelTextures_ != null && FuelGaugeTextures.fuelTextures_.Count > 100)
		{
			gameObject_ = new GameObject();
			gameObject_.transform.SetParent(aircraft_.transform);
			gameObject_.transform.localPosition = new Vector3(1f, 1f, -5f);
			spriteRenderer_ = gameObject_.AddComponent<SpriteRenderer>();
			spriteRenderer_.sprite = FuelGaugeTextures.GetSprite(100);
			if (spriteRenderer_.sprite == null)
			{
				Object.Destroy(gameObject_);
				gameObject_ = null;
				spriteRenderer_ = null;
			}
		}
	}

	private void OnDestroy()
	{
		if (gameObject_ != null)
		{
			Object.Destroy(gameObject_);
		}
		spriteRenderer_ = null;
		gameObject_ = null;
	}
}
