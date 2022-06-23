using UnityEditor;
using UnityEngine;

public class TreeNode
{
    public QuadTreeTerrain QuadTree;
    public TreeNode Parent;
    public TreeNode[] Children;
    public Vector2 Position;
    public Vector3Int Key;
    public float Size;
    public bool IsLeaf = false;
    public bool IsDirty = false;

    private bool[] lodSeams = new bool[4];
    private GameObject gameObject;

    public TreeNode(QuadTreeTerrain quadTree, TreeNode parent, Vector2 position, float size)
    {
        this.QuadTree = quadTree;
        this.Parent = parent;
        this.Position = position;
        this.Size = size;
        this.Key = new Vector3Int((int)position.x, (int)position.y, (int)size);
    }

    public bool CanTraverseDown()
    {
        return Distance() < Size * 1.75f && Size * 0.5f > QuadTree.MinSize;
    }

    public bool CanTraverseUp()
    {
        return Parent != null && !Parent.CanTraverseDown();
    }

    public float Distance()
    {
        Vector2 cameraPosition = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
        return Vector2.Distance(cameraPosition, Position + new Vector2(Size, Size) / 2f);
    }

    public void SetIsLeaf(bool isLeaf)
    {
        if (isLeaf != this.IsLeaf)
        {
            if (isLeaf)
            {
                QuadTree.AddLeave(this);
                LoadMesh();
            }
            else
            {
                QuadTree.RemoveLeave(this);
                UnloadMesh();
            }
            this.IsLeaf = isLeaf;
        }
    }

    public void AddChildren()
    {
        float halfSize = Size / 2.0f;
        Children = new TreeNode[]
        {
            new TreeNode(QuadTree, this, new Vector2(Position.x, Position.y), halfSize),
            new TreeNode(QuadTree, this, new Vector2(Position.x + halfSize, Position.y), halfSize),
            new TreeNode(QuadTree, this, new Vector2(Position.x, Position.y + halfSize), halfSize),
            new TreeNode(QuadTree, this, new Vector2(Position.x + halfSize, Position.y + halfSize), halfSize)
        };
    }

    public void RemoveChildren()
    {
        Children = null;
    }

    public void Update()
    {
        if (CanTraverseDown())
        {
            SetIsLeaf(false);

            if (Children == null)
            {
                AddChildren();
            }

            foreach (var child in Children)
            {
                child.Update();
            }
        }
        else
        {
            SetIsLeaf(true);
        }
    }

    public void LoadMesh()
    {
        gameObject = new GameObject();
        gameObject.transform.parent = QuadTree.transform;
        gameObject.transform.localPosition = new Vector3(Position.x, 0, Position.y);
        gameObject.name = Position.ToString() + "_" + Size;
        gameObject.AddComponent<MeshRenderer>().sharedMaterial = QuadTree.Material;
        gameObject.AddComponent<MeshFilter>();
        IsDirty = true;
    }

    public void UnloadMesh()
    {
        GameObject.Destroy(gameObject);
    }

    public void UpdateMesh()
    {
        gameObject.transform.localScale = new Vector3(Size / 32f, 1, Size / 32f);
        gameObject.GetComponent<MeshFilter>().sharedMesh = QuadTree.GetMesh(lodSeams);
        IsDirty = false;
    }

    public void UpdateLodSeams()
    {
        int grid = (int)Size * 2;

        Vector3Int topKey1 = new Vector3Int((int)Position.x, (Mathf.FloorToInt((Position.y + Size) / grid) * grid), (int)(Size * 2));
        Vector3Int topKey2 = new Vector3Int((int)(Position.x - Size), (Mathf.FloorToInt((Position.y + Size) / grid) * grid), (int)(Size * 2));
        bool top = QuadTree.Neighbors.ContainsKey(topKey1) || QuadTree.Neighbors.ContainsKey(topKey2);
        if (top != lodSeams[0])
        {
            lodSeams[0] = top;
            IsDirty = true;
        }

        Vector3Int bottomKey1 = new Vector3Int((int)Position.x, (Mathf.FloorToInt((Position.y - Size) / grid) * grid), (int)(Size * 2));
        Vector3Int bottomKey2 = new Vector3Int((int)(Position.x - Size), (Mathf.FloorToInt((Position.y - Size) / grid) * grid), (int)(Size * 2));
        bool bottom = QuadTree.Neighbors.ContainsKey(bottomKey1) || QuadTree.Neighbors.ContainsKey(bottomKey2);
        if (bottom != lodSeams[1])
        {
            lodSeams[1] = bottom;
            IsDirty = true;
        }

        Vector3Int leftKey1 = new Vector3Int(Mathf.FloorToInt((Position.x - Size) / grid) * grid, (int)Position.y, (int)(Size * 2));
        Vector3Int leftKey2 = new Vector3Int(Mathf.FloorToInt((Position.x - Size) / grid) * grid, (int)(Position.y - Size), (int)(Size * 2));
        bool left = QuadTree.Neighbors.ContainsKey(leftKey1) || QuadTree.Neighbors.ContainsKey(leftKey2);
        if (left != lodSeams[2])
        {
            lodSeams[2] = left;
            IsDirty = true;
        }

        Vector3Int rightKey1 = new Vector3Int(Mathf.FloorToInt((Position.x + Size) / grid) * grid, (int)Position.y, (int)(Size * 2));
        Vector3Int rightKey2 = new Vector3Int(Mathf.FloorToInt((Position.x + Size) / grid) * grid, (int)(Position.y - Size), (int)(Size * 2));
        bool right = QuadTree.Neighbors.ContainsKey(rightKey1) || QuadTree.Neighbors.ContainsKey(rightKey2);
        if (right != lodSeams[3])
        {
            lodSeams[3] = right;
            IsDirty = true;
        }
    }

    public void DrawDebug()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(Position.x, 0, Position.y), new Vector3(Position.x, 4, Position.y));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(Position.x + Size * 0.5f, 0, Position.y + Size * 0.5f), new Vector3(Size, 0, Size));

        Handles.Label(new Vector3(Position.x + Size * 0.5f, 0, Position.y + Size * 0.5f), Position.ToString());

        if (lodSeams[0])
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(Position.x + Size * 0.5f, 1, Position.y + Size), new Vector3(Size, 16, 1f));
        }
        if (lodSeams[1])
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(Position.x + Size * 0.5f, 1, Position.y), new Vector3(Size, 16, 1f));
        }
        if (lodSeams[2])
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(Position.x, 1, Position.y + Size * 0.5f), new Vector3(1f, 16, Size));
        }
        if (lodSeams[3])
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(Position.x + Size, 1, Position.y + Size * 0.5f), new Vector3(1f, 16, Size));
        }
    }
}