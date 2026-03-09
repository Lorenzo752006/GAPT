using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "2D/Tiles/Specific Sibling Tile")]
public class SiblingRuleTile : RuleTile {

    public List<TileBase> siblings = new List<TileBase>();

    public override bool RuleMatch(int neighbor, TileBase other) {
        switch (neighbor) {
            case TilingRuleOutput.Neighbor.This:
                // GREEN ARROW: Only matches the EXACT same tile asset
                return other == this;
                
            case TilingRuleOutput.Neighbor.NotThis:

                if (other == null) return false;

                if (siblings.Contains(other)) return true;
                return false;
        }
        return base.RuleMatch(neighbor, other);
    }
}