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
    List<Quad> _mainZone = new List<Quad>();
    ConnectPointMap _connectPointMap;
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
        Uncarve();
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
         *  初始化连接点地图
         *  生成连接点
         *  进行连接
         *  进行额外的连接
         */
        SetupConnectPointMap();
        SpownConnectPoints();
        DoConnect();
        ///TODO：进行额外的连接
    }

    void SetupConnectPointMap()
    {
        _connectPointMap = new ConnectPointMap(_spownData.width, _spownData.height);
    }

    void SpownConnectPoints()
    {
        /*
         *  遍历所有区域
         *      生成这个区域的连接点
         */
        foreach (Zone zone in _rooms)
            SpownZoneConnectPoint(zone);
        foreach (Zone zone in _mazes)
            SpownZoneConnectPoint(zone);
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
         *  {
         *      if(越界了)
         *          false
         *  
         *      if(遇到了连接点) 因为连接点连接了两个区域，所以一个连接点肯定会被从两个方向检测两次
         *          false
         *          
         *      if(遇到了空地 && 至少走了一步 && 这个空地不是自己区域的)
         *          true
         *  }
         *  false
         */
        for (int step = 1; step <= MAX_CHECK_SPOWN_CONNECT_POINT_DISTANCE; step++) // 从1开始，因为0就在房间或迷宫里，检测只会浪费运算量
        {
            Vector2 currentPosition = quad.position + direction * step;

            if (!_map.Contains(currentPosition))
                return false;

            Quad currentQuad = _map.GetQuad(currentPosition);

            if (_connectPointMap.IsConnectToUnconnectedZone(currentQuad.position))
                return false;

            if (currentQuad.quadType != QuadType.WALL && step > 1 && !zone.Contains(currentQuad))
                return true;
        }
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
            _connectPointMap.AddConnectPoint(currentPosition);
    }

    void DoConnect()
    {
        /*
         *  随机选一个房间，这个房间加入主区域
         *  
         *  while(还有和主区域相邻的连接点)
         *      随机选一个连接点进行连接（同时要清理连接点）
         */
        AddToMainZone(GetRandomRoom());

        Vector2 connectPoint;
        while ((connectPoint = GetRandomConectPointContiguousMainZone()) != Vector2.zero)
            ConnectZoneAndClearConnectPointByConnectPoint(connectPoint);
    }

    void AddToMainZone(Zone zone)
    {
        _mainZone.AddRange(zone.GetQuads());
    }

    Room GetRandomRoom()
    {
        return _rooms[Random.Range(0, _rooms.Count)];
    }

    /// <summary>
    /// 随机获取一个与主区域相邻的生成点，如果获取不到则返回 Vector2.zero
    /// </summary>
    /// <returns></returns>
    Vector2 GetRandomConectPointContiguousMainZone()
    {
        /*
         *  获取所有与主区域相邻的生成点
         *  
         *  if(有)
         *      随机返回一个
         *  else
         *      zero
         */
        List<Vector2> connectsContiguousMainZone = GetConnectPointsContiguousMainZone();

        if (connectsContiguousMainZone.Count > 0)
            return connectsContiguousMainZone[Random.Range(0, connectsContiguousMainZone.Count)];
        else
            return Vector2.zero;
    }

    List<Vector2> GetConnectPointsContiguousMainZone()
    {
        /*
         *  遍历所有主区域地块
         *      获取地块相邻的连接点并以不重复的方式加入到列表里
         */
        HashSet<Vector2> connectPointsContiguousMainZone = new HashSet<Vector2>();

        foreach (Quad quad in _mainZone)
            connectPointsContiguousMainZone.UnionWith(GetContiguousConnectPoints(quad.position));
        //UnionWith(IEnumerable)：将参数 IEnumerable 里的元素合并进调用的 Set 里，相当于Set版的AddRange

        return new List<Vector2>(connectPointsContiguousMainZone);
    }

    List<Vector2> GetContiguousConnectPoints(Vector2 center)
    {
        /*
         *  遍历相邻地块
         *      if(是连接点)
         *          加进连接点
         */
        List<Vector2> connectPoints = new List<Vector2>();

        foreach (Quad quad in GetContiguousQuads(center))
            if (_connectPointMap.IsConnectToUnconnectedZone(quad.position))
                connectPoints.Add(quad.position);

        return connectPoints;
    }

    List<Quad> GetContiguousQuads(Vector2 center)
    {
        List<Quad> quads = new List<Quad>();

        if (_map.Contains(center + Vector2.up))
            quads.Add(_map.GetQuad(center + Vector2.up));
        if (_map.Contains(center + Vector2.right))
            quads.Add(_map.GetQuad(center + Vector2.right));
        if (_map.Contains(center + Vector2.down))
            quads.Add(_map.GetQuad(center + Vector2.down));
        if (_map.Contains(center + Vector2.left))
            quads.Add(_map.GetQuad(center + Vector2.left));

        return quads;
    }

    void ConnectZoneAndClearConnectPointByConnectPoint(Vector2 connectPoint)
    {
        /*
         *  连接点应该最多相邻两个空地（一层厚的墙，墙是连接点，两边是地块。地图生成原理决定空地夹角不能生成连接点），其中最多一个是主区域地块（两边都是主区域的生成点要清理掉）
         *  
         *  根据连接点获取方向
         *  根据连接点和方向连接到下一个区域
         */
        Vector2 mainZoneQuadPosition = GetContiguousMainZoneQuad(connectPoint).position;
        ConnectZoneAndClearConnectPointByConnectPointAndDirection(connectPoint, (connectPoint - mainZoneQuadPosition));
    }

    void ConnectZoneAndClearConnectPointByConnectPointAndDirection(Vector2 connectPoint, Vector2 direction)
    {
        /*
         *  打穿墙
         *  新的区域的地块加入到主区域
         *  清理新区域的连接点
         */
        Zone newZone = BreakWallAndReturnNewZone(connectPoint, direction);
        AddToMainZone(newZone);
        ClearAZoneConnectPoint(newZone);
    }

    Zone BreakWallAndReturnNewZone(Vector2 connectPoint, Vector2 direction)
    {
        /*
         *  一直向前走
         *      if(是连接点)
         *          打穿并移除连接点
         *      else
         *          返回这个地块所属的区域
         */
        for (Vector2 currentPosition = connectPoint; ; currentPosition += direction)
        {
            if (_connectPointMap.IsConnectToUnconnectedZone(currentPosition))
                BreakWallAndClearConnectPoint(currentPosition);
            else
                return GetZone(_map.GetQuad(currentPosition));
        }
    }

    void BreakWallAndClearConnectPoint(Vector2 position)
    {
        //Debug.Log("打穿墙并移除连接点");
        _map.SetQuadType(position, QuadType.FLOOR);
        _connectPointMap. ChangeConnectPointToConnectToMainZone(position);
    }

    Zone GetZone(Quad quad)
    {
        foreach (Room room in _rooms)
            if (room.Contains(quad))
                return room;

        foreach (Maze maze in _mazes)
            if (maze.Contains(quad))
                return maze;

        throw new System.ArgumentException("传入的地块 " + quad.quadType + "," + quad.position + " 不属于任何房间或迷宫，与设计严重不符");
    }

    void ClearAZoneConnectPoint(Zone zone)
    {
        /*
         *  遍历所有地块
         *      清理一个地块的连接点
         */
        foreach (Quad quad in zone.GetQuads())
            ClearAQuadConnectPoint(quad);
    }

    void ClearAQuadConnectPoint(Quad quad)
    {
        ClearAQuadConnectPointWithDirection(quad, Vector2.up);
        ClearAQuadConnectPointWithDirection(quad, Vector2.right);
        ClearAQuadConnectPointWithDirection(quad, Vector2.down);
        ClearAQuadConnectPointWithDirection(quad, Vector2.left);
    }

    void ClearAQuadConnectPointWithDirection(Quad quad, Vector2 direction)
    {
        /*
         *  if(可以清除)
         *      DO
         */
        if (CanClearConnectPoint(quad, direction))
            DoClearAQuadConnectPointWithDirection(quad, direction);
    }

    bool CanClearConnectPoint(Quad quad, Vector2 direction)
    {
        /*
         *  一直朝前走，直到不是生成点的位置
         *  
         *  if(这个位置是主区域)
         *      true
         *  else
         *      false
         */
        Vector2 currentPosition = quad.position + direction;

        while (_connectPointMap.IsConnectToUnconnectedZone(currentPosition))
            currentPosition += direction;

        return _mainZone.Contains(_map.GetQuad(currentPosition));
    }

    void DoClearAQuadConnectPointWithDirection(Quad quad, Vector2 direction)
    {
        /*
         *  一直朝前走到不是连接点的位置
         *      清除走到位置的连接点
         */
        for (Vector2 currentPosition = quad.position + direction; _connectPointMap.IsConnectToUnconnectedZone(currentPosition); currentPosition += direction)
            _connectPointMap. ChangeConnectPointToConnectToMainZone(currentPosition);
    }

    Quad GetContiguousMainZoneQuad(Vector2 center)
    {
        if (_mainZone.Contains(_map.GetQuad(center + Vector2.up)))
            return _map.GetQuad(center + Vector2.up);
        if (_mainZone.Contains(_map.GetQuad(center + Vector2.right)))
            return _map.GetQuad(center + Vector2.right);
        if (_mainZone.Contains(_map.GetQuad(center + Vector2.down)))
            return _map.GetQuad(center + Vector2.down);
        if (_mainZone.Contains(_map.GetQuad(center + Vector2.left)))
            return _map.GetQuad(center + Vector2.left);

        return null;
    }

    void Uncarve()
    {
        /*
         *  while(还有能反雕刻的地块)
         *      反雕刻一串地块
         */

        Quad canAntiCarveQuad;
        while ((canAntiCarveQuad = GetCanUncarveQuad()) != null)
            UncarveALine(canAntiCarveQuad);
    }

    Quad GetCanUncarveQuad()
    {
        /*
         *  遍历所有地块
         *      if(可以反雕刻)
         *          return
         *  return null
         */
        foreach (Quad quad in _map.GetQuadArray())
            if (CanUncarve(quad))
                return quad;
        return null;
    }

    bool CanUncarve(Quad quad)
    {
        /*
         *  自己不是墙、上下左右至少三面是墙则说明可以反雕刻
         */
        if (quad.quadType == QuadType.WALL)
            return false;

        int wallNumber = 0;
        if (_map.GetQuadType(quad.position + Vector2.up) == QuadType.WALL)
            wallNumber++;
        if (_map.GetQuadType(quad.position + Vector2.right) == QuadType.WALL)
            wallNumber++;
        if (_map.GetQuadType(quad.position + Vector2.down) == QuadType.WALL)
            wallNumber++;
        if (_map.GetQuadType(quad.position + Vector2.left) == QuadType.WALL)
            wallNumber++;

        return wallNumber >= 3;
    }

    void UncarveALine(Quad quad)
    {
        /*
         *  反雕刻这个地块
         *  while(相邻的地块里有可以反雕刻的地块)
         *      转移反雕刻地块到那个地块，再次反雕刻
         */
        Quad currentQuad = quad;
        while (currentQuad != null)
        {
            currentQuad.quadType = QuadType.WALL;
            currentQuad = GetContiguousCanUncarveNode(quad.position);
        }
    }

    Quad GetContiguousCanUncarveNode(Vector2 centerPosition)
    {
        /*
         *  遍历相邻的地块
         *      if(可以雕刻)
         *          return
         *  return null
         */
        foreach (Quad quad in GetContiguousQuads(centerPosition))
            if (CanUncarve(quad))
                return quad;
        return null;
    }
}
