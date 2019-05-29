using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Spown Data", menuName = "Dungeon Spowner/Spown Data")]
public class SpownData : ScriptableObject
{
    public int width
    {
        get { return _width; }
    }
    [SerializeField]
    int _width;
    public int height
    {
        get { return _height; }
    }
    [SerializeField]
    int _height;

    public int spownRoomTime
    {
        get { return _spownRoomTime; }
    }
    [SerializeField]
    int _spownRoomTime;
    public int minRoomWidth
    {
        get { return _minRoomWidth; }
    }
    [SerializeField]
    int _minRoomWidth;
    public int maxRoomWidth
    {
        get { return _maxRoomWidth; }
    }
    [SerializeField]
    int _maxRoomWidth;
    public int minRoomHeight
    {
        get { return _minRoomHeight; }
    }
    [SerializeField]
    int _minRoomHeight;
    public int maxRoomHeight
    {
        get { return _maxRoomHeight; }
    }
    [SerializeField]
    int _maxRoomHeight;
}
