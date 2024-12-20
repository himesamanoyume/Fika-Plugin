using EFT;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BotCacher_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass583).GetMethod(nameof(GClass583.LoadInternal), BindingFlags.Static | BindingFlags.Public);
		}

		[PatchPrefix]
		private static bool PatchPrefix(out CoreBotSettingsClass core, ref bool __result)
		{
			// 这是一个前缀补丁方法,用于处理机器人(Bot)设置的加载
			// 参数说明:
			// - core: 输出参数,用于存储核心机器人设置
			// - __result: 引用参数,表示加载是否成功

			// 首先检查FikaPlugin实例中是否已有机器人难度设置
			if (FikaPlugin.Instance.BotDifficulties != null)
			{
				// 如果有,直接从插件实例获取核心设置
				core = FikaPlugin.Instance.BotDifficulties.GetCoreSettings();
			}
			else
			{
				// 如果没有,尝试从字符串加载核心设置
				string text = GClass583.LoadCoreByString();
				if (text == null)
				{
					// 如果加载失败,设置core为null并返回false
					core = null;
					__result = false;
					return false;
				}
				// 从加载的字符串创建核心设置对象
				core = CoreBotSettingsClass.Create(text);
			}

			// 遍历所有野生出生类型(WildSpawnType)
			foreach (object type in Enum.GetValues(typeof(WildSpawnType)))
			{
				// 对每个出生类型,遍历所有机器人难度等级(BotDifficulty) 
				foreach (object difficulty in Enum.GetValues(typeof(BotDifficulty)))
				{
					BotSettingsComponents botSettingsComponents;
					// 尝试从插件实例获取对应难度和类型的设置组件
					botSettingsComponents = FikaPlugin.Instance.BotDifficulties.GetComponent((BotDifficulty)difficulty, (WildSpawnType)type);
					if (botSettingsComponents != null)
					{
						// 如果成功获取组件,且AllSettings中不存在该配置,则添加
						if (!GClass583.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
						{
							GClass583.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
						}
					}
					else
					{
						// 如果无法从插件获取,尝试通过其他方法创建设置组件
						botSettingsComponents = GClass583.smethod_1(GClass583.CheckOnExclude((BotDifficulty)difficulty, (WildSpawnType)type), (WildSpawnType)type, false);
						if (botSettingsComponents != null)
						{
							// 如果成功创建,且AllSettings中不存在该配置,则添加
							if (!GClass583.AllSettings.ContainsKey((BotDifficulty)difficulty, (WildSpawnType)type))
							{
								GClass583.AllSettings.Add((BotDifficulty)difficulty, (WildSpawnType)type, botSettingsComponents);
							}
						}
						else
						{
							// 如果所有尝试都失败,返回false
							__result = false;
							return false;
						}
					}
				}
			}

			// 所有设置都成功加载,设置结果为true
			__result = true;
			return false;
		}
	}
}
