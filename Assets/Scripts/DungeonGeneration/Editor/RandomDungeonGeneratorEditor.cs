using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AbstarctDungeonGenerator),true)]
public class RandomDungeonGenerator : Editor
{
    AbstarctDungeonGenerator generator;

    private void Awake()
    {
        generator = (AbstarctDungeonGenerator) target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Create Dungeon"))
        {
            generator.generateDungeon();
        }
    }
}
