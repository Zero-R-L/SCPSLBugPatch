using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using Steam;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(NetManager), nameof(NetManager.OnMessageReceived))]
    internal class OnMessageReceivedPatch
    {
        private class BadDataInfo
        {
            internal long PacketCount;
            internal long DataBytes;
            internal HashSet<string> RemoteAddresses;
            internal BadDataInfo()
            {
                PacketCount = 0;
                DataBytes = 0;
                RemoteAddresses = new HashSet<string>();
            }
        }
        private static BadDataInfo Info;
        private const string BadDataInfoFormat = @"
===================Round Potential DDoS Detected===================
Bad Packet Count =>> {0}
Total Size =>> {1}
Total Remote Addresses Count (May Not the DDoS Source Address) =>> {2}↓
{3}
===================================================================";
        internal static void Initialize()
        {
            Info = new BadDataInfo();
        }
        internal static string GetBadDataInfo()
        {
            long count = Info.PacketCount;
            long size = Info.DataBytes;
            HashSet<string> remoteAddresses = Info.RemoteAddresses;
            return string.Format(BadDataInfoFormat, count, BytesToString(size), remoteAddresses.Count, string.Join("\r\n", remoteAddresses));
            string BytesToString(long byteCount)
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
            if (Info.RemoteAddresses.Count != 0)
            {
                Plugin.AddLog(GetBadDataInfo());
            }
            else
            {
                Plugin.AddLog("No Bad Packet");
            }
        }
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

            if (!packet.Verify())
            {
                if (packet.RawData.Length >= 5 && packet.RawData[4] == 84)
                {
                    _ = __instance._udpSocketv4.SendTo(SteamServerInfo.Serialize(), SocketFlags.None, remoteEndPoint);
                    __instance.PoolRecycle(packet);
                }
                else
                {
                    Info.PacketCount++;
                    Info.DataBytes += size;
                    _ = Info.RemoteAddresses.Add(remoteEndPoint.Address.ToString());
                    //NetDebug.WriteError("[NM] DataReceived: bad!");
                    __instance.PoolRecycle(packet);
                }

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
            bool flag = __instance._peersDict.TryGetValue(remoteEndPoint, out NetPeer value2);
            __instance._peersLock.ExitReadLock();
            if (flag && __instance.EnableStatistics)
            {
                value2.Statistics.IncrementPacketsReceived();
                value2.Statistics.AddBytesReceived(size);
            }

            switch (packet.Property)
            {
                case PacketProperty.ConnectRequest:
                    {
                        NetConnectRequestPacket netConnectRequestPacket = NetConnectRequestPacket.FromData(packet);
                        if (netConnectRequestPacket != null)
                        {
                            __instance.ProcessConnectRequest(remoteEndPoint, value2, netConnectRequestPacket);
                        }

                        break;
                    }
                case PacketProperty.PeerNotFound:
                    if (flag)
                    {
                        if (value2.ConnectionState == ConnectionState.Connected)
                        {
                            if (packet.Size == 1)
                            {
                                value2.ResetMtu();
                                _ = __instance.SendRaw(NetConnectAcceptPacket.MakeNetworkChanged(value2), remoteEndPoint);
                            }
                            else if (packet.Size == 2 && packet.RawData[1] == 1)
                            {
                                __instance.DisconnectPeerForce(value2, DisconnectReason.PeerNotFound, SocketError.Success, null);
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
                    if (flag && value2.ConnectionState == ConnectionState.Outgoing)
                    {
                        __instance.DisconnectPeerForce(value2, DisconnectReason.InvalidProtocol, SocketError.Success, null);
                    }

                    break;
                case PacketProperty.Disconnect:
                    if (flag)
                    {
                        DisconnectResult disconnectResult = value2.ProcessDisconnect(packet);
                        if (disconnectResult == DisconnectResult.None)
                        {
                            __instance.PoolRecycle(packet);
                            break;
                        }

                        __instance.DisconnectPeerForce(value2, (disconnectResult == DisconnectResult.Disconnect) ? DisconnectReason.RemoteConnectionClose : DisconnectReason.ConnectionRejected, SocketError.Success, packet);
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
                        if (netConnectAcceptPacket2 != null && value2.ProcessConnectAccept(netConnectAcceptPacket2))
                        {
                            __instance.CreateEvent(NetEvent.EType.Connect, value2, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
                        }
                    }

                    break;
                default:
                    if (flag)
                    {
                        value2.ProcessPacket(packet);
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
}
