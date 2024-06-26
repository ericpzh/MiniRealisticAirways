using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiniRealisticAirways
{
    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }

        public static void SetFieldValue<T>(this object obj, string name, T value)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetType().GetField(name, bindingFlags);
            field.SetValue(obj, value);
        }
    }

    public static class Animation
    {
        public static IEnumerator BlinkCoroutine(SpriteRenderer spriteRenderer)
        {
            while (true)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSecondsRealtime(0.4f);
                spriteRenderer.enabled = true;
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        public static IEnumerator BlinkFastCoroutine(SpriteRenderer spriteRenderer)
        {
            while (true)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSecondsRealtime(0.2f);
                spriteRenderer.enabled = true;
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        public static bool Blink()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds() % 500 < 250;
        }

        public static bool BlinkLong()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds() % 500 < 100;
        }

        public static void SetPixel(bool cond, int x, int y, Color color, ref Texture2D texture)
        {
            if (cond)
            {
                texture.SetPixel(x, y, color);
            }
            else
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        public static Color gaugeColor = new Color(255, 255, 255, 0.5f);
    }

    public static class Utils
    {
        public static void Shuffle<T>(ref List<T> list)
        {   
            System.Random rng = new System.Random();
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T item = list[k];  
                list[k] = list[n];  
                list[n] = item;  
            }  
        }
    }
}