using EFT.UI;
using Fika.Core.Utils;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Fika.Core.Utils.ColorUtils;

namespace Fika.Core.UI.Patches
{
	public class ChangeGameModeButton_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(ChangeGameModeButton).GetMethod(nameof(ChangeGameModeButton.Show));
		}

		[PatchPrefix]
		private static bool PrefixChange(TextMeshProUGUI ____buttonLabel, TextMeshProUGUI ____buttonDescription, Image ____buttonDescriptionIcon,
			GameObject ____availableState)
		{
			____buttonLabel.text = "版本 v1.2";
			____buttonDescription.text = $"{ColorizeText(Colors.BLUE, "Miyako")} Tarkov By 姫様の夢";
			____buttonDescriptionIcon.gameObject.SetActive(false);
			____availableState.SetActive(true);
			return false;
		}
	}
}