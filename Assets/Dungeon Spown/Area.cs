using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Area
{
    Quad[] GetQuads();
    bool Contains(Quad quad);
}
