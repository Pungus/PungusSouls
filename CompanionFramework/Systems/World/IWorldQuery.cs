using System.Collections.Generic;
using UnityEngine;

namespace Systems.World
{
    public interface IWorldQuery
    {
        IEnumerable<T> Find<T>(Vector3 position, float radius);
    }
}