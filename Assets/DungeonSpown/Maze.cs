using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze
{
    List<Quad> _quads = new List<Quad>();

    public void AddQuad(Quad quad)
    {
        _quads.Add(quad);
    }
}
