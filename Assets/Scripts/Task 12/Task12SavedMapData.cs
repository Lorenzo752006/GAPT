using UnityEngine;
using System.Collections.Generic;



[CreateAssetMenu(fileName = "SavedMap", menuName = "Dungeon/SavedMap")]
public class Task12SavedMapData : ScriptableObject
{
    public List<Vector2Int> floorPositions = new List<Vector2Int>();
}
