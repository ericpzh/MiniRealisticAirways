using UnityEngine;

namespace MiniRealisticAirways;

// 全局文本显隐热键。此前挂在 WindSock 上，与风向职责无关且依赖 ESC_Button 存活；
// 独立组件后职责单一，也便于将来处理与其他 mod 的按键冲突。
internal sealed class TextVisibilityHotkey : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			Plugin.showText_ = !Plugin.showText_;
		}
	}
}
