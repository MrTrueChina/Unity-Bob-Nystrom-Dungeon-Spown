using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Zone
{
    Quad[] GetQuads();
    bool Contains(Quad quad);
}
