using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonSpowner
{
    /// <summary>
    /// 填充迷宫时相邻节点之间的距离
    /// </summary>
    const int NODE_DISTANCE = 2;
    /// <summary>
    /// 检测能否生成连接点时最长的检测距离，算法设计每个区域最远隔着2厚度的墙，检测到3就可以知道是否能走到空地
    /// </summary>
    const int MAX_CHECK_SPOWN_CONNECT_POINT_DISTANCE = 3;
    SpownData _spownData;
    Map _map;
    List<Room> _rooms = new List<Room>();
    List<Maze> _mazes = new List<Maze>();
    bool[,] _connectPoints;
    List<Vector2> _carvedNodes = new List<Vector2>();
    List<Vector2> _readyToCarveNodes = new List<Vector2>();
    Vector2 _entrancePosition;

    public Map SpownMap(SpownData spownData, Vector2 entrancePosition)
    {
        SetupSpowner(spownData, entrancePosition);
        DoSpown();
        return _map;
    }

    void SetupSpowner(SpownData spownData, Vector2 entrancePosition)
    {
        _spownData = spownData;
        _entrancePosition = entrancePosition;
    }

    void DoSpown()
    {
        /*
         *  初始化地图
         *  生成房间
         *  填充迷宫
         *  连接房间和迷宫
         *  反雕刻迷宫
         */
        SetupMap();
        SpownRooms();
        FillMaze();
        Connect();
        AntiCarve();
    }

    void SetupMap()
    {
        /*
         *  根据生成信息创建地图
         *  填满墙
         */
        _map = new Map(_spownData.width, _spownData.height);
        FillWall();
    }

    void FillWall()
    {
        for (int x = 0; x < _map.width; x++)
            for (int y = 0; y < _map.height; y++)
                _map.SetQuadType(x, y, QuadType.WALL);
    }

    void SpownRooms()
    {
        /*
         *  循环尝试次数
         *  {
         *      随机获取房间大小和位置
         *      if(不和现有房间重叠或相邻)
         *          加进地图里
         *  }
         */
        for (int i = 0; i < _spownData.spownRoomTime; i++)
        {
            Rect roomRect = GetRandomRoomRect();
            if (NotOverlappingOrAdjacentWithExistingRooms(roomRect))
                AddRoom(roomRect);
        }
    }

    Rect GetRandomRoomRect()
    {
        int width = Random.Range(_spownData.minRoomWidth, _spownData.maxRoomWidth);
        int height = Random.Range(_spownData.minRoomHeight, _spownData.maxRoomHeight);
        int x = Random.Range(1, _map.width - 1 - width);
        int y = Random.Range(1, _map.height - 1 - height);
        return new Rect(x, y, width, height);
    }

    bool NotOverlappingOrAdjacentWithExistingRooms(Rect roomRect)
    {
        foreach (Room room in _rooms)
            if (room.AdjoinsOrOverlaps(roomRect))
                return false;
        return true;
    }

    void AddRoom(Rect roomRect)
    {
        Room room = new Room(_map, roomRect);
        _rooms.Add(room);
    }

    void FillMaze()
    {
        /*
         *  循环到没有可以生成迷宫的位置为止
         *  {
         *      生成并添加迷宫
         *  }
         */
        Vector2 mazeStartPosition;
        while ((mazeStartPosition = GetMazeStartPosition()) != Vector2.zero)
            AddMaze(SpownMaze(mazeStartPosition));
    }

    /// <summary>
    /// 获取可以作为迷宫生成点的点，如果获取不到，返回 Vector2.zero
    /// </summary>
    /// <returns></returns>
    Vector2 GetMazeStartPosition()
    {
        /*
         *  遍历地图边缘以内的所有点
         *      if(可以作为起点)
         *          返回位置
         */
        for (int x = 1; x < _map.width - 1; x++)
            for (int y = 1; y < _map.height - 1; y++) // 地图边缘的点肯定不能雕刻，直接不循环边缘的点
                if (CanCarve(x, y))
                    return new Vector2(x, y);
        return Vector2.zero;
    }

    bool CanCarve(Vector2 node)
    {
        return CanCarve((int)node.x, (int)node.y);
    }

    bool CanCarve(int x, int y)
    {
        /*
         *  可以雕刻的标准是：自己和周围的所有地块都是墙
         *  
         *  遍历九宫格
         *      if(越界了 || 不是墙)
         *          false
         *  true
         */
        for (int xOffset = -1; xOffset <= 1; xOffset++)
            for (int yOffset = -1; yOffset <= 1; yOffset++)
                if (!_map.Contains(x + xOffset, y + yOffset) || _map.GetQuadType(x + xOffset, y + yOffset) != QuadType.WALL)
                    return false;
        return true;
    }

    Maze SpownMaze(Vector2 startPosition)
    {
        /*
         *  雕刻起点
         *  while(还有准备雕刻的点)
         *      随机雕刻一个点
         */
        Maze maze = new Maze();

        CarveStartNode(maze, startPosition);
        while (_readyToCarveNodes.Count > 0)
            CarveRandomNode(maze);

        return maze;
    }

    void CarveStartNode(Maze maze, Vector2 startNode)
    {
        Carve(maze, startNode, startNode);
    }

    void Carve(Maze maze, Vector2 carvedNode, Vector2 newNode)
    {
        /*
         *  打穿墙
         *  把新节点相邻的点加入到准备雕刻列表
         *  把新节点从准备雕刻列表移到已雕刻列表里
         */
        BreakWall(maze, carvedNode, newNode);
        AddContiguousDeactiveNodeToReadyList(newNode);
        MoveNodeToCarvedNodes(maze, newNode);
    }

    void BreakWall(Maze maze, Vector2 nodeA, Vector2 nodeB)
    {
        /*
         *  打穿两个点之间的墙
         *  把这个位置的地块加入到迷宫里
         */
        Vector2 wallPosition = (nodeA + nodeB) / 2;
        _map.SetQuadType(wallPosition, QuadType.FLOOR);
        maze.AddQuad(_map.GetQuad(wallPosition));
    }

    void AddContiguousDeactiveNodeToReadyList(Vector2 centerNode)
    {
        /*
         *  遍历相邻的点
         *      if(不在已雕刻表里 && 不在准备雕刻表里 && 是一个可以雕刻的节点)
         *          加入到准备雕刻表
         */
        foreach (Vector2 contiguousNode in GetContiguousNodes(centerNode))
            if (!_carvedNodes.Contains(contiguousNode) && !_readyToCarveNodes.Contains(contiguousNode) && CanCarve(contiguousNode))
                AddNodeToReadyCarveNodes(contiguousNode);
    }

    List<Vector2> GetContiguousNodes(Vector2 centerNode)
    {
        List<Vector2> nodes = new List<Vector2>();

        if (_map.Contains(centerNode + Vector2.up * NODE_DISTANCE))
            nodes.Add(centerNode + Vector2.up * NODE_DISTANCE);
        if (_map.Contains(centerNode + Vector2.right * NODE_DISTANCE))
            nodes.Add(centerNode + Vector2.right * NODE_DISTANCE);
        if (_map.Contains(centerNode + Vector2.down * NODE_DISTANCE))
            nodes.Add(centerNode + Vector2.down * NODE_DISTANCE);
        if (_map.Contains(centerNode + Vector2.left * NODE_DISTANCE))
            nodes.Add(centerNode + Vector2.left * NODE_DISTANCE);

        return nodes;
    }

    void AddNodeToReadyCarveNodes(Vector2 node)
    {
        /*
         *  将这个节点加入到准备雕刻列表
         *  把这个节点的墙打掉
         */
        _readyToCarveNodes.Add(node);
        _map.SetQuadType(node, QuadType.FLOOR);
    }

    void MoveNodeToCarvedNodes(Maze maze, Vector2 node)
    {
        /*
         *  把这个节点从准备雕刻列表里移除
         *  存进已雕刻列表里
         *  把节点加进迷宫里
         */
        _readyToCarveNodes.Remove(node);
        _carvedNodes.Add(node);
        maze.AddQuad(_map.GetQuad(node));
    }

    void CarveRandomNode(Maze maze)
    {
        /*
         *  随机从准备雕刻节点里取一个出来
         *  在这个节点相邻的节点随机选一个已雕刻节点
         *  雕刻这两个节点
         */
        Vector2 readyToCarveNode = GetRandomReadyCarveNode();
        Vector2 carvedNode = GetRandomContiguousCarvedNode(readyToCarveNode);
        Carve(maze, carvedNode, readyToCarveNode);
    }

    Vector2 GetRandomReadyCarveNode()
    {
        return _readyToCarveNodes[Random.Range(0, _readyToCarveNodes.Count)];
    }

    Vector2 GetRandomContiguousCarvedNode(Vector2 centerNode)
    {
        List<Vector2> contiguousCarvedNode = GetContiguousCarvedNodes(centerNode);
        return contiguousCarvedNode[Random.Range(0, contiguousCarvedNode.Count)];
    }

    List<Vector2> GetContiguousCarvedNodes(Vector2 centerNode)
    {
        /*
         *  获取所有已雕刻节点
         *  返回其中所有已雕刻节点
         */
        return new List<Vector2>(GetContiguousNodes(centerNode).Where(node => _carvedNodes.Contains(node))); // 使用Linq的Where方法来筛选在已雕刻表里的节点然后创建一个新的List接收这些节点并返回
        //Where方法：Linq提供的扩展方法，传入判断标准（方法），返回符合标准的 IEnumerable
        //new List(IEnumerable)：创建List并把参数 IEnumerable 里的元素复制到这个List里
    }

    void AddMaze(Maze maze)
    {
        _mazes.Add(maze);
    }

    void Connect()
    {
        /*
         *  生成连接点
         *  连接房间
         */
        SpownConnectPoints();
        DoConnect();
    }

    void SpownConnectPoints()
    {
        /*
         *  初始化连接点数组
         *  
         *  遍历所有区域
         *      生成这个区域的连接点
         */
        SetupConnectPoints();

        foreach (Zone zone in _rooms)
            SpownZoneConnectPoint(zone);
        foreach (Zone zone in _mazes)
            SpownZoneConnectPoint(zone);
    }

    void SetupConnectPoints()
    {
        _connectPoints = new bool[_map.width, _map.height];

        for (int x = 0; x < _map.width; x++)
            for (int y = 0; y < _map.height; y++)
                _connectPoints[x, y] = false;
    }

    void SpownZoneConnectPoint(Zone zone)
    {
        /*
         *  遍历所有地块
         *      生成这个地块的连接点
         */
        foreach (Quad quad in zone.GetQuads())
            SpownAQuadConnectPoint(zone, quad);
    }

    void SpownAQuadConnectPoint(Zone zone, Quad quad)
    {
        /*
         *  遍历上下左右
         *      生成一个方向的连接点
         */
        SpownADirectionConnectPoint(zone, quad, Vector2.up);
        SpownADirectionConnectPoint(zone, quad, Vector2.right);
        SpownADirectionConnectPoint(zone, quad, Vector2.down);
        SpownADirectionConnectPoint(zone, quad, Vector2.left);
    }

    void SpownADirectionConnectPoint(Zone zone, Quad quad, Vector2 direction)
    {
        /*
         *  if(这个方向能生成连接点)
         *      生成这个方向的连接点
         */
        if (CanSpownConnectPoint(zone, quad, direction))
            DoSpownADirectionConnectPoint(quad, direction);
    }

    bool CanSpownConnectPoint(Zone zone, Quad quad, Vector2 direction)
    {
        /*
         *  向前走最长检测距离
         *      if(遇到了空地 && 至少走了一步 && 这个空地不是自己区域的)
         *          true
         *  false
         */
        for (int step = 1; step < MAX_CHECK_SPOWN_CONNECT_POINT_DISTANCE; step++)
            if (_map.GetQuadType(quad.position + direction * step) != QuadType.WALL && step > 1 && !zone.Contains(_map.GetQuad(quad.position + direction * step)))
                return true;
        return false;
    }

    void DoSpownADirectionConnectPoint(Quad quad, Vector2 direction)
    {
        /*
         *  循环到走到空地为止
         *      生成连接点
         */
        Vector2 currentPosition;
        for (int step = 1; _map.GetQuadType((currentPosition = quad.position + direction * step)) == QuadType.WALL; step++)
            AddConnectPoint(currentPosition);
    }

    void AddConnectPoint(Vector2 position)
    {
        _connectPoints[(int)position.x, (int)position.y] = true;
    }

    void DoConnect()
    {
        //TODO：连接
    }

    void AntiCarve()
    {
        //TODO：反雕刻
    }
}
