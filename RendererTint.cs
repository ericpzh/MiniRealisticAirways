using UnityEngine;

namespace MiniRealisticAirways;

internal sealed class RendererTint
{
	private readonly Renderer renderer_;
	private readonly MaterialPropertyBlock original_ = new MaterialPropertyBlock();
	private readonly MaterialPropertyBlock tinted_ = new MaterialPropertyBlock();
	internal RendererTint(Renderer renderer)
	{
		renderer_ = renderer;
		if (renderer_ != null)
		{
			renderer_.GetPropertyBlock(original_);
			renderer_.GetPropertyBlock(tinted_);
		}
	}
	internal void Set(Color color)
	{
		if (renderer_ == null) return;
		tinted_.SetColor("_Color", color);
		renderer_.SetPropertyBlock(tinted_);
	}
	internal void Restore()
	{
		if (renderer_ != null) renderer_.SetPropertyBlock(original_);
	}
}
