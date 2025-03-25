using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

// 玩家状态枚举：未准备/已准备
public enum PlayerState
{
    NotReady,
    Ready
}

public class Player : MonoBehaviour
{
    // 棋盘相关参数
    public Vector3 zeroPosition;    // 棋盘左下角原点坐标（世界坐标系）
    public float cellWidth;         // 每个棋格宽度（世界单位）
    public ChessType chessType = ChessType.Black;   // 玩家棋子颜色（黑/白）
    private List<Chess> chessList = new List<Chess>();   // 已放置棋子列表

    // 网络组件
    [HideInInspector]public PhotonView photonView;  // 当前玩家对象的PhotonView组件
    private NetWorkManager netWorkManager;          // 场景中的网络管理器

    // 棋子放置相关变量
    private Vector3 generatePos;    // 棋子生成位置（世界坐标）
    private int row;                // 当前点击位置的行号（0-14）
    private int column;             // 当前点击位置的列号（0-14）
    private int[] rowColumn = new int[2];   // 行列号的数组形式（用于RPC参数）

    // 输入计算相关
    private Vector3 mousePos;   // 鼠标点击位置（屏幕坐标系）
    private Vector3 offset;     // 点击位置相对于棋盘原点的偏移量

    // 棋子预制体
    public GameObject blackChess;   // 黑棋预制体
    public GameObject whiteChess;   // 白棋预制体
    private GameObject newChess;    // 最新生成的棋子对象

    // 玩家状态
    public PlayerState playerState = PlayerState.NotReady;  // 玩家准备状态

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        netWorkManager = GameObject.FindObjectOfType<NetWorkManager>();
        // 初始化玩家UI显示
        if (photonView.IsMine)
        {
            netWorkManager.SetSelfText(chessType);
        }
        else
        {
            netWorkManager.SetHostilefText(chessType);
        }

        // 注册玩家到网络管理器
        Photon.Realtime.Player photonPlayer = photonView.Owner;
        netWorkManager.RegisterPlayer(photonPlayer.ActorNumber, this);
    }

    void Update()
    {
        // 玩家控制权验证 --------------------------
        // 只允许控制自己的玩家对象
        if (!photonView.IsMine) return;
        // 检查是否轮到当前玩家回合
        if (netWorkManager.playerTurn != chessType) return;
        // 验证所有玩家是否都已准备
        var players = GameObject.FindObjectsOfType<Player>();
        foreach (var player in players)
        {
            if (player.playerState != PlayerState.Ready) return;
        }
        // 更新游戏状态为开始
        netWorkManager.gameState = GameState.Start;
        // 游戏未开始时禁止操作
        if (netWorkManager.gameState != GameState.Start) return;

        // 鼠标点击处理 ------------------------------
        if (Input.GetMouseButtonDown(0))
        {
            // 坐标转换计算 --------------------------
            // 将屏幕坐标转换为世界坐标
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // 计算相对于棋盘原点的偏移量
            offset = mousePos - zeroPosition;

            // 计算点击的棋盘行列号
            column = (int)Mathf.Round(offset.x / cellWidth);  // X轴对应列号
            row = (int)Mathf.Round(offset.y / cellWidth);     // Y轴对应行号
            rowColumn[0] = row;  // 存储为数组[行,列]
            rowColumn[1] = column;

            // 落子合法性验证 ------------------------
            // 边界检查（15x15棋盘）
            if (row < 0 || row > 14 || column < 0 || column > 14) return;
            // 检查是否已有棋子
            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            foreach (var chess in chessList)
            {
                if (chess.row == row && chess.column == column) return;
            }

            // 计算棋子生成的世界坐标
            generatePos = new Vector3(
                column * cellWidth,  // X轴位置 = 列号 * 格宽
                row * cellWidth,     // Y轴位置 = 行号 * 格宽 
                0) + zeroPosition;   // 加上棋盘原点偏移

            // 生成棋子对象 --------------------------
            Chess currentChess = null;
            if (chessType == ChessType.Black && blackChess != null)
            {
                // 生成黑棋并同步
                newChess = PhotonNetwork.Instantiate(blackChess.name, generatePos, Quaternion.identity);
                newChess.GetComponent<PhotonView>().RPC("SetPositionInfo", RpcTarget.All, rowColumn);
                currentChess = newChess.GetComponent<Chess>();
            }
            else if (whiteChess != null)
            {
                // 生成白棋并同步
                newChess = PhotonNetwork.Instantiate(whiteChess.name, generatePos, Quaternion.identity);
                newChess.GetComponent<PhotonView>().RPC("SetPositionInfo", RpcTarget.All, rowColumn);
                currentChess = newChess.GetComponent<Chess>();
            }

            // 游戏逻辑处理 --------------------------
            // 播放落子音效
            netWorkManager.GetComponent<PhotonView>().RPC("PlayMarkingAudio", RpcTarget.All);

            // 胜负判定
            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            bool isFive = JudgeFiveChess(chessList, currentChess);
            if (isFive)
            {
                // 游戏结束处理
                netWorkManager.GetComponent<PhotonView>().RPC("GameOver", RpcTarget.All, currentChess.chessType);
                netWorkManager.GetComponent<PhotonView>().RPC("ReSetGame", RpcTarget.All);
                return;
            }

            // 切换回合
            netWorkManager.GetComponent<PhotonView>().RPC("ChangeTurn", RpcTarget.All);
        }
    }

    /// <summary>
    /// 获取当前玩家的可序列化状态数据（用于网络同步）
    /// </summary>
    /// <returns>
    /// 返回包含玩家关键状态的对象数组：
    /// [0] ChessType - 棋子颜色类型
    /// [1] PlayerState - 玩家准备状态
    /// </returns>
    public object[] GetPlayerState()
    {
        return new object[] {
        chessType,       // 玩家当前棋子颜色（Black/White）
        playerState      // 玩家准备状态（NotReady/Ready）
    };
    }

    /// <summary>
    /// [PunRPC] 设置玩家棋子类型（网络同步方法）
    /// </summary>
    [PunRPC]
    public void SetChessType(ChessType type)
    {
        chessType = type;  // 设置当前玩家棋子颜色
    }

    /// <summary>
    /// [PunRPC] 设置玩家准备状态（网络同步方法）
    /// </summary>
    /// <remarks>
    /// 执行流程：
    /// 1. 修改本地玩家准备状态
    /// 2. 更新Photon网络自定义属性（自动同步到其他客户端）
    /// 3. 更新本地和对手的UI显示
    /// </remarks>
    [PunRPC]
    public void SetPlayerReady()
    {
        // 修改本地状态为已准备
        playerState = PlayerState.Ready;

        // 设置Photon自定义属性（自动同步到所有客户端）
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
    {
        { "IsReady", true }  // 使用Hashtable键值对存储准备状态
    };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props); // 触发OnPlayerPropertiesUpdate回调

        // 更新UI显示
        if (photonView.IsMine)
        {
            // 更新自己的准备状态显示
            netWorkManager.selfReadyText.text = "已准备";
        }
        else
        {
            // 更新对手的准备状态显示
            netWorkManager.hostileReadyText.text = "已准备";
        }
    }

    /// <summary>
    /// 判断是否形成五子连珠（核心胜负判定算法）
    /// </summary>
    /// <param name="chessList">棋盘上所有棋子列表</param>
    /// <param name="currentChess">当前刚落下的棋子</param>
    /// <returns>true表示五子连珠达成，false表示未达成</returns>
    /// <remarks>
    /// 检测逻辑：
    /// 1. 筛选出当前玩家颜色的所有棋子
    /// 2. 从当前棋子出发，向8个方向递归检测连续棋子
    /// 3. 合并相反方向的棋子数量，判断是否达到5连
    /// </remarks>
    bool JudgeFiveChess(List<Chess> chessList, Chess currentChess)
    {
        bool result = false;
        // 筛选当前玩家颜色的棋子（优化点：可缓存此列表避免重复筛选）
        List<Chess> currentChessTypeList = chessList.Where(en => en.chessType == chessType).ToList();

        // 八方向检测（实际只需四个轴线方向的检测）
        List<Chess> upList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Up);     // 正上方
        List<Chess> downList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Down); // 正下方
        List<Chess> leftList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Left); // 正左方
        List<Chess> rightList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Right);// 正右方
        List<Chess> leftUpList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.LeftUp);    // 左上方
        List<Chess> rightDownList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.RightDown);// 右下方
        List<Chess> leftDownList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.LeftDown);  // 左下方
        List<Chess> rightUpList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.RightUp);    // 右上方

        // 四轴线胜负判定（垂直/水平/左上右下斜线/左下右上斜线）
        if (upList.Count + downList.Count + 1 >= 5 ||          // 垂直方向（当前棋子+上方+下方）
           leftList.Count + rightList.Count + 1 >= 5 ||        // 水平方向（当前棋子+左方+右方）
           leftUpList.Count + rightDownList.Count + 1 >= 5 ||  // 主斜线（当前棋子+左上方+右下方）
           leftDownList.Count + rightUpList.Count + 1 >= 5)    // 副斜线（当前棋子+左下方+右上方）
        {
            result = true;
        }

        return result;
    }

    /// <summary>
    /// 递归获取指定方向上的连续同色棋子（深度优先搜索）
    /// </summary>
    /// <param name="currentChessTypeList">当前玩家颜色的所有棋子</param>
    /// <param name="currentChess">当前检测的基准棋子</param>
    /// <param name="direction">检测方向（八方向枚举）</param>
    /// <returns>沿指定方向的连续棋子列表（不包含当前棋子）</returns>
    /// <remarks>
    /// 实现原理：
    /// 1. 根据方向参数确定相邻棋子的行列偏移量
    /// 2. 递归检测相邻棋子的相邻棋子
    /// 3. 注意：递归深度最大为4层（五子棋规则）
    /// </remarks>
    List<Chess> GetSameChessByDirection(List<Chess> currentChessTypeList, Chess currentChess, ChessDirection direction)
    {
        List<Chess> result = new List<Chess>();

        switch (direction)
        {
            case ChessDirection.Up: // 正上方检测（行号+1，列号不变）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column)
                    {
                        result.Add(item);
                        // 递归检测更上方的棋子
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Up));
                    }
                }
                break;
            case ChessDirection.Down: // 正下方检测（行号-1，列号不变）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Down));
                    }
                }
                break;
            case ChessDirection.Left: // 正左方检测（列号-1，行号不变）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Left));
                    }
                }
                break;
            case ChessDirection.Right: // 正右方检测（列号+1，行号不变）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Right));
                    }
                }
                break;
            case ChessDirection.LeftUp: // 左上方检测（行号+1，列号-1）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.LeftUp));
                    }
                }
                break;
            case ChessDirection.RightDown: // 右下方检测（行号-1，列号+1）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.RightDown));
                    }
                }
                break;
            case ChessDirection.LeftDown: // 左下方检测（行号-1，列号-1）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.LeftDown));
                    }
                }
                break;
            case ChessDirection.RightUp: // 右上方检测（行号+1，列号+1）
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.RightUp));
                    }
                }
                break;
        }

        return result;
    }
}

// 棋子寻找方向
public enum ChessDirection
{
    Up,
    Down,
    Left,
    Right,
    LeftUp,
    RightDown,
    LeftDown,
    RightUp
}