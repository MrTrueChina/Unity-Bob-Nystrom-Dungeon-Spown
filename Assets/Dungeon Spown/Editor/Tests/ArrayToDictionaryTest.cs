using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using System.Linq;

/// <summary>
/// 用于测试数组转正数Key Dictionry 的类
/// </summary>
[TestFixture]
public class ArrayToDictionaryTest
{
    int[] _array = { 0, 2, 3, 5, 0, -1, -3, 7, 6 };
    Dictionary<int, int> _target = new Dictionary<int, int>
    {
        { 0, 0 },
        { 1, 2 },
        { 2, 3 },
        { 3, 5 },
        { 4, 0 },
        { 5, -1 },
        { 6, -3 },
        { 7, 7 },
        { 8, 6 },
    };

    [Test]
    public void DictionarySequenceEqual()
    {
        Dictionary<int, int> a = new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 }, };
        Dictionary<int, int> b = new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 }, };

        if (a.SequenceEqual(b))
            Debug.Log("Dictionary<int, int>.SequenceEqual 是根据值的");
        else
            Debug.Log("Dictionary<int, int>.SequenceEqual 不是根据值的");
    }

    [Test]
    public void ToDictionary()
    {
        try
        {
            Dictionary<int, int> dectionary = _array.ToDictionary(i => i);
            LogResult(dectionary, "_array.ToDictionary(i => i)");
        }
        catch
        {
            LogOnException("_array.ToDictionary(i => i)");
        }
    }

    [Test]
    public void SelectToDictionary()
    {
        try
        {
            Dictionary<int, int> dectionary = _array.Select((value, index) => new { value, index }).ToDictionary(item => item.index, item => item.value);
            //IEnumerable.Select(IEnumerable<TSource>, Func<TSource,Int32,TResult>)，Linq提供的扩展方法，效果类似于SQL的Select，将元素选择出来然后包装起来返回
            //第一个参数 IEnumerable<TSource>，由于是扩展方法，这个参数是调用对象，不传入
            //第二个参数 Func<TSource,Int32,TResult>，TSource是元素，int是这个元素的下标，TResult是这个方法的返回值
            //Select 中直接 new 了一个匿名类，这个匿名类的变量名就是传进去的变量的变量名，在后面的 ToDictionary 里直接使用这个变量名取值就行
            LogResult(dectionary, "_array.Select((index, value) => new { index, value }).ToDictionary(item => item.index, item => item.value)");
        }
        catch
        {
            LogOnException("_array.Select((index, value) => new { index, value }).ToDictionary(item => item.index, item => item.value)");
        }
    }

    void LogResult(Dictionary<int, int> result, string method)
    {
        foreach (KeyValuePair<int,int> pair in result)
            Debug.Log(pair);
        //SequenceEqual：序列相等，对比两个对象的序列化结果是否相等，可以用于判断集合是否在内容的值上相等
        if (result.SequenceEqual(_target))
            Debug.Log(method + " 可以获得下标为 Key 值为 value 的 Dictionary");
        else
            Debug.Log(method + " 不可以获得下标为 Key 值为 value 的 Dictionary");
    }

    void LogOnException(string method)
    {
        Debug.Log(method + " 会抛异常");
    }
}
