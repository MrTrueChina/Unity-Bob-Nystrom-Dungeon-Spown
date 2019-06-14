using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConnectPointState
{
    /// <summary>
    /// 不可使用的连接点状态，用于填充没有连接点的位置
    /// </summary>
    Disable = 0,
    /// <summary>
    /// 连接到没有连接到的区域
    /// </summary>
    ConnectToUnconnectedZone = 1,
    /// <summary>
    /// 连接到已经连接的区域，但还可以在开多个门的时候使用
    /// </summary>
    ConnectToMainZone = 2,
    /// <summary>
    /// 连接到已经连接的区域，并且已经在开多个门时使用或所在位置的墙已经被打通，不再进行使用
    /// </summary>
    Used = 3,
}

public class ConnectPointMap
{
    ConnectPointState[,] _connectPoints;

    public ConnectPointMap(int width, int height)
    {
        _connectPoints = new ConnectPointState[width, height];
    }

    public void AddConnectPoint(Vector2 position)
    {
        //地图生成过程中只会生成一次连接点，就是在开始连接之前，所以添加连接点的操作就是把连接点状态设为连接到未连接区域
        _connectPoints[(int)position.x, (int)position.y] = ConnectPointState.ConnectToUnconnectedZone;
    }

    public void ChangeConnectPointToConnectToMainZone(Vector2 position)
    {
        _connectPoints[(int)position.x, (int)position.y] = ConnectPointState.ConnectToMainZone;
    }

    public void ChangeConnectPointToUsed(Vector2 position)
    {
        _connectPoints[(int)position.x, (int)position.y] = ConnectPointState.Used;
    }

    public bool IsConnectToUnconnectedZone(Vector2 position)
    {
        return _connectPoints[(int)position.x, (int)position.y] == ConnectPointState.ConnectToUnconnectedZone;
    }
}
