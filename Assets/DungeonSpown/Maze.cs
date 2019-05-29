using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : Zone
{
    List<Quad> _quads = new List<Quad>();

    public void AddQuad(Quad quad)
    {
        _quads.Add(quad);
    }

    public Quad[] GetQuads()
    {
        return _quads.ToArray();
    }

    public bool Contains(Quad quad)
    {
        return _quads.Contains(quad);
    }
}
