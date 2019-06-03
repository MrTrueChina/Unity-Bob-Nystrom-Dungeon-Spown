using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quad
{
    public Vector2 position
    {
        get { return _position; }
    }
    Vector2 _position;
    public QuadType quadType
    {
        get { return _quadType; }
        set { _quadType = value; }
    }
    QuadType _quadType;

    public Quad(Vector2 position, QuadType quadType)
    {
        _position = position;
        _quadType = quadType;
    }

    public override string ToString()
    {
        return "Quad[" + position + "," + quadType + "]";
    }
}
