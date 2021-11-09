using System.Collections;
using UnityEngine;

//this node functions as not allowing any forklifts to pass if one forklift is already in this section
public class BlockNode : LineNode
{
    public string blockId = "";
    public enum BlockType{ ENTRANCE, EXIT};
    public BlockType blockType = BlockType.ENTRANCE;

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.DrawIcon(transform.position, "stop");
    }
}