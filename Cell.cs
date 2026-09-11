using UnityEngine;

namespace MiniRealisticAirways;

public class Cell
{
	public Vector2 cell_;

	public GameObject gameObject_;

	public SpriteRenderer spriteRenderer_;

	public bool enabled_ = true;

	public Cell(Vector2 cell)
	{
		cell_ = cell;
	}
}
