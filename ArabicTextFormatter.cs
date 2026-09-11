using System;
using System.Collections.Generic;
using ArabicSupport;
using TMPro;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Keeps logical Arabic text separate from the presentation-form string
/// consumed by the game's TMP setup.  ArabicFixer is part of the game itself;
/// this class only protects TMP rich-text tags while it performs shaping.
/// </summary>
internal static class ArabicTextFormatter
{
	private sealed class State : MonoBehaviour
	{
		internal string LogicalText;
		internal string RenderedText;
	}

	private sealed class TagPair
	{
		internal readonly string Open;
		internal readonly string Close;
		internal int RestoredCount;

		internal TagPair(string open, string close)
		{
			Open = open;
			Close = close;
		}
	}

	private struct PendingTag
	{
		internal readonly string Name;
		internal readonly string Open;
		internal readonly string Token;

		internal PendingTag(string name, string open, string token)
		{
			Name = name;
			Open = open;
			Token = token;
		}
	}

	private static bool malformedMarkupWarningLogged_;

	internal static void SetText(TMP_Text label, string logicalText)
	{
		if (label == null)
		{
			return;
		}
		State state = GetOrCreateState(label);
		state.LogicalText = logicalText ?? string.Empty;
		Apply(label, state);
	}

	internal static void Apply(TMP_Text label)
	{
		if (label == null)
		{
			return;
		}
		State state = GetOrCreateState(label);
		string current = label.text ?? string.Empty;
		// A caller outside MRA (for example the stock Modal setter) can assign
		// text directly.  Detect that assignment without re-shaping our own
		// presentation string a second time.
		if (!string.Equals(current, state.RenderedText, StringComparison.Ordinal))
		{
			state.LogicalText = current;
		}
		Apply(label, state);
	}

	/// <summary>
	/// Produces the exact presentation-form string that TMP will receive for a
	/// logical string. Used by the font registry when it prewarms Arabic glyphs
	/// before a modal becomes visible.
	/// </summary>
	internal static string ShapeForFont(string logicalText)
	{
		if (string.IsNullOrEmpty(logicalText) || !ModLocalization.IsRtl)
		{
			return logicalText ?? string.Empty;
		}
		return Shape(logicalText);
	}

	private static void Apply(TMP_Text label, State state)
	{
		string logical = state.LogicalText ?? string.Empty;
		string rendered = logical;
		if (ModLocalization.IsRtl && logical.Length != 0)
		{
			rendered = Shape(logical);
			// ArabicFixer returns visual-order presentation forms.  Leaving TMP's
			// RTL pass enabled would reverse the already-fixed string a second time.
			label.isRightToLeftText = false;
		}
		if (!string.Equals(label.text, rendered, StringComparison.Ordinal))
		{
			label.text = rendered;
		}
		state.RenderedText = rendered;
	}

	private static State GetOrCreateState(TMP_Text label)
	{
		State state = label.GetComponent<State>();
		if (state == null)
		{
			state = label.gameObject.AddComponent<State>();
			state.LogicalText = label.text ?? string.Empty;
			state.RenderedText = string.Empty;
		}
		return state;
	}

	private static string Shape(string logical)
	{
		if (logical.IndexOf('<') < 0)
		{
			return ArabicFixer.Fix(logical, showTashkeel: false, useHinduNumbers: false);
		}

		// Each paired tag receives one private-use marker.  ArabicFixer keeps
		// markers as delimiters and reverses the complete line; using the same
		// marker at both ends makes every scope a palindrome, so nested TMP tags
		// remain paired.  Markers are restored after shaping and never reach TMP.
		if (ContainsPrivateUseCharacter(logical))
		{
			WarnMalformedMarkupOnce("Arabic rich text contains a private-use marker; shaping was skipped.");
			return logical;
		}
		List<TagPair> tagPairs = new List<TagPair>();
		Dictionary<char, TagPair> tagTokens = new Dictionary<char, TagPair>();
		Stack<PendingTag> openTags = new Stack<PendingTag>();
		if (!TryProtectTags(logical, tagPairs, tagTokens, openTags, out string protectedText))
		{
			WarnMalformedMarkupOnce("Arabic rich text contains unmatched or unsupported tags; shaping was skipped.");
			return logical;
		}
		try
		{
			string fixedText = ArabicFixer.Fix(protectedText, showTashkeel: false, useHinduNumbers: false);
			if (TryRestoreTags(fixedText, tagPairs, tagTokens, out string restoredText))
			{
				return restoredText;
			}
			WarnMalformedMarkupOnce("Arabic shaping changed protected tag markers; shaping was skipped.");
		}
		catch (Exception exception)
		{
			WarnMalformedMarkupOnce("Arabic shaping failed: " + exception.GetBaseException().Message);
		}
		return logical;
	}

	private static bool TryProtectTags(string text, List<TagPair> tagPairs, Dictionary<char, TagPair> tagTokens, Stack<PendingTag> openTags, out string protectedText)
	{
		System.Text.StringBuilder result = new System.Text.StringBuilder(text.Length);
		int nextMarker = 0xE000;
		int index = 0;
		while (index < text.Length)
		{
			if (text[index] != '<')
			{
				result.Append(text[index++]);
				continue;
			}
			int end = text.IndexOf('>', index + 1);
			if (end < 0)
			{
				result.Append(text[index++]);
				continue;
			}
			string tag = text.Substring(index, end - index + 1);
			string name = GetTagName(tag);
			if (name.Length == 0)
			{
				result.Append(tag);
				index = end + 1;
				continue;
			}

			if (nextMarker > 0xF8FF)
			{
				protectedText = null;
				return false;
			}
			char marker = (char)nextMarker++;
			string token = new string(marker, 1);
			if (IsClosingTag(tag))
			{
				PendingTag pending = FindPendingTag(name, openTags);
				if (pending.Token == null)
				{
					result.Append(tag);
				}
				else
				{
					TagPair pair = new TagPair(pending.Open, tag);
					tagPairs.Add(pair);
					tagTokens[pending.Token[0]] = pair;
					result.Append(pending.Token);
				}
			}
			else if (IsSelfClosingTag(tag))
			{
				TagPair pair = new TagPair(tag, tag);
				tagPairs.Add(pair);
				tagTokens[marker] = pair;
				result.Append(token);
			}
			else
			{
				openTags.Push(new PendingTag(name, tag, token));
				result.Append(token);
			}
			index = end + 1;
		}
		protectedText = result.ToString();
		return openTags.Count == 0;
	}

	private static PendingTag FindPendingTag(string name, Stack<PendingTag> openTags)
	{
		if (openTags.Count == 0)
		{
			return default(PendingTag);
		}
		PendingTag pending = openTags.Pop();
		if (!string.Equals(pending.Name, name, StringComparison.OrdinalIgnoreCase))
		{
			// Malformed markup is left untouched as far as possible; putting the
			// pending tag back prevents a later valid close from being mispaired.
			openTags.Push(pending);
			return default(PendingTag);
		}
		return pending;
	}

	private static bool TryRestoreTags(string text, List<TagPair> tagPairs, Dictionary<char, TagPair> tagTokens, out string restoredText)
	{
		System.Text.StringBuilder result = new System.Text.StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			if (IsPrivateUseCharacter(text[i]) && !tagTokens.ContainsKey(text[i]))
			{
				restoredText = null;
				return false;
			}
			if (!tagTokens.TryGetValue(text[i], out TagPair pair))
			{
				result.Append(text[i]);
				continue;
			}
			if (pair.Open == pair.Close)
			{
				pair.RestoredCount++;
				result.Append(pair.Open);
				continue;
			}
			// ArabicFixer reverses the visual string, so the two marker occurrences
			// are encountered in reverse scope order.  Keep TMP markup syntactically
			// valid around the shaped segment: the first occurrence opens the scope
			// and the second closes it.
			result.Append(pair.RestoredCount++ == 0 ? pair.Open : pair.Close);
		}
		for (int i = 0; i < tagPairs.Count; i++)
		{
			TagPair pair = tagPairs[i];
			int expectedCount = pair.Open == pair.Close ? 1 : 2;
			if (pair.RestoredCount != expectedCount)
			{
				restoredText = null;
				return false;
			}
		}
		restoredText = result.ToString();
		return true;
	}

	private static bool ContainsPrivateUseCharacter(string text)
	{
		for (int i = 0; i < text.Length; i++)
		{
			if (IsPrivateUseCharacter(text[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsPrivateUseCharacter(char character)
	{
		return character >= '\uE000' && character <= '\uF8FF';
	}

	private static void WarnMalformedMarkupOnce(string message)
	{
		if (!malformedMarkupWarningLogged_)
		{
			malformedMarkupWarningLogged_ = true;
			Plugin.Log?.LogWarning(message);
		}
	}

	private static string GetTagName(string tag)
	{
		int index = 1;
		if (index < tag.Length && tag[index] == '/')
		{
			index++;
		}
		while (index < tag.Length && char.IsWhiteSpace(tag[index]))
		{
			index++;
		}
		int start = index;
		while (index < tag.Length && (char.IsLetterOrDigit(tag[index]) || tag[index] == '_'))
		{
			index++;
		}
		return start == index ? string.Empty : tag.Substring(start, index - start);
	}

	private static bool IsClosingTag(string tag)
	{
		return tag.Length > 1 && tag[1] == '/';
	}

	private static bool IsSelfClosingTag(string tag)
	{
		return tag.EndsWith("/>", StringComparison.Ordinal) || string.Equals(GetTagName(tag), "br", StringComparison.OrdinalIgnoreCase);
	}

	internal static void Reset()
	{
		malformedMarkupWarningLogged_ = false;
	}
}
