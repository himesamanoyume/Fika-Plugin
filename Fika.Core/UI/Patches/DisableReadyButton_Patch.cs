﻿// © 2024 Lacyway All Rights Reserved

using EFT.UI;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI
{
	/// <summary>
	/// Created by: Lacyway
	/// </summary>
	public class DisableReadyButton_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MatchMakerSelectionLocationScreen).GetMethod(nameof(MatchMakerSelectionLocationScreen.Awake));
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