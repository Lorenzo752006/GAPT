using System.Collections.Generic;
using UnityEngine;

public class GOAPAction7
{
    public string actionName;
    public Transform target;

    public Dictionary<string, bool> preconditions = new Dictionary<string, bool>();
    public Dictionary<string, bool> effects = new Dictionary<string, bool>();

    public GOAPAction7(string actionName)
    {
        this.actionName = actionName;
    }
}