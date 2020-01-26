using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    [SerializeField] private List<EnvironmentTile> AccessibleTiles;
    [SerializeField] private List<EnvironmentTile> HighlightedTiles;
    [SerializeField] private List<EnvironmentTile> DangerTiles;
    [SerializeField] private List<EnvironmentTile> InaccessibleTiles;
    [SerializeField] private List<EnvironmentTile> DangerInaccessibleTiles;
    [SerializeField] private float AccessiblePercentage;

    public Vector2Int Size;
    public EnvironmentTile[][] mMap;

    //mAll not needed
    //private List<EnvironmentTile> mAll;

    private List<EnvironmentTile> mToBeTested;
    private List<EnvironmentTile> mLastSolution;

    private readonly Vector3 NodeSize = Vector3.one * 9.0f; 
    private const float TileSize = 10.0f;
    private const float TileHeight = 2.5f;

    public EnvironmentTile Start { get; private set; }

    //My variables
    [SerializeField] private Enemy Robot;
    [SerializeField] private Enemy Orc;

    private int WaterTiles = 9;
    public List<Enemy> Enemies;
    public int RoomType { get; set; }
    public int EnemyType { get; set; }

    private void Awake()
    {
        //mAll not needed
        //mAll = new List<EnvironmentTile>();

        mToBeTested = new List<EnvironmentTile>();
    }

    private void OnDrawGizmos()
    {
        // Draw the environment nodes and connections if we have them
        if (mMap != null)
        {
            for (int x = 0; x < Size.x; ++x)
            {
                for (int y = 0; y < Size.y; ++y)
                {
                    if (mMap[x][y].Connections != null)
                    {
                        for (int n = 0; n < mMap[x][y].Connections.Count; ++n)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawLine(mMap[x][y].Position, mMap[x][y].Connections[n].Position);
                        }
                    }

                    // Use different colours to represent the state of the nodes
                    Color c = Color.white;
                    if ( !mMap[x][y].IsAccessible )
                    {
                        c = Color.red;
                    }
                    else
                    {
                        if(mLastSolution != null && mLastSolution.Contains( mMap[x][y] ))
                        {
                            c = Color.green;
                        }
                        else if (mMap[x][y].Visited)
                        {
                            c = Color.yellow;
                        }
                    }

                    Gizmos.color = c;
                    Gizmos.DrawWireCube(mMap[x][y].Position, NodeSize);
                }
            }
        }
    }

    private void Generate(int roomType, int enemyType)
    {
        // Setup the map of the environment tiles according to the specified width and height
        // Generate tiles from the list of accessible and inaccessible prefabs using a random
        // and the specified accessible percentage

        int WaterSpace = 4;
        Size.x += WaterSpace;
        Size.y += WaterSpace;

        mMap = new EnvironmentTile[Size.x][];
        RoomType = roomType;
        EnemyType = enemyType;

        int halfWidth = Size.x / 2;
        int halfHeight = Size.y / 2;

        Vector3 position = new Vector3( -(halfWidth * TileSize), 0.0f, -(halfHeight * TileSize) );

        //Generate Tiles
        for ( int x = 0; x < Size.x ; ++x)
        {
            bool isAccessible;
            bool isPickupable = false;
            mMap[x] = new EnvironmentTile[Size.y ];
            for ( int y = 0; y < Size.y; ++y)
            {
                Vector3 offset = new Vector3(0,0,0);
                Quaternion rotation = Quaternion.identity;
                int num = -1;
                if (x < (WaterSpace / 2) || y < (WaterSpace / 2) || x > Size.x-1 - (WaterSpace / 2) || y > Size.y-1 - (WaterSpace / 2))
                {
                    isAccessible = false;
                    if (x == 0 || y == 0 || x == Size.x - 1 || y == Size.y - 1)
                        num = 16;
                    else
                    {
                        if (x == y || y + x == Size.x - 1)
                        {
                            num = 17;
                            if (x == 1 && y == 1)
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                                offset.z += 10;
                            }
                            else if (x == 1 && y == Size.y - 2)
                            {
                                rotation = Quaternion.Euler(0, 180, 0);
                                offset.z += 10;
                                offset.x += 10;
                            }
                            else if (x == Size.x - 2 && y == Size.y - 2)
                            {
                                rotation = Quaternion.Euler(0, 270, 0);
                                offset.x += 10;
                            }
                        }
                        else
                        {
                            num = 19;
                            if (y == Size.y - 2)
                            {
                                rotation = Quaternion.Euler(0, 90, 0);
                                offset.z += 10;
                            }
                            else if (y == 1)
                            {
                                rotation = Quaternion.Euler(0, 270, 0);
                                offset.x += 10;
                            }
                            else if (x == Size.x - 2)
                            {
                                rotation = Quaternion.Euler(0, 180, 0);
                                offset.z += 10;
                                offset.x += 10;
                            }
                        }
                    }
                }
                else if (x == (WaterSpace/2) || y == (WaterSpace/2) || x == Size.x - 1 - (WaterSpace / 2) || y == Size.y - 1 - (WaterSpace / 2))
                {
                    num = 0;
                    isAccessible = true;
                }
                else
                {
                    isAccessible = Random.value < AccessiblePercentage;
                }

                EnvironmentTile tile = GenerateTile(true, 0, num, position+offset, rotation, x, y, isAccessible, isPickupable);

                if (x == (WaterSpace / 2) && y == (WaterSpace / 2))
                {
                    Start = tile;
                }

                position.z += TileSize;
            }

            position.x += TileSize;
            position.z = -(halfHeight * TileSize);
        }

        //Generate Enemies
        Enemies = new List<Enemy>(0);
        for (int i = 0; i < RoomType; i++)
        {
            if (EnemyType == 0)
            {
                
                Enemies.Add(Instantiate(Robot, transform));
                GenerateEnemy(Enemies[i]);
            }
            if (EnemyType == 1)
            {
                Enemies.Add(Instantiate(Orc, transform));
                GenerateEnemy(Enemies[i]);
            }
        }
    }

    //made so individual tiles can be created
    public EnvironmentTile GenerateTile(bool Generation, int Type, int num, Vector3 position, Quaternion rotation, int x, int y, bool isAccessible, bool isPickupable)
    {
        EnvironmentTile prefab = null;
        if (Generation == false)
        {
            if (Type == 0 && num <= AccessibleTiles.Count)
                prefab = AccessibleTiles[num];
            else if (Type == 1 && num <= HighlightedTiles.Count)
                prefab = HighlightedTiles[num];
            else if (Type == 2 && num <= DangerTiles.Count)
                prefab = DangerTiles[num];
            else if (Type == 3 && num <= InaccessibleTiles.Count)
                prefab = InaccessibleTiles[num];
            else if (Type == 4 && num <= DangerInaccessibleTiles.Count)
                prefab = DangerInaccessibleTiles[num];
            position = new Vector3(position.x - (TileSize / 2), 0, position.z - (TileSize / 2));
        }
        else
        {
            List<EnvironmentTile> tiles = isAccessible ? AccessibleTiles : InaccessibleTiles;
            if (num == -1)
            {
                if (isAccessible)
                {
                    num = Random.Range(0, tiles.Count);
                    prefab = tiles[num];
                    if (num == 1)
                        isPickupable = true;
                }
                else
                {
                    num = Random.Range(0, tiles.Count - WaterTiles);
                    prefab = tiles[num];
                }
            }
            else
                prefab = tiles[num];
        }
        EnvironmentTile tile = Instantiate(prefab, position, rotation, transform);
        tile.Position = new Vector3(position.x + (TileSize / 2), TileHeight, position.z + (TileSize / 2));
        tile.Rotation = rotation;
        tile.Random = num;
        tile.X = x;
        tile.Y = y;
        tile.IsAccessible = isAccessible;
        tile.IsPickupable = isPickupable;
        tile.Connections = new List<EnvironmentTile>();
        tile.gameObject.name = string.Format("Tile({0},{1})", x, y);
        mMap[x][y] = tile;

        //mAll not needed
        //mAll.Add(tile);

        return tile;
    }

    private void GenerateEnemy(Enemy Instance)
    {
        int tempx, tempy;     
        tempx = Random.Range(0, (Size.x)-1);
        tempy = Random.Range(0, (Size.y)-1);
        while (mMap[tempx][tempy].Position == Start.Position || mMap[tempx][tempy].IsAccessible == false)
        {
            tempx = Random.Range(0, (Size.x) - 1);
            tempy = Random.Range(0, (Size.y) - 1);
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].transform.position == mMap[tempx][tempy].Position)
                {
                    tempx = Random.Range(0, (Size.x) - 1);
                    tempy = Random.Range(0, (Size.y) - 1);
                }
            }
        }
        Instance.transform.position = mMap[tempx][tempy].Position;
        Instance.transform.rotation = Quaternion.identity;
        Instance.CurrentPosition = mMap[tempx][tempy];
        Instance.game = GetComponentInParent<Game>();
        Instance.Hud = GetComponentInParent<Game>().GetComponentInChildren<HUD>();
        Instance.CurrentArea = this;
    }

    private void SetupConnections()
    {
        // Currently we are only setting up connections between adjacnt nodes      
        for (int x = 0; x < Size.x; ++x)
        {
            for (int y = 0; y < Size.y; ++y)
            {
                EnvironmentTile tile = mMap[x][y];
                Connect(tile,x,y);
            }
        }
    }

    public void NewEnemies()
    {
        Enemies = new List<Enemy>(0);
        for (int i = 0; i < RoomType; i++)
        {
            if (EnemyType == 0)
            {

                Enemies.Add(Instantiate(Robot, transform));
                GenerateEnemy(Enemies[i]);
            }
            if (EnemyType == 1)
            {
                Enemies.Add(Instantiate(Orc, transform));
                GenerateEnemy(Enemies[i]);
            }
        }
    }

    //Made so can be used seperatly by an individual tile
    public void Connect(EnvironmentTile tile, int x, int y)
    {
        if (x > 0)
        {
            tile.Connections.Add(mMap[x - 1][y]);
        }

        if (x < Size.x - 1)
        {
            tile.Connections.Add(mMap[x + 1][y]);
        }

        if (y > 0)
        {
            tile.Connections.Add(mMap[x][y - 1]);
        }

        if (y < Size.y - 1)
        {
            tile.Connections.Add(mMap[x][y + 1]);
        }
    }

    private float Distance(EnvironmentTile a, EnvironmentTile b)
    {
        // Use the length of the connection between these two nodes to find the distance, this 
        // is used to calculate the local goal during the search for a path to a location
        float result = float.MaxValue;
        EnvironmentTile directConnection = a.Connections.Find(c => c == b);
        if (directConnection != null)
        {
            result = TileSize;
        }
        return result;
    }

    private float Heuristic(EnvironmentTile a, EnvironmentTile b)
    {
        // Use the locations of the node to estimate how close they are by line of sight
        // experiment here with better ways of estimating the distance. This is used  to
        // calculate the global goal and work out the best order to prossess nodes in
        return Vector3.Distance(a.Position, b.Position);
    }

    public void GenerateWorld(int roomType, int enemyType)
    {
        Generate(roomType, enemyType);
        SetupConnections();
    }

    public void CleanUpWorld()
    {
        if (mMap != null)
        {
            for (int x = 0; x < Size.x; ++x)
            {
                for (int y = 0; y < Size.y; ++y)
                {
                    Destroy(mMap[x][y].gameObject);
                }
            }
        }
        //Update later with more maps
        for(int i = 0; i < Enemies.Count; i++)
        {
            Destroy(Enemies[i].gameObject);
        }
        Enemies.Clear();
    }

    public List<EnvironmentTile> Solve(EnvironmentTile begin, EnvironmentTile destination)
    {
        List<EnvironmentTile> result = null;
        
        if (begin != null && destination != null)
        {
            // Nothing to solve if there is a direct connection between these two locations
            EnvironmentTile directConnection = begin.Connections.Find(c => c == destination);
            if (directConnection == null)
            {
                // Set all the state to its starting values
                mToBeTested.Clear();

                //mAll not needed
                //for (int count = 0; count < mAll.Count; ++count)
                //{
                //    mAll[count].Parent = null;
                //    mAll[count].Global = float.MaxValue;
                //    mAll[count].Local = float.MaxValue;
                //    mAll[count].Visited = false;
                //}

                for (int i = 0; i < Size.x; ++i)
                {
                    for (int j = 0; j < Size.y; ++j)
                    {
                        mMap[i][j].Parent = null;
                        mMap[i][j].Global = float.MaxValue;
                        mMap[i][j].Local = float.MaxValue;
                        mMap[i][j].Visited = false;
                    }
                }

                // Setup the start node to be zero away from start and estimate distance to target
                EnvironmentTile currentNode = begin;
                currentNode.Local = 0.0f;
                currentNode.Global = Heuristic(begin, destination);

                // Maintain a list of nodes to be tested and begin with the start node, keep going
                // as long as we still have nodes to test and we haven't reached the destination
                mToBeTested.Add(currentNode);

                while (mToBeTested.Count > 0 && currentNode != destination)
                {
                    // Begin by sorting the list each time by the heuristic
                    mToBeTested.Sort((a, b) => (int)(a.Global - b.Global));

                    // Remove any tiles that have already been visited
                    mToBeTested.RemoveAll(n => n.Visited);

                    // Check that we still have locations to visit
                    if (mToBeTested.Count > 0)
                    {
                        // Mark this note visited and then process it
                        currentNode = mToBeTested[0];
                        currentNode.Visited = true;

                        // Check each neighbour, if it is accessible and hasn't already been 
                        // processed then add it to the list to be tested 
                        for (int count = 0; count < currentNode.Connections.Count; ++count)
                        {
                            EnvironmentTile neighbour = currentNode.Connections[count];

                            if (!neighbour.Visited && neighbour.IsAccessible)
                            {
                                mToBeTested.Add(neighbour);
                            }

                            // Calculate the local goal of this location from our current location and 
                            // test if it is lower than the local goal it currently holds, if so then
                            // we can update it to be owned by the current node instead 
                            float possibleLocalGoal = currentNode.Local + Distance(currentNode, neighbour);
                            if (possibleLocalGoal < neighbour.Local)
                            {
                                neighbour.Parent = currentNode;
                                neighbour.Local = possibleLocalGoal;
                                neighbour.Global = neighbour.Local + Heuristic(neighbour, destination);
                            }
                        }
                    }
                }

                // Build path if we found one, by checking if the destination was visited, if so then 
                // we have a solution, trace it back through the parents and return the reverse route
                if (destination.Visited)
                {
                   
                    result = new List<EnvironmentTile>();
                    EnvironmentTile routeNode = destination;

                    while (routeNode.Parent != null)
                    {
                        result.Add(routeNode);
                        routeNode = routeNode.Parent;
                    }

                    result.Add(routeNode);
                    result.Reverse();

                    //Debug.LogFormat("Path Found: {0} steps {1} long", result.Count, destination.Local);
                }
                else
                {
                    Debug.LogWarning("Path Not Found");
                }
            }
            else
            {
                result = new List<EnvironmentTile>();
                result.Add(begin);
                result.Add(destination);
                //Debug.LogFormat("Direct Connection: {0} <-> {1} {2} long", begin, destination, TileSize);
            }
        }
        else
        {
            Debug.LogWarning("Cannot find path for invalid nodes");
        }

        mLastSolution = result;

        return result;
    }
}
