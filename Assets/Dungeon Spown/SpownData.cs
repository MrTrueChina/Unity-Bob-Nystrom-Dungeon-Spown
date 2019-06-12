using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MtC.Tools.Random;
using System.Linq;

[CreateAssetMenu(fileName = "Spown Data", menuName = "Dungeon Spowner/Spown Data")]
public class SpownData : ScriptableObject
{
    public int width
    {
        get { return _width; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _width;
    public int height
    {
        get { return _height; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _height;

    public int spownRoomTime
    {
        get { return _spownRoomTime; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _spownRoomTime;
    public int minRoomWidth
    {
        get { return _minRoomWidth; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _minRoomWidth;
    public int maxRoomWidth
    {
        get { return _maxRoomWidth; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _maxRoomWidth;
    public int minRoomHeight
    {
        get { return _minRoomHeight; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _minRoomHeight;
    public int maxRoomHeight
    {
        get { return _maxRoomHeight; }
    }
#pragma warning disable 0649
    [SerializeField]
    int _maxRoomHeight;

#pragma warning disable 0649
    [SerializeField]
    int[] _roomDoorNumberProbability;

    WeightedRandom doorsNumberRandom
    {
        get
        {
            if (_doorsNumberRandom != null)
                return _doorsNumberRandom;

            lock (this)
            {
                if (_doorsNumberRandom == null)
                    _doorsNumberRandom = new WeightedRandom(
                        _roomDoorNumberProbability
                        .Select((probability, index) => new { index, probability })
                        .ToDictionary(item => item.index, item => item.probability)
                    );
                //Select<TSource,TResult>(IEnumerable<TSource>, Func<TSource,Int32,TResult>)：将原来的集合中的元素和索引结合起来转化为新的集合
                //这个方法和 SQL 里的 select 十分像，获取原来的集合中的所有元素，之后把他们用集合的形式打包返回
                //这个方法是 Linq 提供的扩展方法，所以 IEnumerable<TSource> 参数并不需要传，而是直接把调用的集合作为参数
                //Func<TSource,Int32,TResult> 参数要传入一个有两个参数一个返回值的方法，其中第一个参数是原本列表里的元素，第二个参数是这个元素的下标，返回值则是存入到新的集合中的对象
            }

            return _doorsNumberRandom;
        }
    }
    WeightedRandom _doorsNumberRandom;

    /// <summary>
    /// 根据房间门数量概率随机获取一个房间的门的数量
    /// </summary>
    /// <returns></returns>
    public int GetRoomDoorNumber()
    {
        return doorsNumberRandom.GetInt();
    }
}
