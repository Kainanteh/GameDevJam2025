using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathRequest
{
    public Vector3 destino;
    public Building target;

    public PathRequest(Vector3 destino, Building target)
    {
        this.destino = destino;
        this.target = target;
    }
}
