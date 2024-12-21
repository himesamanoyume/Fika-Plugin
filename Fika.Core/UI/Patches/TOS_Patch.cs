using Comfort.Common;
using EFT;
using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
	public class TOS_Patch : ModulePatch
	{
		// protected const string str_1 = "V2VsY29tZSB0byBGaWthIQoKRmlrYSBpcyBhIGNvLW9wIG1vZCBmb3IgU1BULCBhbGxvd2luZyB5b3UgdG8gcGxheSB3aXRoIHlvdXIgZnJpZW5kcy4gRmlrYSBpcyBhbmQgd2lsbCBhbHdheXMgYmUgZnJlZSwgaWYgeW91IHBhaWQgZm9yIGl0IHlvdSBnb3Qgc2NhbW1lZC4gWW91IGFyZSBhbHNvIG5vdCBhbGxvd2VkIHRvIGhvc3QgcHVibGljIHNlcnZlcnMgd2l0aCBtb25ldGl6YXRpb24gb3IgZG9uYXRpb25zLgoKV2FpdCBmb3IgdGhpcyBtZXNzYWdlIHRvIGZhZGUgdG8gYWNjZXB0IG91ciBUZXJtcyBvZiBTZXJ2aWNlLgoKWW91IGNhbiBqb2luIG91ciBEaXNjb3JkIGhlcmU6IGh0dHBzOi8vZGlzY29yZC5nZy9wcm9qZWN0LWZpa2E=";
		// protected const string str_2 = "V2VsY29tZSB0byBGaWthIQoKRmlrYSBpcyBhIGNvLW9wIG1vZCBmb3IgU1BULCBhbGxvd2luZyB5b3UgdG8gcGxheSB3aXRoIHlvdXIgZnJpZW5kcy4gRmlrYSBpcyBhbmQgd2lsbCBhbHdheXMgYmUgZnJlZSwgaWYgeW91IHBhaWQgZm9yIGl0IHlvdSBnb3Qgc2NhbW1lZC4gWW91IGFyZSBhbHNvIG5vdCBhbGxvd2VkIHRvIGhvc3QgcHVibGljIHNlcnZlcnMgd2l0aCBtb25ldGl6YXRpb24gb3IgZG9uYXRpb25zLgoKWW91IGNhbiBqb2luIG91ciBEaXNjb3JkIGhlcmU6IGh0dHBzOi8vZGlzY29yZC5nZy9wcm9qZWN0LWZpa2E=";

		private static bool HasShown = false;

		protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_23));

		[PatchPostfix]
		public static void Postfix()
		{
			if (HasShown)
			{
				return;
			}

			HasShown = true;
			FikaPlugin.Instance.StartCoroutine(Display());
		}

		private static void AcceptTos()
		{
			FikaPlugin.AcceptedTOS.Value = true;
		}

		private static IEnumerator Display()
		{
			while (!FikaPlugin.Instance.LocalesLoaded)
			{
				yield return new WaitForEndOfFrame();
			}

			if (!FikaPlugin.AcceptedTOS.Value)
			{
				// byte[] str_1_b = Convert.FromBase64String(str_1);
				// string str_1_d = Encoding.UTF8.GetString(str_1_b);
				string str_1_d = "欢迎来到MIYAKO TARKOV！\n\nFika 是一个SPT的合作模组，允许您与朋友一起玩。如果您为此付了钱，说明你被骗了，Fika是免费的。 您也不允许建立付费或需要捐赠的公共服务器。\n\nMiyako服特色:\n\n三倍技能升级速度、实验室丧尸活动常驻、专用客户端自带2名AIPMC队友、任务条件可跳过\n\n等待此消息自动消失来接受Fika的服务条款。";
				Singleton<PreloaderUI>.Instance.ShowFikaMessage($"{ColorizeText(EColor.BLUE, "MIYAKO")} TARKOV", str_1_d, ErrorScreen.EButtonType.QuitButton, 30f,
					Application.Quit, AcceptTos);
			}
			else
			{
				// byte[] str_2_b = Convert.FromBase64String(str_2);
				// string str_2_d = Encoding.UTF8.GetString(str_2_b);
				string str_2_d = "欢迎来到MIYAKO TARKOV！\n\nMiyako服特色:\n\n三倍技能升级速度、实验室丧尸活动常驻、专用客户端自带2名AIPMC队友、任务条件可跳过";
				Singleton<PreloaderUI>.Instance.ShowFikaMessage($"{ColorizeText(EColor.BLUE, "MIYAKO")} TARKOV", str_2_d, ErrorScreen.EButtonType.OkButton, 60f,
					null,
					null);
			}
		}
	}
}
