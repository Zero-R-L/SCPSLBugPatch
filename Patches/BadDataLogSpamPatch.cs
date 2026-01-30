using HarmonyLib;
using LabApi.Features.Console;
using LiteNetLib;
using LiteNetLib.Utils;
using Steam;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using static LiteNetLib.NetManager;

namespace SCPSLBugPatch.Patches
{
    internal static class BadDataLogSpamPatch
    {
        private const string BadDataInfoFormat = @"
===================Round Potential DDoS Detected===================
Bad Packet Count =>> {0}
Total Size =>> {1}
Total Remote Addresses Count (May Not the DDoS Source Address) =>> {2}↓
{3}
===================================================================";

        internal static long PacketCount;
        internal static long DataBytes;
        internal static HashSet<string> RemoteAddresses;
        internal static void Initialize()
        {
            PacketCount = 0;
            DataBytes = 0;
            RemoteAddresses = new HashSet<string>();
        }
        internal static string GetBadDataInfo()
        {
            return string.Format(BadDataInfoFormat, PacketCount, BytesToString(DataBytes), RemoteAddresses.Count, string.Join("\r\n", RemoteAddresses));
            static string BytesToString(long byteCount)
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                {
                    return "0" + suf[0];
                }

                long bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }
        }
        internal static void LogBadDataInfo()
        {
            if (RemoteAddresses.Count != 0)
            {
                MainClass.AddLog(GetBadDataInfo());
            }
            else
            {
                Logger.Info("No Bad Packet");
            }
        }
        [HarmonyPatch(typeof(NetManager), nameof(NetManager.OnMessageReceived))]
        private static class A
        {
            private static bool Prefix(NetPacket packet, IPEndPoint remoteEndPoint, NetManager __instance)
            {
                int size = packet.Size;
                if (__instance.EnableStatistics)
                {
                    __instance.Statistics.IncrementPacketsReceived();
                    __instance.Statistics.AddBytesReceived(size);
                }

                if (__instance._ntpRequests.Count > 0 && __instance._ntpRequests.TryGetValue(remoteEndPoint, out _))
                {
                    if (packet.Size >= 48)
                    {
                        byte[] array = new byte[packet.Size];
                        Buffer.BlockCopy(packet.RawData, 0, array, 0, packet.Size);
                        NtpPacket ntpPacket = NtpPacket.FromServerResponse(array, DateTime.UtcNow);
                        try
                        {
                            ntpPacket.ValidateReply();
                        }
                        catch (InvalidOperationException)
                        {
                            ntpPacket = null;
                        }

                        if (ntpPacket != null)
                        {
                            _ = __instance._ntpRequests.Remove(remoteEndPoint);
                            __instance._ntpEventListener?.OnNtpResponse(ntpPacket);
                        }
                    }

                    return false;
                }

                if (__instance._extraPacketLayer != null)
                {
                    int offset = 0;
                    __instance._extraPacketLayer.ProcessInboundPacket(ref remoteEndPoint, ref packet.RawData, ref offset, ref packet.Size);
                    if (packet.Size == 0)
                    {
                        return false;
                    }
                }

                //if (!packet.Verify())
                //{
                //    if (packet.RawData.Length >= 5 && packet.RawData[4] == 84)
                //    {
                //        _ = __instance._udpSocketv4.SendTo(SteamServerInfo.Serialize(), SocketFlags.None, remoteEndPoint);
                //        __instance.PoolRecycle(packet);
                //    }
                //    else
                //    {
                //        PacketCount++;
                //        DataBytes += size;
                //        _ = RemoteAddresses.Add(remoteEndPoint.Address.ToString());
                //        //NetDebug.WriteError("[NM] DataReceived: bad!");
                //        __instance.PoolRecycle(packet);
                //    }

                //    return false;
                //}

                if (!packet.Verify())
                {
                    if (packet.RawData.Length < 5 || packet.RawData[4] != 84)
                    {
                        PacketCount++;
                        DataBytes += size;
                        _ = RemoteAddresses.Add(remoteEndPoint.Address.ToString());
                        //NetDebug.WriteError("[NM] DataReceived: bad!");
                        __instance.PoolRecycle(packet);
                        return false;
                    }

                    long num = Stopwatch.GetTimestamp() - 40000000;
                    List<IPEndPoint> list = new List<IPEndPoint>();
                    foreach (KeyValuePair<IPEndPoint, ChallengeInfo> challenge in __instance._challenges)
                    {
                        if (challenge.Value.Timestamp < num)
                        {
                            list.Add(challenge.Key);
                        }
                    }

                    foreach (IPEndPoint item in list)
                    {
                        __instance._challenges.Remove(item);
                    }

                    if (__instance._challenges.TryGetValue(remoteEndPoint, out var value2))
                    {
                        int num2 = Array.IndexOf(packet.RawData, (byte)0) + 1;
                        if (num2 != 0 && packet.RawData.Length >= num2 + 4 && BitConverter.ToUInt32(packet.RawData, num2) == value2.Challenge)
                        {
                            __instance._challenges.Remove(remoteEndPoint);
                            __instance._udpSocketv4.SendTo(SteamServerInfo.Serialize(), SocketFlags.None, remoteEndPoint);
                            __instance.PoolRecycle(packet);
                        }
                        else
                        {
                            //NetDebug.WriteError("[NM] DataReceived: bad! Expected Challenge was not received.");
                            __instance.PoolRecycle(packet);
                        }

                        return false;
                    }

                    uint num3 = (uint)((Random)(object)__instance._random).Next();
                    __instance._challenges[remoteEndPoint] = new ChallengeInfo
                    {
                        Challenge = num3,
                        Timestamp = Stopwatch.GetTimestamp()
                    };
                    byte[] array2 = new byte[9];
                    for (int i = 0; i < 4; i++)
                    {
                        array2[i] = byte.MaxValue;
                    }

                    array2[4] = 65;
                    Buffer.BlockCopy(BitConverter.GetBytes(num3), 0, array2, 5, 4);
                    __instance._udpSocketv4.SendTo(array2, SocketFlags.None, remoteEndPoint);
                    __instance.PoolRecycle(packet);
                    return false;
                }

                switch (packet.Property)
                {
                    case PacketProperty.ConnectRequest:
                        if (NetConnectRequestPacket.GetProtocolId(packet) != 13)
                        {
                            _ = __instance.SendRawAndRecycle(__instance.PoolGetWithProperty(PacketProperty.InvalidProtocol), remoteEndPoint);
                            return false;
                        }

                        break;
                    case PacketProperty.Broadcast:
                        if (__instance.BroadcastReceiveEnabled)
                        {
                            __instance.CreateEvent(NetEvent.EType.Broadcast, null, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, packet);
                        }

                        return false;
                    case PacketProperty.UnconnectedMessage:
                        if (__instance.UnconnectedMessagesEnabled)
                        {
                            __instance.CreateEvent(NetEvent.EType.ReceiveUnconnected, null, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, packet);
                        }

                        return false;
                    case PacketProperty.NatMessage:
                        if (__instance.NatPunchEnabled)
                        {
                            __instance.NatPunchModule.ProcessMessage(remoteEndPoint, packet);
                        }

                        return false;
                }

                __instance._peersLock.EnterReadLock();
                bool flag = __instance._peersDict.TryGetValue(remoteEndPoint, out NetPeer value3);
                __instance._peersLock.ExitReadLock();
                if (flag && __instance.EnableStatistics)
                {
                    value3.Statistics.IncrementPacketsReceived();
                    value3.Statistics.AddBytesReceived(size);
                }

                switch (packet.Property)
                {
                    case PacketProperty.ConnectRequest:
                        {
                            NetConnectRequestPacket netConnectRequestPacket = NetConnectRequestPacket.FromData(packet);
                            if (netConnectRequestPacket != null)
                            {
                                __instance.ProcessConnectRequest(remoteEndPoint, value3, netConnectRequestPacket);
                            }

                            break;
                        }
                    case PacketProperty.PeerNotFound:
                        if (flag)
                        {
                            if (value3.ConnectionState == ConnectionState.Connected)
                            {
                                if (packet.Size == 1)
                                {
                                    value3.ResetMtu();
                                    _ = __instance.SendRaw(NetConnectAcceptPacket.MakeNetworkChanged(value3), remoteEndPoint);
                                }
                                else if (packet.Size == 2 && packet.RawData[1] == 1)
                                {
                                    __instance.DisconnectPeerForce(value3, DisconnectReason.PeerNotFound, SocketError.Success, null);
                                }
                            }
                        }
                        else
                        {
                            if (packet.Size <= 1)
                            {
                                break;
                            }

                            bool flag2 = false;
                            if (__instance.AllowPeerAddressChange)
                            {
                                NetConnectAcceptPacket netConnectAcceptPacket = NetConnectAcceptPacket.FromData(packet);
                                if (netConnectAcceptPacket != null && netConnectAcceptPacket.PeerNetworkChanged && netConnectAcceptPacket.PeerId < __instance._peersArray.Length)
                                {
                                    __instance._peersLock.EnterUpgradeableReadLock();
                                    NetPeer netPeer = __instance._peersArray[netConnectAcceptPacket.PeerId];
                                    if (netPeer != null && netPeer.ConnectTime == netConnectAcceptPacket.ConnectionTime && netPeer.ConnectionNum == netConnectAcceptPacket.ConnectionNumber)
                                    {
                                        if (netPeer.ConnectionState == ConnectionState.Connected)
                                        {
                                            netPeer.InitiateEndPointChange();
                                            if (__instance._peerAddressChangedListener != null)
                                            {
                                                __instance.CreateEvent(NetEvent.EType.PeerAddressChanged, netPeer, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
                                            }
                                        }

                                        flag2 = true;
                                    }

                                    __instance._peersLock.ExitUpgradeableReadLock();
                                }
                            }

                            __instance.PoolRecycle(packet);
                            if (!flag2)
                            {
                                NetPacket netPacket = __instance.PoolGetWithProperty(PacketProperty.PeerNotFound, 1);
                                netPacket.RawData[1] = 1;
                                _ = __instance.SendRawAndRecycle(netPacket, remoteEndPoint);
                            }
                        }

                        break;
                    case PacketProperty.InvalidProtocol:
                        if (flag && value3.ConnectionState == ConnectionState.Outgoing)
                        {
                            __instance.DisconnectPeerForce(value3, DisconnectReason.InvalidProtocol, SocketError.Success, null);
                        }

                        break;
                    case PacketProperty.Disconnect:
                        if (flag)
                        {
                            DisconnectResult disconnectResult = value3.ProcessDisconnect(packet);
                            if (disconnectResult == DisconnectResult.None)
                            {
                                __instance.PoolRecycle(packet);
                                break;
                            }

                            __instance.DisconnectPeerForce(value3, (disconnectResult == DisconnectResult.Disconnect) ? DisconnectReason.RemoteConnectionClose : DisconnectReason.ConnectionRejected, SocketError.Success, packet);
                        }
                        else
                        {
                            __instance.PoolRecycle(packet);
                        }

                        _ = __instance.SendRawAndRecycle(__instance.PoolGetWithProperty(PacketProperty.ShutdownOk), remoteEndPoint);
                        break;
                    case PacketProperty.ConnectAccept:
                        if (flag)
                        {
                            NetConnectAcceptPacket netConnectAcceptPacket2 = NetConnectAcceptPacket.FromData(packet);
                            if (netConnectAcceptPacket2 != null && value3.ProcessConnectAccept(netConnectAcceptPacket2))
                            {
                                __instance.CreateEvent(NetEvent.EType.Connect, value3, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
                            }
                        }

                        break;
                    default:
                        if (flag)
                        {
                            value3.ProcessPacket(packet);
                        }
                        else
                        {
                            _ = __instance.SendRawAndRecycle(__instance.PoolGetWithProperty(PacketProperty.PeerNotFound), remoteEndPoint);
                        }

                        break;
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ServerShutdown), nameof(ServerShutdown.Shutdown))]
        private static class B
        {
            private static void Prefix()
            {
                LogBadDataInfo();
            }
        }
    }
}
