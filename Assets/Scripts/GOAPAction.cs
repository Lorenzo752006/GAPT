using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GOAPAction
{
    public string actionName;
    public Transform target;
    public float cost = 1f;

    public Dictionary<string, bool> preconditions = new Dictionary<string, bool>();
    public Dictionary<string, bool> effects = new Dictionary<string, bool>();

    public GOAPAction(string name)
    {
        actionName = name;
    }
}