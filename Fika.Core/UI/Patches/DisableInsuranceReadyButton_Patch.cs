// © 2024 Lacyway All Rights Reserved

using EFT.UI;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
	/// <summary>
	/// Created by: Lacyway
	/// </summary>
	public class DisableInsuranceReadyButton_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MatchmakerInsuranceScreen).GetMethod(nameof(MatchmakerInsuranceScreen.Awake));
		}

		[PatchPostfix]
		static void Postfix(DefaultUIButton ____readyButton)
		{
			____readyButton.SetDisabledTooltip("已被Fika禁用");
			____readyButton.SetEnabledTooltip("已被Fika禁用");

			____readyButton.Interactable = false;
		}
	}
}