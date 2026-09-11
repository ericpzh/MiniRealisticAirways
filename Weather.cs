using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways;

public class Weather : MonoBehaviour
{
	public List<Cell> cells_;

	public const float SIZE = 0.5f;

	public bool enabled_ = false;

	private const float MOVE_STEP = 0.005f;

	private Vector2 center_;

	private bool destroying_;

	// 全部网格的整体包围盒。InCell 先做 O(1) 判外，避免对每架飞机每物理帧
	// 线性扫描全部网格；网格会连续漂移偏离对齐，不能用网格坐标做哈希索引。
	private float boundsMinX_;

	private float boundsMinY_;

	private float boundsMaxX_;

	private float boundsMaxY_;

	private bool boundsValid_;

	public bool InCell(Vector2 position)
	{
		if (!enabled_ || cells_ == null || !boundsValid_)
		{
			return false;
		}
		if (position.x < boundsMinX_ || position.x > boundsMaxX_ || position.y < boundsMinY_ || position.y > boundsMaxY_)
		{
			return false;
		}
		foreach (Cell item in cells_)
		{
			if (position.x >= item.cell_.x && position.x <= item.cell_.x + SIZE && position.y >= item.cell_.y && position.y <= item.cell_.y + SIZE && item.enabled_)
			{
				return true;
			}
		}
		return false;
	}

	private void RecomputeBounds()
	{
		boundsValid_ = false;
		if (cells_ == null || cells_.Count == 0)
		{
			return;
		}
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		foreach (Cell item in cells_)
		{
			if (!item.enabled_) continue;
			if (item.cell_.x < minX) minX = item.cell_.x;
			if (item.cell_.y < minY) minY = item.cell_.y;
			if (item.cell_.x + SIZE > maxX) maxX = item.cell_.x + SIZE;
			if (item.cell_.y + SIZE > maxY) maxY = item.cell_.y + SIZE;
		}
		boundsMinX_ = minX;
		boundsMinY_ = minY;
		boundsMaxX_ = maxX;
		boundsMaxY_ = maxY;
		boundsValid_ = minX <= maxX && minY <= maxY;
	}

	public void DestroyWeather()
	{
		if (!destroying_)
		{
			destroying_ = true;
			StartCoroutine(DestroyWeatherCoroutine());
		}
	}

	private void EnqueueRandom(List<Vector2> directions, Queue<Cell> queue, Cell current)
	{
		Utils.Shuffle(directions);
		for (int i = 1; i < directions.Count; i++)
		{
			Vector2 cell = new Vector2(directions[i].x * SIZE + current.cell_.x, directions[i].y * SIZE + current.cell_.y);
			queue.Enqueue(new Cell(cell));
		}
	}

	private void GenerateCells()
	{
		center_ = new Vector2(Random.Range(-6f, 6f), Random.Range(-6f, 6f));
		Cell cell = new Cell(center_);
		cells_ = new List<Cell> { cell };
		// 去重键仅用于生成阶段，移动时不再持有可变坐标索引。
		HashSet<Vector2> occupied = new HashSet<Vector2> { cell.cell_ };
		List<Vector2> directions = new List<Vector2>
		{
			new Vector2(-1f, 0f),
			new Vector2(0f, -1f),
			new Vector2(0f, 1f),
			new Vector2(1f, 0f)
		};
		Queue<Cell> queue = new Queue<Cell>();
		EnqueueRandom(directions, queue, cell);
		int num = 0;
		while (cells_.Count < 60 && ++num < Plugin.MAX_WHILE_LOOP_ITER)
		{
			while (queue.Count > 0 && occupied.Contains(queue.Peek().cell_))
			{
				queue.Dequeue();
			}
			if (queue.Count == 0)
			{
				break;
			}
			cell = queue.Dequeue();
			EnqueueRandom(directions, queue, cell);
			cells_.Add(cell);
			occupied.Add(cell.cell_);
			if (num == Plugin.MAX_WHILE_LOOP_ITER - 1)
			{
				Plugin.Log.LogWarning("INF Loop in GenerateCells().");
			}
		}
		RecomputeBounds();
	}

	private int GetColor(Cell cell, float duration)
	{
		float num = Vector2.Distance(cell.cell_, center_);
		if (num < 0.6f && center_.x >= cell.cell_.x)
		{
			if ((double)duration > 0.8)
			{
				return WeatherCellTextures.GREEN;
			}
			if ((double)duration > 0.4)
			{
				return WeatherCellTextures.YELLOW;
			}
			return WeatherCellTextures.RED;
		}
		if ((num < 1f || (num < 1.5f && center_.y >= cell.cell_.y)) && (double)duration < 0.6)
		{
			return WeatherCellTextures.YELLOW;
		}
		return WeatherCellTextures.GREEN;
	}

	private void MoveCells(float x, float y, int step)
	{
		if (cells_ == null || WeatherCellTextures.textures_ == null)
		{
			return;
		}
		bool flag = false;
		foreach (Cell item in cells_)
		{
			if (!item.enabled_ || item.spriteRenderer_ == null || item.gameObject_ == null)
			{
				continue;
			}
			int color = GetColor(item, 0f);
			if (!flag && step > 6 && step % 2 == 0 && color == WeatherCellTextures.GREEN && (double)Random.value < 0.25)
			{
				item.spriteRenderer_.enabled = false;
				item.spriteRenderer_.sprite = null;
				item.enabled_ = false;
				flag = true;
				continue;
			}
			if (step == 12 || step == 18 || step == 24)
			{
				item.spriteRenderer_.sprite = WeatherCellTextures.GetSprite(9, color);
				item.spriteRenderer_.enabled = true;
			}
			// x/y 为本步增量；上游按步数递增，保留既有天气压迫节奏。
			item.cell_.x += x;
			item.cell_.y += y;
			item.gameObject_.transform.position = new Vector3(item.cell_.x, item.cell_.y, -9f);
		}
		RecomputeBounds();
	}

	private IEnumerator GenerateWeatherCoroutine()
	{
		WaitForSeconds frameDelay = new WaitForSeconds(2f);
		for (int i = 0; i < 10; i++)
		{
			if (cells_ == null || WeatherCellTextures.textures_ == null)
			{
				yield break;
			}
			foreach (Cell cell in cells_)
			{
				if (cell.spriteRenderer_ == null)
				{
					continue;
				}
				cell.spriteRenderer_.sprite = WeatherCellTextures.GetSprite(i, GetColor(cell, 0f));
				cell.spriteRenderer_.enabled = true;
			}
			yield return frameDelay;
		}
		enabled_ = true;
		int xDirection = Random.Range(-1, 2);
		int yDirection = Random.Range(-1, 2);
		int it = 0;
		while (true)
		{
			int num2;
			if (xDirection == 0 && yDirection == 0)
			{
				int num = it + 1;
				it = num;
				num2 = ((num < Plugin.MAX_WHILE_LOOP_ITER) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			if (num2 == 0)
			{
				break;
			}
			xDirection = Random.Range(-1, 2);
			yDirection = Random.Range(-1, 2);
			if (it == Plugin.MAX_WHILE_LOOP_ITER - 1)
			{
				Plugin.Log.LogWarning("INF Loop in GenerateWeatherCoroutine().");
			}
		}
		Plugin.Log.LogInfo("Moving weather towards (" + xDirection + ", " + yDirection + ")");
		WaitForSeconds moveDelay = new WaitForSeconds(2.1666667f);
		for (int j = 0; (float)j < 30f && cells_ != null; j++)
		{
			// 保留上游加速漂移：30 步单轴累计 MOVE_STEP × 435。
			MoveCells(MOVE_STEP * j * xDirection, MOVE_STEP * j * yDirection, j);
			yield return moveDelay;
		}
	}

	private IEnumerator DestroyWeatherCoroutine()
	{
		enabled_ = false;
		if (cells_ == null || WeatherCellTextures.textures_ == null)
		{
			CleanupCells();
			Object.Destroy(this);
			yield break;
		}
		WaitForSeconds fadeDelay = new WaitForSeconds(1f);
		for (int i = 9; i >= 0; i--)
		{
			foreach (Cell cell in cells_)
			{
				if (cell.enabled_ && cell.spriteRenderer_ != null)
				{
					cell.spriteRenderer_.sprite = WeatherCellTextures.GetSprite(i, GetColor(cell, 1f));
					cell.spriteRenderer_.enabled = true;
				}
			}
			yield return fadeDelay;
		}
		CleanupCells();
		Object.Destroy(this);
	}

	private void Start()
	{
		GenerateCells();
		foreach (Cell item in cells_)
		{
			GameObject gameObject = new GameObject("MiniRealisticAirways Weather Cell");
			gameObject.transform.position = new Vector3(item.cell_.x, item.cell_.y, -9f);
			item.gameObject_ = gameObject;
			SpriteRenderer spriteRenderer_ = gameObject.AddComponent<SpriteRenderer>();
			item.spriteRenderer_ = spriteRenderer_;
		}
		StartCoroutine(GenerateWeatherCoroutine());
	}

	private void CleanupCells()
	{
		if (cells_ == null)
		{
			return;
		}
		foreach (Cell cell in cells_)
		{
			if (cell.spriteRenderer_ != null)
			{
				cell.spriteRenderer_.sprite = null;
			}
			if (cell.gameObject_ != null)
			{
				Object.Destroy(cell.gameObject_);
				cell.gameObject_ = null;
			}
		}
		cells_.Clear();
		cells_ = null;
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		CleanupCells();
		if (EventManager.weather_ == this)
		{
			EventManager.weather_ = null;
		}
	}
}
