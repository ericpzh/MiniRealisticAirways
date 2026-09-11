using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways;

public abstract class Gauge : MonoBehaviour
{
	private enum InitializationState
	{
		Uninitialized,
		Initializing,
		Ready,
		Failed
	}

	private const int MaxInitializationAttempts = 3;

	private const float InitializationRetryDelay = 1f;

	protected List<GameObject> gameObjects_;

	protected List<SpriteRenderer> spriteRenderers_;

	private InitializationState initializationState_;

	private int initializationAttempts_;

	private float nextInitializationAttemptTime_;

	private bool initializationWarningLogged_;

	public bool Ready()
	{
		return initializationState_ == InitializationState.Ready && spriteRenderers_ != null;
	}

	public bool InitializationFailed()
	{
		return initializationState_ == InitializationState.Failed && initializationAttempts_ >= MaxInitializationAttempts;
	}

	public void EnableSpriteRenderer(int count = 3)
	{
		if (spriteRenderers_ == null)
		{
			return;
		}
		for (int i = 0; i < Math.Min(spriteRenderers_.Count, count); i++)
		{
			spriteRenderers_[i].enabled = true;
		}
	}

	public void DisableSpriteRenderer()
	{
		if (spriteRenderers_ == null)
		{
			return;
		}
		foreach (SpriteRenderer item in spriteRenderers_)
		{
			item.enabled = false;
		}
	}

	protected bool Initialize()
	{
		return TryInitialize();
	}

	public bool TryInitialize()
	{
		if (Ready())
		{
			return true;
		}
		if (initializationState_ == InitializationState.Initializing || InitializationFailed())
		{
			return false;
		}
		if (initializationAttempts_ > 0 && Time.unscaledTime < nextInitializationAttemptTime_)
		{
			return false;
		}
		initializationAttempts_++;
		initializationState_ = InitializationState.Initializing;
		List<GameObject> createdGameObjects = new List<GameObject>(3);
		List<SpriteRenderer> createdSpriteRenderers = new List<SpriteRenderer>(3);
		try
		{
			Sprite arrowSprite = AcquireSprite();
			if (arrowSprite == null)
			{
				MarkInitializationFailed("Gauge initialization skipped because the arrow texture is unavailable.");
				return false;
			}
			for (int i = 0; i < 3; i++)
			{
				GameObject gameObject = new GameObject();
				createdGameObjects.Add(gameObject);
				SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
				spriteRenderer.sprite = arrowSprite;
				spriteRenderer.enabled = false;
				createdSpriteRenderers.Add(spriteRenderer);
			}
			gameObjects_ = createdGameObjects;
			spriteRenderers_ = createdSpriteRenderers;
			ConfigureRenderers();
			initializationState_ = InitializationState.Ready;
			return true;
		}
		catch (Exception exception)
		{
			DestroyCreatedObjects(createdGameObjects);
			gameObjects_ = null;
			spriteRenderers_ = null;
			MarkInitializationFailed("Gauge initialization failed: " + exception.GetBaseException());
			return false;
		}
	}

	// 每次初始化（包括重试）都必须完成挂载与布局后才能标记就绪。
	protected abstract void ConfigureRenderers();

	// 飞机/航点仪表的等级符号默认是箭头；改用实心方块的仪表覆盖此方法。
	protected virtual Sprite AcquireSprite()
	{
		return GaugeArrowTexture.GetSprite();
	}

	private void MarkInitializationFailed(string message)
	{
		initializationState_ = InitializationState.Failed;
		nextInitializationAttemptTime_ = Time.unscaledTime + InitializationRetryDelay;
		if (!initializationWarningLogged_)
		{
			initializationWarningLogged_ = true;
			Plugin.Log?.LogWarning(message);
		}
	}

	private static void DestroyCreatedObjects(List<GameObject> gameObjects)
	{
		if (gameObjects != null)
		{
			for (int i = 0; i < gameObjects.Count; i++)
			{
				if (gameObjects[i] != null)
				{
					UnityEngine.Object.Destroy(gameObjects[i]);
				}
			}
		}
	}

	protected IEnumerator TransitioningCoroutine(int level, int targetLevel)
	{
		// 闪烁语义：爬升闪目标箭头，下降闪正在失去的本级箭头。
		// 统一取较大一级，跨级（如 1→3）与 0↔1 组合也能命中正确箭头。
		int arrowIndex = Math.Clamp(Math.Max(level, targetLevel) - 1, 0, spriteRenderers_.Count - 1);
		return Animation.BlinkCoroutine(spriteRenderers_[arrowIndex]);
	}

	protected void UpdateGaugeSpriteRenderers(int level)
	{
		if (spriteRenderers_ != null && spriteRenderers_.Count >= 3)
		{
			for (int i = 0; i < 3; i++)
			{
				spriteRenderers_[i].enabled = i <= level;
			}
		}
	}

	private void OnDestroy()
	{
		if (spriteRenderers_ != null)
		{
			// GaugeArrowTexture owns the shared Sprite. Only the renderer and its
			// GameObject belong to this gauge instance.
			spriteRenderers_.Clear();
		}
		if (gameObjects_ != null)
		{
			for (int i = 0; i < gameObjects_.Count; i++)
			{
				if (gameObjects_[i] != null)
				{
					UnityEngine.Object.Destroy(gameObjects_[i]);
				}
			}
			gameObjects_.Clear();
		}
	}
}
