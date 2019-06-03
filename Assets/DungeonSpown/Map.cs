using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map
{
    public int width
    {
        get
        {
            return _quads.GetLength(0);
        }
    }

    public int height
    {
        get
        {
            return _quads.GetLength(1);
        }
    }

    Quad[,] _quads;

    public Map(int width, int height)
    {
        _quads = new Quad[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _quads[x, y] = new Quad(new Vector2(x, y), QuadType.WALL);
    }

    public Quad GetQuad(Vector2 position)
    {
        return GetQuad((int)position.x, (int)position.y);
    }

    public Quad GetQuad(int x, int y)
    {
        return _quads[x, y];
    }

    public Quad[] GetQuadArray()
    {
        List<Quad> quads = new List<Quad>();

        foreach (Quad quad in _quads)
            quads.Add(quad);

        return quads.ToArray();
    }

    public void SetQuadType(Vector2 position, QuadType type)
    {
        SetQuadType((int)position.x, (int)position.y, type);
    }

    public void SetQuadType(int x, int y, QuadType type)
    {
        _quads[x, y].quadType = type;
    }

    public QuadType GetQuadType(Vector2 position)
    {
        return GetQuadType((int)position.x, (int)position.y);
    }

    public QuadType GetQuadType(int x, int y)
    {
        return _quads[x, y].quadType;
    }

    public bool Contains(Vector2 position)
    {
        return Contains((int)position.x, (int)position.y);
    }

    public bool Contains(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
