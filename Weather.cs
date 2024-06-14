using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRealisticAirways
{
    public class Cell
    {
        public Cell(Vector2 cell)
        {
            cell_ = cell;
        }

        public Vector2 cell_;
        public GameObject obj_;
        public SpriteRenderer spriteRenderer_;
    }

    public class CellComparer : IComparer<Cell> {
        public int Compare(Cell left, Cell right) {
            var dif = left.cell_ - right.cell_;
            if(dif.x == 0 && dif.y == 0)
                return 0;
            else if(dif.x == 0)
                return (int)Mathf.Sign(dif.y);
            return (int)Mathf.Sign(dif.x);
        }
    }

    public class Weather : MonoBehaviour
    {
        public bool InCell(Vector2 position)
        {
            if (!enabled_)
            {
                return false;
            }

            foreach(Cell cell in cells_)
            {
                if (position.x >= cell.cell_.x && position.x <= cell.cell_.x + SIZE &&
                    position.y >= cell.cell_.y && position.y <= cell.cell_.y + SIZE)
                {
                    return true;
                }
            }
            return false;
        }
        
        public void DestoryWeather()
        {
            StartCoroutine(DestoryWeatherCoroutine());
        }

        private void EnqueueRandom(ref List<Vector2> directions, ref Queue<Cell> queue, Cell current)
        {
            Utils.Shuffle<Vector2>(ref directions);
            for (int i = 1; i < directions.Count; i++)
            {
                // Chopping off one direction to get more randomness.
                Vector2 vector = new Vector2(directions[i].x * SIZE + current.cell_.x,
                                             directions[i].y * SIZE + current.cell_.y);
                queue.Enqueue(new Cell(vector));
            }
        }

        private void GenerateCells()
        {
            // Cell center.
            center_ = new Vector2(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(-6f, 6f));
            Cell current = new Cell(center_);
            cells_ = new SortedSet<Cell>(new CellComparer()){ current };

            List<Vector2> directions = new List<Vector2>{
                new Vector2(-1, 0), new Vector2(0, -1), new Vector2(0, 1), new Vector2(1, 0)};
            Queue<Cell> queue = new Queue<Cell>();
            EnqueueRandom(ref directions, ref queue, current);

            while (cells_.Count < GENERATE_NUMBER)
            {
                while (queue.Count > 0 && cells_.Contains(queue.Peek()))
                {
                    queue.Dequeue();
                }

                if (queue.Count == 0)
                {
                    return;
                }

                current = queue.Dequeue();
                EnqueueRandom(ref directions, ref queue, current);
                cells_.Add(current);
            }
        }

        private int GetColor(Cell cell)
        {
            if (center_ == null)
            {
                return 0;
            }

            float distance = Vector2.Distance(cell.cell_, center_);
            if (distance < 0.6f && center_.x >= cell.cell_.x)
            {
                return 0;
            }
            else if (distance < 1f || (distance < 1.5f && center_.y >= cell.cell_.y))
            {
                return 1;
            }
            return 2;
        }

        private IEnumerator GenerateWeatherCoroutine()
        {
            for (int i = 0; i < WeatherCellTextures.OPACITY_GRADIENT; i++)
            {
                foreach (Cell cell in cells_)
                {
                    Destroy(cell.spriteRenderer_.sprite);
                    cell.spriteRenderer_.sprite = Sprite.Create(WeatherCellTextures.textures_[i][GetColor(cell)],
                                                                WeatherCellTextures.rect_, Vector2.zero);
                    cell.spriteRenderer_.enabled = true;
                }

                yield return new WaitForSeconds(ENABLE_TIME / WeatherCellTextures.OPACITY_GRADIENT);
            }

            enabled_ = true;

            yield return new WaitForSeconds(EventManager.EVENT_RESTORE_TIME - DISABLE_TIME);
        }

        private IEnumerator DestoryWeatherCoroutine()
        {
            enabled_ = false;

            for (int i = WeatherCellTextures.OPACITY_GRADIENT - 1; i >= 0; i--)
            {
                foreach (Cell cell in cells_)
                {
                    if (cell.spriteRenderer_.sprite != null)
                    {
                        Destroy(cell.spriteRenderer_.sprite);
                    }
                    cell.spriteRenderer_.sprite = Sprite.Create(WeatherCellTextures.textures_[i][GetColor(cell)],
                                                                WeatherCellTextures.rect_, Vector2.zero);
                    cell.spriteRenderer_.enabled = true;
                }

                yield return new WaitForSeconds(DISABLE_TIME / WeatherCellTextures.OPACITY_GRADIENT);
            }

            foreach (Cell cell in cells_)
            {
                SpriteRenderer spriteRenderer = cell.spriteRenderer_;
                if (spriteRenderer != null)
                {
                    Destroy(spriteRenderer.sprite);
                }
            }

            Destroy(this);
        }

        private void Start()
        {
            GenerateCells();

            foreach(Cell cell in cells_)
            {
                if (cell.cell_ != null)
                {
                    GameObject obj = new GameObject();
                    obj.transform.position = new Vector3(cell.cell_.x, cell.cell_.y, -9f);
                    cell.obj_ = obj;

                    SpriteRenderer spriteRenderer = obj.AddComponent<SpriteRenderer>();
                    cell.spriteRenderer_ = spriteRenderer;
                }
            }

            StartCoroutine(GenerateWeatherCoroutine());
        }

        // Weather are list of continous square cells for size SIZE.
        public SortedSet<Cell> cells_;
        public const float SIZE = 0.5f;
        public bool enabled_ = false;
        private const float ENABLE_TIME = 2 * WeatherCellTextures.OPACITY_GRADIENT;
        private const int GENERATE_NUMBER = 60;
        private const float DISABLE_TIME = WeatherCellTextures.OPACITY_GRADIENT;
        private Vector2 center_;
    }

    public static class WeatherCellTextures
    {
        private static Texture2D DrawCell(Color color)
        {
            Texture2D texture = new Texture2D(SIZE, SIZE);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        public static void PreLoadTextures()
        {
            Plugin.Log.LogInfo("Pre-rendered weather cell textures.");
            colors_ = new List<Color>{new Color(1f, 0f, 0f, 0.1f),
                                      new Color(1f, 1f, 0f, 0.1f),
                                      new Color(0f, 1f, 0f, 0.1f)};

            textures_ = new List<List<Texture2D>>();
            for (int i = 1; i < OPACITY_GRADIENT + 1; i++)
            {
                List<Texture2D> textures = new List<Texture2D>();
                foreach (Color color in colors_)
                {
                    float opacity = (float)(i) / (float)OPACITY_GRADIENT * color.a;
                    textures.Add(DrawCell(new Color(color.r, color.g, color.b, opacity)));
                }
                textures_.Add(textures);
            }

            rect_ = new Rect(0, 0, SIZE, SIZE);
        }

        public static void DestoryTextures()
        {
            Plugin.Log.LogInfo("Weather cell textures destoried.");
            foreach (List<Texture2D> textures in textures_)
            {
                foreach (Texture2D texture in textures)
                {
                    Texture2D.Destroy(texture);
                }
            }
        }
        public static Rect rect_;
        public const int OPACITY_GRADIENT = 10;
        public static List<List<Texture2D>> textures_;
        private static List<Color> colors_;
        private const int SIZE = (int)(100 * Weather.SIZE);
    }
}