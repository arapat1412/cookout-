
using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public int colorId;
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;
    public Team teamId;
    public int hatId;

    public PlayerRole role;

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId &&
            colorId == other.colorId &&
            playerName == other.playerName &&
            playerId == other.playerId &&
            teamId == other.teamId &&
            hatId == other.hatId &&
            role == other.role;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref colorId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref teamId); // ✅ THÊM DÒNG NÀY
        serializer.SerializeValue(ref hatId);

        serializer.SerializeValue(ref role);
    }
}