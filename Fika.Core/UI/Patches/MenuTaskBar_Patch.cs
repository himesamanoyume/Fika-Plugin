﻿using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Networking.Http;
using Fika.Core.Utils;
using Newtonsoft.Json.Linq;
using SPT.Reflection.Patching;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
	public class MenuTaskBar_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(MenuTaskBar).GetMethod(nameof(MenuTaskBar.Awake));
		}

		[PatchPostfix]
		public static void Postfix(MenuTaskBar __instance)
		{
			GameObject watchlistGameobject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/Watchlist");
			if (watchlistGameobject != null)
			{
				GameObject.Destroy(watchlistGameobject);
				GameObject fleaMarketGameObject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/FleaMarket");
				if (fleaMarketGameObject != null)
				{
					GameObject downloadProfileGameObject = GameObject.Instantiate(fleaMarketGameObject);
					downloadProfileGameObject.name = "DownloadProfile";
					downloadProfileGameObject.transform.SetParent(fleaMarketGameObject.transform.parent, false);
					downloadProfileGameObject.transform.SetSiblingIndex(10);

					GameObject downloadProfileButton = downloadProfileGameObject.transform.GetChild(0).gameObject;
					downloadProfileButton.name = "DownloadProfileButton";

					LocalizedText text = downloadProfileGameObject.GetComponentInChildren<LocalizedText>();
					if (text != null)
					{
						text.method_2("下载存档");
						text.LocalizationKey = "";
					}

					GameObject buildListObject = GameObject.Find("/Menu UI/UI/EquipmentBuildsScreen/Panels/BuildsListPanel/Header/SizeSample/Selected/Icon");
					if (buildListObject != null)
					{
						Image downloadImage = buildListObject.GetComponent<Image>();
						Image downloadProfileImage = downloadProfileButton.transform.GetChild(0).gameObject.GetComponent<Image>();
						if (downloadProfileImage != null && downloadImage != null)
						{
							downloadProfileImage.sprite = downloadImage.sprite;
						}
					}

					AnimatedToggle animatedToggle = downloadProfileGameObject.GetComponentInChildren<AnimatedToggle>();
					if (animatedToggle != null)
					{
						animatedToggle.onValueChanged.AddListener(async (arg) =>
						{
							try
							{
								JObject profile = await FikaRequestHandler.GetProfile();
								bool responseHasError = profile.ContainsKey("errmsg");
								string error = responseHasError ? profile.Value<string>("errmsg") : "检索配置文件失败";
								if (!responseHasError && profile != null)
								{
									Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
									string installDir = Environment.CurrentDirectory;
									string fikaDir = installDir + @"\user\fika";

									if (!string.IsNullOrEmpty(installDir))
									{
										if (!Directory.Exists(fikaDir))
										{
											Directory.CreateDirectory(fikaDir);
										}

										string profileId = Singleton<ClientApplication<ISession>>.Instance.Session.Profile.ProfileId;

										if (File.Exists(@$"{fikaDir}\{profileId}.json"))
										{
											File.Copy(@$"{fikaDir}\{profileId}.json", @$"{fikaDir}\{profileId}.json.BAK", true);
										}

										File.WriteAllText(@$"{fikaDir}\{profileId}.json", profile.ToString());
										NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.SAVED_PROFILE.Localized(),
											[ColorizeText(EColor.BLUE, profileId), fikaDir]));

										GameObject.Destroy(downloadProfileGameObject);
									}
								}
								else
								{
									NotificationManagerClass.DisplayWarningNotification(error);
								}
							}
							catch (Exception ex)
							{
								NotificationManagerClass.DisplayWarningNotification(LocaleUtils.UNKNOWN_ERROR.Localized());
								FikaPlugin.Instance.FikaLogger.LogError(ex.Message);
							}
						});
					}
				}
			}
		}
	}
}
