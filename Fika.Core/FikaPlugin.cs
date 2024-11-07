using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Console;
using Fika.Core.Coop.Airdrops.Utils;
using Fika.Core.Coop.FreeCamera.Patches;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Patches.Airdrop;
using Fika.Core.Coop.Patches.Lighthouse;
using Fika.Core.Coop.Patches.LocalGame;
using Fika.Core.Coop.Patches.Overrides;
using Fika.Core.Coop.Patches.Weather;
using Fika.Core.EssentialPatches;
using Fika.Core.Models;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Websocket;
using Fika.Core.UI;
using Fika.Core.UI.Models;
using Fika.Core.UI.Patches;
using Fika.Core.UI.Patches.MatchmakerAcceptScreen;
using Fika.Core.Utils;
using SPT.Common.Http;
using SPT.Custom.Airdrops.Patches;
using SPT.Custom.BTR.Patches;
using SPT.Custom.Patches;
using SPT.Custom.Utils;
using SPT.Reflection.Patching;
using SPT.SinglePlayer.Patches.MainMenu;
using SPT.SinglePlayer.Patches.Progression;
using SPT.SinglePlayer.Patches.Quests;
using SPT.SinglePlayer.Patches.RaidFix;
using SPT.SinglePlayer.Patches.ScavMode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;

namespace Fika.Core
{
	/// <summary>
	/// Fika.Core Plugin. <br/> <br/>
	/// Originally by: Paulov <br/>
	/// Re-written by: Lacyway
	/// </summary>
	[BepInPlugin("com.fika.core", "姫様の夢汉化 Fika.Core", "0.9.9015")]
	[BepInProcess("EscapeFromTarkov.exe")]
	[BepInDependency("com.SPT.custom", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-custom, that way we can disable its patches
	[BepInDependency("com.SPT.singleplayer", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-singleplayer, that way we can disable its patches
	[BepInDependency("com.SPT.core", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-core, that way we can disable its patches
	[BepInDependency("com.SPT.debugging", BepInDependency.DependencyFlags.HardDependency)] // This is used so that we guarantee to load after spt-debugging, that way we can disable its patches
	public class FikaPlugin : BaseUnityPlugin
	{
		public static FikaPlugin Instance;
		public static InternalBundleLoader BundleLoaderPlugin { get; private set; }
		public static string EFTVersionMajor { get; internal set; }
		public static string ServerModVersion { get; private set; }
		private static Version RequiredServerVersion = new("2.2.8");
		public ManualLogSource FikaLogger { get => Logger; }
		public BotDifficulties BotDifficulties;
		public FikaModHandler ModHandler = new();
		public string[] LocalIPs;
		public static DedicatedRaidWebSocketClient DedicatedRaidWebSocket { get; set; }

		public static Dictionary<string, string> RespectedPlayersList = new()
		{
			{ "samswat",      "godfather of modern SPT modding ~ SSH"                                                       },
			{ "katto",        "kmc leader & founder. OG revolutionary of custom assets ~ SSH"                               },
			{ "polivilas",    "who started it all -- #emutarkov2019 ~ Senko-san"                                            },
			{ "balist0n",     "author of the first singleplayer-focussed mechanics and good friend ~ Senko-san"             },
			{ "ghostfenixx",  "keeps asking me to fix bugs ~ TheSparta"                                                     },
			{ "thurman",      "aka TwistedGA, helped a lot of new modders, including me when I first started ~ TheSparta"   },
			{ "chomp",        "literally unstoppable, carrying SPT development every single day ~ TheSparta"                },
			{ "nimbul",       "Sat with Lacy many night and is loved by both Lacy & me. We miss you <3 ~ SSH"               },
			{ "vox",          "My favourite american. ~ Lacyway"                                                            },
			{ "rairai",       "Very nice and caring person, someone I've appreciated getting to know. ~ Lacyway"            },
			{ "cwx",          "Active and dedicated tester who has contributed a lot of good ideas to Fika. ~ Lacyway"       }
		};

		public static Dictionary<string, string> DevelopersList = new()
		{
			{ "lacyway",      "no one unified the community as much as you ~ Senko-san"                  },
			{ "ssh_",         "my little favorite gremlin. ~ Lacyway"                                    },
			{ "nexus4880",    "the one who taught me everything I know now. ~ SSH"                       },
			{ "thesparta",    "I keep asking him to fix these darn bugs ~ GhostFenixx"                   },
			{ "senko-san",    "creator of SPT, extremely talented dev, a blast to work with ~ TheSparta" },
			{ "leaves",       "Super talented person who comes up with the coolest ideas ~ Lacyway" }
		};

		#region config values

		// Hidden
		public static ConfigEntry<bool> AcceptedTOS { get; set; }

		//Advanced
		public static ConfigEntry<bool> OfficialVersion { get; set; }
		public static ConfigEntry<bool> DisableSPTAIPatches { get; set; }

		// Coop
		public static ConfigEntry<bool> ShowNotifications { get; set; }
		public static ConfigEntry<bool> AutoExtract { get; set; }
		public static ConfigEntry<bool> ShowExtractMessage { get; set; }
		public static ConfigEntry<KeyboardShortcut> ExtractKey { get; set; }
		public static ConfigEntry<bool> EnableChat { get; set; }
		public static ConfigEntry<KeyboardShortcut> ChatKey { get; set; }

		// Coop | Name Plates
		public static ConfigEntry<bool> UseNamePlates { get; set; }
		public static ConfigEntry<bool> HideHealthBar { get; set; }
		public static ConfigEntry<bool> UseHealthNumber { get; set; }
		public static ConfigEntry<bool> UsePlateFactionSide { get; set; }
		public static ConfigEntry<bool> HideNamePlateInOptic { get; set; }
		public static ConfigEntry<bool> NamePlateUseOpticZoom { get; set; }
		public static ConfigEntry<bool> DecreaseOpacityNotLookingAt { get; set; }
		public static ConfigEntry<float> NamePlateScale { get; set; }
		public static ConfigEntry<float> OpacityInADS { get; set; }
		public static ConfigEntry<float> MaxDistanceToShow { get; set; }
		public static ConfigEntry<float> MinimumOpacity { get; set; }
		public static ConfigEntry<float> MinimumNamePlateScale { get; set; }
		public static ConfigEntry<bool> ShowEffects { get; set; }
		public static ConfigEntry<bool> UseOcclusion { get; set; }

		// Coop | Quest Sharing
		public static ConfigEntry<EQuestSharingTypes> QuestTypesToShareAndReceive { get; set; }
		public static ConfigEntry<bool> QuestSharingNotifications { get; set; }
		public static ConfigEntry<bool> EasyKillConditions { get; set; }

		// Coop | Custom
		public static ConfigEntry<bool> UsePingSystem { get; set; }
		public static ConfigEntry<KeyboardShortcut> PingButton { get; set; }
		public static ConfigEntry<Color> PingColor { get; set; }
		public static ConfigEntry<float> PingSize { get; set; }
		public static ConfigEntry<int> PingTime { get; set; }
		public static ConfigEntry<bool> PlayPingAnimation { get; set; }
		public static ConfigEntry<bool> ShowPingDuringOptics { get; set; }
		public static ConfigEntry<bool> PingUseOpticZoom { get; set; }
		public static ConfigEntry<bool> PingScaleWithDistance { get; set; }
		public static ConfigEntry<float> PingMinimumOpacity { get; set; }
		public static ConfigEntry<EPingSound> PingSound { get; set; }

		// Coop | Debug
		public static ConfigEntry<KeyboardShortcut> FreeCamButton { get; set; }
		public static ConfigEntry<bool> AZERTYMode { get; set; }
		public static ConfigEntry<bool> KeybindOverlay { get; set; }

		// Performance
		public static ConfigEntry<bool> DynamicAI { get; set; }
		public static ConfigEntry<float> DynamicAIRange { get; set; }
		public static ConfigEntry<EDynamicAIRates> DynamicAIRate { get; set; }
		public static ConfigEntry<bool> DynamicAIIgnoreSnipers { get; set; }

		// Performance | Bot Limits            
		public static ConfigEntry<bool> EnforcedSpawnLimits { get; set; }
		public static ConfigEntry<bool> DespawnFurthest { get; set; }
		public static ConfigEntry<float> DespawnMinimumDistance { get; set; }
		public static ConfigEntry<int> MaxBotsFactory { get; set; }
		public static ConfigEntry<int> MaxBotsCustoms { get; set; }
		public static ConfigEntry<int> MaxBotsInterchange { get; set; }
		public static ConfigEntry<int> MaxBotsReserve { get; set; }
		public static ConfigEntry<int> MaxBotsGroundZero { get; set; }
		public static ConfigEntry<int> MaxBotsWoods { get; set; }
		public static ConfigEntry<int> MaxBotsStreets { get; set; }
		public static ConfigEntry<int> MaxBotsShoreline { get; set; }
		public static ConfigEntry<int> MaxBotsLabs { get; set; }
		public static ConfigEntry<int> MaxBotsLighthouse { get; set; }

		// Network
		public static ConfigEntry<bool> NativeSockets { get; set; }
		public static ConfigEntry<string> ForceIP { get; set; }
		public static ConfigEntry<string> ForceBindIP { get; set; }
		public static ConfigEntry<string> ForceBindIP2 { get; set; }
		public static ConfigEntry<float> AutoRefreshRate { get; set; }
		public static ConfigEntry<int> UDPPort { get; set; }
		public static ConfigEntry<bool> UseUPnP { get; set; }
		public static ConfigEntry<bool> UseNatPunching { get; set; }
		public static ConfigEntry<int> ConnectionTimeout { get; set; }

		// Gameplay
		public static ConfigEntry<float> HeadDamageMultiplier { get; set; }
		public static ConfigEntry<float> ArmpitDamageMultiplier { get; set; }
		public static ConfigEntry<float> StomachDamageMultiplier { get; set; }
		public static ConfigEntry<bool> DisableBotMetabolism { get; set; }
		#endregion

		#region client config
		public bool UseBTR;
		public bool FriendlyFire;
		public bool DynamicVExfils;
		public bool AllowFreeCam;
		public bool AllowSpectateFreeCam;
		public bool AllowItemSending;
		public string[] BlacklistedItems;
		public bool ForceSaveOnDeath;
		public bool UseInertia;
		public bool SharedQuestProgression;
		#endregion

		#region natpunch config
		public bool NatPunchServerEnable;
		public string NatPunchServerIP;
		public int NatPunchServerPort;
		public int NatPunchServerNatIntroduceAmount;
		#endregion

		protected void Awake()
		{
			Instance = this;

			GetNatPunchServerConfig();
			SetupConfig();

			new FikaVersionLabel_Patch().Enable();
			new DisableReadyButton_Patch().Enable();
			new DisableInsuranceReadyButton_Patch().Enable();
			new DisableMatchSettingsReadyButton_Patch().Enable();
			new TarkovApplication_LocalGamePreparer_Patch().Enable();
			new TarkovApplication_LocalGameCreator_Patch().Enable();
			new DeathFade_Patch().Enable();
			new NonWaveSpawnScenario_Patch().Enable();
			new WaveSpawnScenario_Patch().Enable();
			new WeatherNode_Patch().Enable();
			new MatchmakerAcceptScreen_Awake_Patch().Enable();
			new MatchmakerAcceptScreen_Show_Patch().Enable();
			new Minefield_method_2_Patch().Enable();
			new BotCacher_Patch().Enable();
			new AbstractGame_InRaid_Patch().Enable();
			new DisconnectButton_Patch().Enable();
			new ChangeGameModeButton_Patch().Enable();
			new MenuTaskBar_Patch().Enable();
			new GameWorld_Create_Patch().Enable();
			new World_AddSpawnQuestLootPacket_Patch().Enable();

			gameObject.AddComponent<MainThreadDispatcher>();

#if GOLDMASTER
            new TOS_Patch().Enable();
#endif
			OfficialVersion.SettingChanged += OfficialVersion_SettingChanged;

			DisableSPTPatches();
			EnableOverridePatches();

			GetClientConfig();

			string fikaVersion = Assembly.GetAssembly(typeof(FikaPlugin)).GetName().Version.ToString();

			Logger.LogInfo($"Fika is loaded! Running version: " + fikaVersion);

			BundleLoaderPlugin = new();
			BundleLoaderPlugin.Create();

			FikaAirdropUtil.GetConfigFromServer();
			BotSettingsRepoAbstractClass.Init();

			BotDifficulties = FikaRequestHandler.GetBotDifficulties();
			ConsoleScreen.Processor.RegisterCommandGroup<FikaCommands>();

			if (AllowItemSending)
			{
				new ItemContext_Patch().Enable();
			}

			StartCoroutine(RunChecks());
		}

		private void VerifyServerVersion()
		{
			string version = FikaRequestHandler.CheckServerVersion().Version;
			bool failed = true;
			if (Version.TryParse(version, out Version serverVersion))
			{
				if (serverVersion >= RequiredServerVersion)
				{
					failed = false;
				}
			}

			if (failed)
			{
				FikaLogger.LogError($"Server version check failed. Expected: >{RequiredServerVersion}, received: {serverVersion}");
				MessageBoxHelper.Show($"Failed to verify server mod version.\nMake sure that the server mod is installed and up-to-date!\nRequired Server Version: {RequiredServerVersion}",
					"FIKA ERROR", MessageBoxHelper.MessageBoxType.OK);
				Application.Quit();
			}
			else
			{
				FikaLogger.LogInfo($"Server version check passed. Expected: >{RequiredServerVersion}, received: {serverVersion}");
			}
		}

		/// <summary>
		/// Coroutine to ensure all mods are loaded by waiting 5 seconds
		/// </summary>
		/// <returns></returns>
		private IEnumerator RunChecks()
		{
			yield return new WaitForSeconds(5);
			VerifyServerVersion();
			ModHandler.VerifyMods();
		}

		private void GetClientConfig()
		{
			ClientConfigModel clientConfig = FikaRequestHandler.GetClientConfig();

			UseBTR = clientConfig.UseBTR;
			FriendlyFire = clientConfig.FriendlyFire;
			DynamicVExfils = clientConfig.DynamicVExfils;
			AllowFreeCam = clientConfig.AllowFreeCam;
			AllowSpectateFreeCam = clientConfig.AllowSpectateFreeCam;
			AllowItemSending = clientConfig.AllowItemSending;
			BlacklistedItems = clientConfig.BlacklistedItems;
			ForceSaveOnDeath = clientConfig.ForceSaveOnDeath;
			UseInertia = clientConfig.UseInertia;
			SharedQuestProgression = clientConfig.SharedQuestProgression;

			clientConfig.LogValues();
		}

		private void GetNatPunchServerConfig()
		{
			NatPunchServerConfigModel natPunchServerConfig = FikaRequestHandler.GetNatPunchServerConfig();

			NatPunchServerEnable = natPunchServerConfig.Enable;
			NatPunchServerIP = RequestHandler.Host.Replace("http://", "").Split(':')[0];
			NatPunchServerPort = natPunchServerConfig.Port;
			NatPunchServerNatIntroduceAmount = natPunchServerConfig.NatIntroduceAmount;

			natPunchServerConfig.LogValues();
		}

		private void SetupConfig()
        {
            // Hidden

            AcceptedTOS = Config.Bind("隐藏", "已接受条款", false,
                new ConfigDescription("已接受服务条款", tags: new ConfigurationManagerAttributes() { Browsable = false }));

            // Advanced

            OfficialVersion = Config.Bind("进阶", "官方版本", false,
                new ConfigDescription("显示官方版本而非 Fika 版本", tags: new ConfigurationManagerAttributes() { IsAdvanced = true }));

            DisableSPTAIPatches = Config.Bind("进阶", "禁用 SPT AI 补丁", false,
                new ConfigDescription("禁用 Fika 中有可能'多余'的 SPT AI 补丁", tags: new ConfigurationManagerAttributes { IsAdvanced = true }));

            // Coop

            ShowNotifications = Instance.Config.Bind("2.联机", "显示通知", true,
                new ConfigDescription("启用自定义通知，当玩家死亡、提取、击杀boss等时显示", tags: new ConfigurationManagerAttributes() { Order = 7 }));

            AutoExtract = Config.Bind("联机", "自动撤离", false,
                new ConfigDescription("在撤离倒计时结束后自动撤离。只在没有客户端连接时且作为主机时有效", tags: new ConfigurationManagerAttributes() { Order = 6 }));

            ShowExtractMessage = Config.Bind("联机", "显示 撤离信息", true,
                new ConfigDescription("是否在死亡/撤离后显示消息", tags: new ConfigurationManagerAttributes() { Order = 5 }));

            ExtractKey = Config.Bind("联机", "撤离 按键", new KeyboardShortcut(KeyCode.F8),
                new ConfigDescription("用于从战局中撤离的按键", tags: new ConfigurationManagerAttributes() { Order = 2 }));

            EnableChat = Config.Bind("联机", "启用 聊天", false,
                new ConfigDescription("切换以启用游戏内聊天。不能在战局中更改", tags: new ConfigurationManagerAttributes() { Order = 1 }));

            ChatKey = Config.Bind("联机", "聊天 按键", new KeyboardShortcut(KeyCode.RightControl),
                new ConfigDescription("打开聊天窗口的按键", tags: new ConfigurationManagerAttributes() { Order = 0 }));

            // Coop | Name Plates

            UseNamePlates = Config.Bind("联机 | 铭牌显示", "显示玩家 铭牌", false,
                new ConfigDescription("是否显示 玩家名称&血条", tags: new ConfigurationManagerAttributes() { Order = 13 }));

            HideHealthBar = Config.Bind("联机 | 铭牌显示", "隐藏 血条", false,
                new ConfigDescription("完全隐藏玩家血条", tags: new ConfigurationManagerAttributes() { Order = 12 }));

            UseHealthNumber = Config.Bind("联机 | 铭牌显示", "显示 血量百分比%", false,
                new ConfigDescription("显示血量的百分比%而不是显示血条", tags: new ConfigurationManagerAttributes() { Order = 11 }));

            ShowEffects = Config.Bind("联机 | 铭牌显示", "显示 状态效果", true,
                new ConfigDescription("开启后，会在玩家血条下方展示当前的状态效果", tags: new ConfigurationManagerAttributes() { Order = 10 }));

            UsePlateFactionSide = Config.Bind("联机 | 铭牌显示", "显示 玩家阵营", true,
                new ConfigDescription("开启后，会在玩家血条边上显示玩家的所属阵营", tags: new ConfigurationManagerAttributes() { Order = 9 }));

            HideNamePlateInOptic = Config.Bind("联机 | 铭牌显示", "使用 瞄准镜 使隐藏铭牌", true,
                new ConfigDescription("在通过 瞄准镜 查看时隐藏铭牌", tags: new ConfigurationManagerAttributes() { Order = 8 }));

            NamePlateUseOpticZoom = Config.Bind("联机 | 铭牌显示", "铭牌使用 瞄准镜 缩放", true,
                new ConfigDescription("是否在使用瞄准镜时缩放显示铭牌", tags: new ConfigurationManagerAttributes() { Order = 7, IsAdvanced = true }));

            DecreaseOpacityNotLookingAt = Config.Bind("联机 | 铭牌显示", "未注视时降低透明度", true,
                new ConfigDescription("当未注视玩家时，降低铭牌的透明度", tags: new ConfigurationManagerAttributes() { Order = 6 }));

            NamePlateScale = Config.Bind("联机 | 铭牌显示", "铭牌比例", 0.22f,
                new ConfigDescription("铭牌的大小", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 5 }));

            OpacityInADS = Config.Bind("联机 | 铭牌显示", "瞄准时透明度", 0.75f,
                new ConfigDescription("瞄准时铭牌的透明度", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

            MaxDistanceToShow = Config.Bind("联机 | 铭牌显示", "铭牌显示的最大距离", 500f,
                new ConfigDescription("铭牌将变得不可见的最大距离，开始在输入值的一半处逐渐消失", new AcceptableValueRange<float>(10f, 1000f), new ConfigurationManagerAttributes() { Order = 3 }));

            MinimumOpacity = Config.Bind("联机 | 铭牌显示", "最小 铭牌透明度", 0.1f,
                new ConfigDescription("铭牌的最小透明度", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 2 }));

            MinimumNamePlateScale = Config.Bind("联机 | 铭牌显示", "最小 铭牌比例", 0.01f,
                new ConfigDescription("铭牌的最小比例", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes() { Order = 1 }));

            UseOcclusion = Config.Bind("联机 | 铭牌显示", "使用遮挡", false,
                new ConfigDescription("当玩家不在视线范围内时，使用遮挡来隐藏铭牌", tags: new ConfigurationManagerAttributes() { Order = 0 }));

            // Coop | Quest Sharing

            QuestTypesToShareAndReceive = Config.Bind("联机 | 任务共享", "任务类型", EQuestSharingTypes.All,
                new ConfigDescription("选择能够共享的任务类型。\nKill - 击杀\nItem - 寻找物品\nLocation - 位置踩点\nPlaceBeacon - 放置标记：既包括标记也包括放置物品", tags: new ConfigurationManagerAttributes() { Order = 2 }));

            QuestSharingNotifications = Config.Bind("联机 | 任务共享", "显示通知", true,
                new ConfigDescription("是否在任务进度共享时显示通知", tags: new ConfigurationManagerAttributes() { Order = 1 }));

            EasyKillConditions = Config.Bind("联机 | 任务共享", "共享击杀进度", false,
                new ConfigDescription("启用简化击杀条件。当启用时，每当友方玩家击杀敌人时，将被视为自己任务进度所杀", tags: new ConfigurationManagerAttributes() { Order = 0 }));

            // Coop | Custom

            UsePingSystem = Config.Bind("联机 | 自定义", "启用标点标记", false,
                new ConfigDescription("启用标点系统。开启后，您可以通过按下标点键来接收和发送标点", tags: new ConfigurationManagerAttributes() { Order = 9 }));

            PingButton = Config.Bind("联机 | 自定义", "标点 按键", new KeyboardShortcut(KeyCode.U),
                new ConfigDescription("用于发送标点的按键", tags: new ConfigurationManagerAttributes() { Order = 8 }));

            PingColor = Config.Bind("联机 | 自定义", "标点颜色", Color.white,
                new ConfigDescription("标点在其他玩家屏幕上的颜色", tags: new ConfigurationManagerAttributes() { Order = 7 }));

            PingSize = Config.Bind("联机 | 自定义", "标点大小", 1f,
                new ConfigDescription("标点图标大小倍率", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes() { Order = 6 }));

            PingTime = Config.Bind("联机 | 自定义", "标点显示时间", 3,
                new ConfigDescription("标点显示的时长", new AcceptableValueRange<int>(2, 10), new ConfigurationManagerAttributes() { Order = 5 }));

            PlayPingAnimation = Config.Bind("联机 | 自定义", "播放标点动画", false,
                new ConfigDescription("当发送标点时自动播放指示动画。可能会影响游戏体验", tags: new ConfigurationManagerAttributes() { Order = 4 }));

            ShowPingDuringOptics = Config.Bind("联机 | 自定义", "瞄准镜中显示标点", false,
                new ConfigDescription("是否在瞄准镜视野中显示标点", tags: new ConfigurationManagerAttributes() { Order = 3 }));

            PingUseOpticZoom = Config.Bind("联机 | 自定义", "标点在瞄准镜时缩放", true,
                new ConfigDescription("标点位置是否应使用瞄准镜时显示", tags: new ConfigurationManagerAttributes() { Order = 2, IsAdvanced = true }));

            PingScaleWithDistance = Config.Bind("联机 | 自定义", "标点随距离缩放", true,
                new ConfigDescription("标点大小是否应随距离从玩家缩放", tags: new ConfigurationManagerAttributes() { Order = 1, IsAdvanced = true }));

            PingMinimumOpacity = Config.Bind("联机 | 自定义", "标点最小透明度", 0.05f,
                new ConfigDescription("标点大小是否应随距离从玩家缩放", new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes() { Order = 0, IsAdvanced = true }));
            
            PingSound = Config.Bind("联机 | 自定义", "标点音效", EPingSound.SubQuestComplete,
                new ConfigDescription("标点时播放的音效"));

            // Coop | Debug

            FreeCamButton = Config.Bind("联机 | 调试Debug", "自由视角 按键", new KeyboardShortcut(KeyCode.F9),
                "用于切换自由视角的按键");

            AZERTYMode = Config.Bind("联机 | 调试Debug", "AZERTY模式(法式键位)", false,
                "如果自由视角应该使用AZERTY键盘布局进行输入");

            KeybindOverlay = Config.Bind("联机 | 调试Debug", "按键绑定叠加", true,
                "是否显示包含所有自由视角按键绑定的叠加层");

            // Performance

            DynamicAI = Config.Bind("性能", "动态AI", false,
                new ConfigDescription("使用动态AI系统，在AI位于任何玩家范围之外时禁用AI", tags: new ConfigurationManagerAttributes() { Order = 3 }));

            DynamicAIRange = Config.Bind("性能", "动态AI 范围", 100f,
                new ConfigDescription("动态禁用AI的范围", new AcceptableValueRange<float>(150f, 1000f), new ConfigurationManagerAttributes() { Order = 2 }));

            DynamicAIRate = Config.Bind("性能", "动态AI 扫描频率", EDynamicAIRates.Medium,
                new ConfigDescription("动态AI扫描所有玩家范围的频率", tags: new ConfigurationManagerAttributes() { Order = 1 }));

            DynamicAIIgnoreSnipers = Config.Bind("性能", "动态AI - 忽略狙击手", true,
                new ConfigDescription("是否动态AI应该忽略狙击手", tags: new ConfigurationManagerAttributes() { Order = 0 }));


            // Performance | Max Bots

            EnforcedSpawnLimits = Config.Bind("性能 | AI上限", "强制生成限制", false,
                new ConfigDescription("当生成AI时，强制执行生成限制，确保不超过原版限制。主要在使用生成模组或修改AI限制的情况下生效。不会阻止特殊AI的生成，例如Boss", tags: new ConfigurationManagerAttributes() { Order = 14 }));

            DespawnFurthest = Config.Bind("性能 | AI上限", "优先消失最远的AI", false,
                new ConfigDescription("在强制生成限制时，是否让最远的AI消失，而不是阻止生成。这将使低最大AI数量的战局更活跃。对性能较差的PC有帮助。\n不过，如果没有动态生成模组，这可能会迅速耗尽地图上的生成点，使战局变得十分冷清", tags: new ConfigurationManagerAttributes() { Order = 13 }));

            DespawnMinimumDistance = Config.Bind("性能 | AI上限", "最小消失距离", 200.0f,
                new ConfigDescription("在此距离内AI不会消失", new AcceptableValueRange<float>(50f, 3000f), new ConfigurationManagerAttributes() { Order = 12 }));

            MaxBotsFactory = Config.Bind("性能 | AI上限", "AI上限 - 工厂", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 11 }));

            MaxBotsCustoms = Config.Bind("性能 | AI上限", "AI上限 - 海关", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 10 }));

            MaxBotsInterchange = Config.Bind("性能 | AI上限", "AI上限 - 立交桥", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 8 }));

            MaxBotsReserve = Config.Bind("性能 | AI上限", "AI上限 - 储备站", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 7 }));

            MaxBotsWoods = Config.Bind("性能 | AI上限", "AI上限 - 森林", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 6 }));

            MaxBotsShoreline = Config.Bind("性能 | AI上限", "AI上限 - 海岸线", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 5 }));

            MaxBotsStreets = Config.Bind("性能 | AI上限", "AI上限 - 塔科夫街区", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 4 }));

            MaxBotsGroundZero = Config.Bind("性能 | AI上限", "AI上限 - 市中心", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 3 }));

            MaxBotsLabs = Config.Bind("性能 | AI上限", "AI上限 - 实验室", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 2 }));

            MaxBotsLighthouse = Config.Bind("性能 | AI上限", "AI上限 - 灯塔", 0,
                new ConfigDescription("可以同时存在的最大AI数量。如果你的PC性能较差，这个设置很有用。设置为0则使用原版限制。在战局中不能更改", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes() { Order = 1 }));

            // Network

            NativeSockets = Config.Bind(section: "网络", "本地套接字(Native Sockets)", false,
                new ConfigDescription("使用本地套接字进行游戏流量传输。这使用直接的套接字调用进行发送/接收，以显著提高速度并减少GC压力。仅适用于Windows。Linux可能无效", tags: new ConfigurationManagerAttributes() { Order = 8 }));

            ForceIP = Config.Bind("网络", "强制IP(Force IP)", "",
                new ConfigDescription("当托管时，强制服务器使用此IP进行广播，而不是自动尝试获取IP。留空以禁用\n若使用radmin等虚拟局域网联机工具，请填写自己联机所使用的IP", tags: new ConfigurationManagerAttributes() { Order = 7 }));

            ForceBindIP = Config.Bind("网络", "强制绑定IP(Force Bind IP)", "",
                new ConfigDescription("当托管时，强制服务器使用此本地IP启动服务器。如果您在VPN上托管，这非常有用\n若使用radmin等虚拟局域网联机工具，请填写自己联机所使用的IP", new AcceptableValueList<string>(GetLocalAddresses()), new ConfigurationManagerAttributes() { Order = 6 }));

            AutoRefreshRate = Config.Bind("网络", "刷新服务器房间频率", 10f,
                new ConfigDescription("客户端在地图准备大厅屏幕上每X秒将向服务器请求刷新当前房间列表", new AcceptableValueRange<float>(3f, 60f), new ConfigurationManagerAttributes() { Order = 5 }));

            UDPPort = Config.Bind("网络", "UDP 端口", 25565,
                new ConfigDescription("用于UDP游戏数据包的端口", tags: new ConfigurationManagerAttributes() { Order = 4 }));

            UseUPnP = Config.Bind("网络", "使用 UPnP", false,
                new ConfigDescription("尝试使用UPnP打开端口。如果您无法自己打开端口，但路由器支持UPnP，这将很有用", tags: new ConfigurationManagerAttributes() { Order = 3 }));

            UseNatPunching = Config.Bind("网络", "使用 NAT 打孔", false,
                new ConfigDescription("在托管战局时使用NAT打孔。仅适用于全锥形NAT类型的路由器，并且需要在SPT服务器上运行NatPunchServer。启用此模式时，UPnP、强制IP和强制绑定IP将被禁用", tags: new ConfigurationManagerAttributes() { Order = 2 }));

            ConnectionTimeout = Config.Bind("网络", "连接超时", 15,
                new ConfigDescription("如果未收到数据包，则连接被视为丢失的时间", new AcceptableValueRange<int>(5, 60), new ConfigurationManagerAttributes() { Order = 1 }));

            // Gameplay

            HeadDamageMultiplier = Config.Bind("游戏玩法", "头部伤害倍率", 1f,
                new ConfigDescription("头部受到伤害的倍数。0.2 表示 20%", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 4 }));

            ArmpitDamageMultiplier = Config.Bind("游戏玩法", "腋下伤害倍率", 1f,
                new ConfigDescription("腋下受到伤害的倍数。0.2 表示 20%", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 3 }));

            StomachDamageMultiplier = Config.Bind("游戏玩法", "腹部伤害倍率", 1f,
                new ConfigDescription("腹部受到伤害的倍数。0.2 表示 20%", new AcceptableValueRange<float>(0.05f, 1f), new ConfigurationManagerAttributes() { Order = 2 }));

            DisableBotMetabolism = Config.Bind("游戏玩法", "禁用AI新陈代谢", false,
                new ConfigDescription("禁用AI的新陈代谢，防止它们在过长时间的战局中因能量/水分丧失而死亡", tags: new ConfigurationManagerAttributes() { Order = 1 }));
        }

		private void OfficialVersion_SettingChanged(object sender, EventArgs e)
		{
			FikaVersionLabel_Patch.UpdateVersionLabel();
		}

		private string[] GetLocalAddresses()
		{
			List<string> ips = [];
			ips.Add("Disabled");
			ips.Add("0.0.0.0");

			try
			{
				foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
				{
					foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
					{
						if (!ip.IsDnsEligible)
						{
							continue;
						}

						if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							string stringIp = ip.Address.ToString();
							if (stringIp != "127.0.0.1")
							{
								ips.Add(stringIp);
							}
						}
					}
				}

				LocalIPs = ips.Skip(1).ToArray();
				return [.. ips];
			}
			catch (Exception)
			{
				return [.. ips];
			}
		}

		private void DisableSPTPatches()
		{
			Chainloader.PluginInfos.TryGetValue("com.SPT.core", out PluginInfo pluginInfo);

			bool OlderSPTVersion = false;

			if (pluginInfo.Metadata.Version < Version.Parse("3.9.5"))
			{
				FikaLogger.LogWarning("Older SPT version found!");
				OlderSPTVersion = true;
			}

			// Disable these as they interfere with Fika
			new BotDifficultyPatch().Disable();
			new AirdropPatch().Disable();
			new AirdropFlarePatch().Disable();
			new VersionLabelPatch().Disable();
			new EmptyInfilFixPatch().Disable();
			new OfflineSpawnPointPatch().Disable();
			new BotTemplateLimitPatch().Disable();
			new OfflineRaidSettingsMenuPatch().Disable();
			new AddEnemyToAllGroupsInBotZonePatch().Disable();
			new MaxBotPatch().Disable();
			new LabsKeycardRemovalPatch().Disable(); // We handle this locally instead
			new AmmoUsedCounterPatch().Disable();
			new ArmorDamageCounterPatch().Disable();
			new DogtagPatch().Disable();
			new OfflineSaveProfilePatch().Disable(); // We handle this with our own exit manager
			new ScavRepAdjustmentPatch().Disable();
			new DisablePvEPatch().Disable();
			new ClampRagdollPatch().Disable();
			new LighthouseBridgePatch().Disable();

			new AddEnemyToAllGroupsInBotZonePatch().Disable();

			Assembly sptCustomAssembly = typeof(IsEnemyPatch).Assembly;

			if (OlderSPTVersion)
			{
				Type botCallForHelpCallBotPatchType = sptCustomAssembly.GetType("SPT.Custom.Patches.BotCallForHelpCallBotPatch");
				ModulePatch botCallForHelpCallBotPatch = (ModulePatch)Activator.CreateInstance(botCallForHelpCallBotPatchType);
				botCallForHelpCallBotPatch.Disable();
			}

			if (DisableSPTAIPatches.Value)
			{
				new BotEnemyTargetPatch().Disable();
				new IsEnemyPatch().Disable();

				if (!OlderSPTVersion)
				{
					new BotOwnerDisposePatch().Disable();
					new BotCalledDataTryCallPatch().Disable();
					new BotSelfEnemyPatch().Disable();
				}
				else
				{
					Type botOwnerDisposePatchType = sptCustomAssembly.GetType("SPT.Custom.Patches.BotOwnerDisposePatch");
					ModulePatch botOwnerDisposePatch = (ModulePatch)Activator.CreateInstance(botOwnerDisposePatchType);
					botOwnerDisposePatch.Disable();

					Type botCalledDataTryCallPatchType = sptCustomAssembly.GetType("SPT.Custom.Patches.BotCalledDataTryCallPatch");
					ModulePatch botCalledDataTryCallPatch = (ModulePatch)Activator.CreateInstance(botCalledDataTryCallPatchType);
					botCalledDataTryCallPatch.Disable();

					Type botSelfEnemyPatchType = sptCustomAssembly.GetType("SPT.Custom.Patches.BotSelfEnemyPatch");
					ModulePatch botSelfEnemyPatch = (ModulePatch)Activator.CreateInstance(botSelfEnemyPatchType);
					botSelfEnemyPatch.Disable();
				}
			}

			new BTRInteractionPatch().Disable();
			new BTRExtractPassengersPatch().Disable();
			new BTRPatch().Disable();
		}

		private void EnableOverridePatches()
		{
			new BotDifficultyPatch_Override().Enable();
			new ScavProfileLoad_Override().Enable();
			new MaxBotPatch_Override().Enable();
			new BotTemplateLimitPatch_Override().Enable();
			new OfflineRaidSettingsMenuPatch_Override().Enable();
			new AddEnemyToAllGroupsInBotZonePatch_Override().Enable();
			new AirdropBox_Patch().Enable();
			new FikaAirdropFlare_Patch().Enable();
			new LighthouseBridge_Patch().Enable();
			new LighthouseMines_Patch().Enable();
		}

		public enum EDynamicAIRates
		{
			Low,
			Medium,
			High
		}

		public enum EPingSound
		{
			SubQuestComplete,
			InsuranceInsured,
			ButtonClick,
			ButtonHover,
			InsuranceItemInsured,
			MenuButtonBottom,
			ErrorMessage,
			InspectWindow,
			InspectWindowClose,
			MenuEscape,
		}

		[Flags]
		public enum EQuestSharingTypes
		{
			Kills = 1,
			Item = 2,
			Location = 4,
			PlaceBeacon = 8,

			All = Kills | Item | Location | PlaceBeacon
		}
	}
}