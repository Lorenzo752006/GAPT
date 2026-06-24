using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonPatternData", menuName = "Dungeon/PatternData")]
public class DungeonData : ScriptableObject
{
    // These lists act as the "Cheat Sheet" for your generator
    public List<bool> horizontalTrue = new List<bool>();
    public List<bool> horizontalFalse = new List<bool>();
    public List<bool> verticalTrue = new List<bool>();
    public List<bool> verticalFalse = new List<bool>();
    public List<bool> diagonalTrue = new List<bool>();
    public List<bool> diagonalFalse = new List<bool>();
}