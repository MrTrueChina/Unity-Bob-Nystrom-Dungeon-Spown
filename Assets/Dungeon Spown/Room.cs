using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : Area
{
    Quad[,] _quads;
    Rect _rect;

    public Room(Map map, Rect rect)
    {
        _rect = rect;
        _quads = new Quad[(int)_rect.width, (int)_rect.height];

        GetQuadsFromMap(map);
        FillFloor();
    }

    void GetQuadsFromMap(Map map)
    {
        for (int x = 0; x < _rect.width; x++)
            for (int y = 0; y < _rect.height; y++)
                _quads[x, y] = map.GetQuad(x + (int)_rect.x, y + (int)_rect.y);
    }

    void FillFloor()
    {
        for (int x = 0; x < _rect.width; x++)
            for (int y = 0; y < _rect.height; y++)
                _quads[x, y].quadType = QuadType.FLOOR;
    }

    /// <summary>
    /// 判断房间是否和指定Rect相邻或重叠
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public bool AdjoinsOrOverlaps(Rect rect)
    {
        return new Rect(_rect.x - 1, _rect.y - 1, _rect.width + 2, _rect.height + 2).Overlaps(rect);
    }

    public Quad[] GetQuads()
    {
        int width = _quads.GetLength(0);
        int height = _quads.GetLength(1);

        Quad[] quadArray = new Quad[width*height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                quadArray[x + y * width] = _quads[x, y];

        return quadArray;
    }

    public bool Contains(Quad quad)
    {
        foreach (Quad thisQuad in _quads)
            if (thisQuad == quad)
                return true;
        return false;
    }
}
