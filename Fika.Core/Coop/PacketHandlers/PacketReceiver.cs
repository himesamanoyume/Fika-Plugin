﻿// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
	public class PacketReceiver : MonoBehaviour
	{
		private CoopPlayer player;
		private ObservedCoopPlayer observedPlayer;
		public FikaServer Server { get; private set; }
		public FikaClient Client { get; private set; }
		public Queue<WeaponPacket> FirearmPackets { get; private set; } = new(50);
		public Queue<DamagePacket> DamagePackets { get; private set; } = new(50);
		public Queue<ArmorDamagePacket> ArmorDamagePackets { get; private set; } = new(50);
		public Queue<InventoryPacket> InventoryPackets { get; private set; } = new(50);
		public Queue<CommonPlayerPacket> CommonPlayerPackets { get; private set; } = new(50);
		public Queue<HealthSyncPacket> HealthSyncPackets { get; private set; } = new(50);
		private readonly Queue<GClass3119> inventoryOperations = new();

		protected void Awake()
		{
			player = GetComponent<CoopPlayer>();
			if (!player.IsYourPlayer)
			{
				observedPlayer = GetComponent<ObservedCoopPlayer>();
			}
		}

		protected void Start()
		{
			if (FikaBackendUtils.IsServer)
			{
				Server = Singleton<FikaServer>.Instance;
			}
			else
			{
				Client = Singleton<FikaClient>.Instance;
			}
		}

		protected void OnDestroy()
		{
			FirearmPackets.Clear();
			DamagePackets.Clear();
			ArmorDamagePackets.Clear();
			InventoryPackets.Clear();
			CommonPlayerPackets.Clear();
			HealthSyncPackets.Clear();
			inventoryOperations.Clear();
		}

		protected void Update()
		{
			if (observedPlayer != null)
			{
				int healthSyncPackets = HealthSyncPackets.Count;
				if (healthSyncPackets > 0)
				{
					for (int i = 0; i < healthSyncPackets; i++)
					{
						HealthSyncPacket packet = HealthSyncPackets.Dequeue();
						if (packet.Packet.SyncType == GStruct358.ESyncType.IsAlive && !packet.Packet.Data.IsAlive.IsAlive)
						{
							observedPlayer.SetAggressor(packet.KillerId);
							observedPlayer.CorpseSyncPacket = packet.CorpseSyncPacket;
							if (packet.TriggerZones.Length > 0)
							{
								observedPlayer.TriggerZones.Clear();
								foreach (string triggerZone in packet.TriggerZones)
								{
									observedPlayer.TriggerZones.Add(triggerZone);
								}
							}
						}
						observedPlayer.NetworkHealthController.HandleSyncPacket(packet.Packet);
					}
				}
			}
			if (player == null)
			{
				return;
			}
			int firearmPackets = FirearmPackets.Count;
			if (firearmPackets > 0)
			{
				for (int i = 0; i < firearmPackets; i++)
				{
					player.HandleWeaponPacket(FirearmPackets.Dequeue());
				}
			}
			int damagePackets = DamagePackets.Count;
			if (damagePackets > 0)
			{
				for (int i = 0; i < damagePackets; i++)
				{
					DamagePacket damagePacket = DamagePackets.Dequeue();
					player.HandleDamagePacket(ref damagePacket);
				}
			}
			int armorDamagePackets = ArmorDamagePackets.Count;
			if (armorDamagePackets > 0)
			{
				for (int i = 0; i < armorDamagePackets; i++)
				{
					ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
					player.HandleArmorDamagePacket(ref armorDamagePacket);
				}
			}
			int inventoryPackets = InventoryPackets.Count;
			if (inventoryPackets > 0)
			{
				for (int i = 0; i < inventoryPackets; i++)
				{
					ConvertInventoryPacket(InventoryPackets.Dequeue());
				}
			}
			int commonPlayerPackets = CommonPlayerPackets.Count;
			if (commonPlayerPackets > 0)
			{
				for (int i = 0; i < commonPlayerPackets; i++)
				{
					player.HandleCommonPacket(CommonPlayerPackets.Dequeue());
				}
			}
			int inventoryOps = inventoryOperations.Count;
			if (inventoryOps > 0)
			{
				if (inventoryOperations.Peek().WaitingForForeignEvents())
				{
					return;
				}
				inventoryOperations.Dequeue().method_1(HandleResult);
			}
		}

		private void ConvertInventoryPacket(in InventoryPacket packet)
		{
			if (packet.HasItemControllerExecutePacket)
			{
				if (packet.ItemControllerExecutePacket.OperationBytes.Length == 0)
				{
					FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Bytes were null!");
					return;
				}

				InventoryController controller = player.InventoryController;
				if (controller != null)
				{
					try
					{
						if (controller is Interface15 networkController)
						{
							GClass1170 reader = new(packet.ItemControllerExecutePacket.OperationBytes);
							GClass1670 descriptor = reader.ReadPolymorph<GClass1670>();
							GStruct423 result = networkController.CreateOperationFromDescriptor(descriptor);
							if (!result.Succeeded)
							{
								FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Unable to process descriptor from netId {packet.NetId}, error: {result.Error}");
								return;
							}

							inventoryOperations.Enqueue(result.Value);
						}
					}
					catch (Exception exception)
					{
						FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Exception thrown: {exception}");
					}
				}
				else
				{
					FikaPlugin.Instance.FikaLogger.LogError("ConvertInventoryPacket: inventory was null!");
				}
			}
		}

		private void HandleResult(IResult result)
		{
			if (result.Failed)
			{
				FikaPlugin.Instance.FikaLogger.LogError($"Error in operation: {result.Error}");
			}
		}
	}
}
