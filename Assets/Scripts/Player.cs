using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public enum PlayerState
{
    NotReady,
    Ready
}

public class Player : MonoBehaviour
{
    public Vector3 zeroPosition;    // 棋盘参考原点，设为左下角
    public float cellWidth;         // 每个格子的宽度
    public ChessType chessType = ChessType.Black;   // 玩家默认棋子类型
    public List<Chess> chessList = new List<Chess>();   // 棋盘上棋子构成的列表

    private PhotonView photonView;  // player自身的PhotonView
    private Vector3 generatePos;    // 棋子生成位置
    private int row;                // 棋子所在行
    private int column;             // 棋子所在列
    private int[] rowColumn = new int[2];   // 棋子所在行列

    private Vector3 mousePos;   // 鼠标点击位置
    private Vector3 offset;     // 点击位置相对于棋盘左下角的距离偏差

    public GameObject blackChess;   // 黑棋游戏对象
    public GameObject whiteChess;   // 白棋游戏对象
    private GameObject newChess;    // 新建的棋子游戏对象

    public PlayerState playerState = PlayerState.NotReady;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        if(photonView.IsMine)
        {
            GameObject.FindObjectOfType<NetWorkManager>().SetSelfText(chessType);
        }
        else
        {
            GameObject.FindObjectOfType<NetWorkManager>().SetHostilefText(chessType);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 如果Player是当前客户端创建的，才能控制，否则不能控制。
        if (!photonView.IsMine) return;
        if (GameObject.FindObjectOfType<NetWorkManager>().playerTurn != chessType) return;
        if (GameObject.FindObjectOfType<NetWorkManager>().gameState != GameState.Ready) return;
        var players = GameObject.FindObjectsOfType<Player>();
        foreach (var player in players)
        {
            if (player.playerState != PlayerState.Ready) return;
        }


        if (Input.GetMouseButtonDown(0))
        {
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // 点击屏幕坐标转化为世界坐标
            offset = mousePos - zeroPosition;  // 点击位置相对于棋盘坐标原点的偏差

            column = (int)Mathf.Round(offset.x / cellWidth);    // 点击位置在棋盘坐标轴x轴上几格，即第几列
            row = (int)Mathf.Round(offset.y / cellWidth);    // 点击位置在棋盘坐标轴y轴上几格，即第几行
            // 将棋子所在行列存储为数组，用于RPC传递参数
            rowColumn[0] = row;
            rowColumn[1] = column;

            // 边界判断
            if (row < 0 || row > 14 || column < 0 || column > 14) return;

            // 获取棋盘上棋子列表
            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            // 该位置已存在棋子则不能落子
            foreach (var chess in chessList)
            {
                if (chess.row == row && chess.column == column) return;
            }
            // （格数*每格宽度）= 世界坐标轴上落子位置 + 棋盘坐标轴所在位置（向量和，即整个世界坐标和落子坐标向左下移动） = 在世界轴上的最终位置
            generatePos = new Vector3(column * cellWidth, row * cellWidth, 0) + zeroPosition;    
            

            Chess currentChess = new Chess();

            if (chessType == ChessType.Black)
            {
                if (blackChess != null)
                {
                    newChess = PhotonNetwork.Instantiate(blackChess.name, generatePos, Quaternion.identity);
                    newChess.GetComponent<PhotonView>().RPC("SetPositionInfo", RpcTarget.All, rowColumn);
                    currentChess = newChess.GetComponent<Chess>();
                }
            }
            else
            {
                if (whiteChess != null)
                {
                    newChess = PhotonNetwork.Instantiate(whiteChess.name, generatePos, Quaternion.identity);
                    newChess.GetComponent<PhotonView>().RPC("SetPositionInfo", RpcTarget.All, rowColumn);
                    currentChess = newChess.GetComponent<Chess>();
                }
            }

            GameObject.FindObjectOfType<NetWorkManager>().GetComponent<PhotonView>().RPC("PlayMarkingAudio", RpcTarget.All);

            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            bool isFive = JudgeFiveChess(chessList, currentChess);
            if(isFive)
            {
                GameObject.FindObjectOfType<NetWorkManager>().GetComponent<PhotonView>().RPC("GameOver",RpcTarget.All, currentChess.chessType);
            }


            GameObject.FindObjectOfType<NetWorkManager>().GetComponent<PhotonView>().RPC("ChangeTurn", RpcTarget.All);


        }
    }

    [PunRPC]
    public void SetChessType(ChessType type)
    {
        chessType = type;
    }
    [PunRPC]
    public void SetPlayerReady()
    {
        playerState = PlayerState.Ready;
        if(photonView.IsMine)
        {
            GameObject.FindObjectOfType<NetWorkManager>().selfReadyText.text = "已准备";
        }
        else
        {
            GameObject.FindObjectOfType<NetWorkManager>().hostileReadyText.text = "已准备";
        }
    }

    // 判断是否五子相连
    bool JudgeFiveChess(List<Chess> chessList, Chess currentChess)
    {
        bool result = false;
        List<Chess> currentChessTypeList = chessList.Where(en => en.chessType == chessType).ToList();

        List<Chess> upList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Up);
        List<Chess> downList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Down);
        List<Chess> leftList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Left);
        List<Chess> rightList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Right);
        List<Chess> leftUpList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.LeftUp);
        List<Chess> rightDownList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.RightDown);
        List<Chess> leftDownList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.LeftDown);
        List<Chess> rightUpList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.RightUp);

        if (upList.Count + downList.Count + 1 >= 5 ||
           leftList.Count + rightList.Count + 1 >= 5 ||
           leftUpList.Count + rightDownList.Count + 1 >= 5 ||
           leftDownList.Count + rightUpList.Count + 1 >= 5)
        {
            result = true;
        }

        return result;
    }

    // 获取此方向上相邻同色棋子，返回同色棋子列表
    List<Chess> GetSameChessByDirection(List<Chess> currentChessTypeList, Chess currentChess, ChessDirection direction)
    {
        List<Chess> result = new List<Chess>();

        switch (direction)
        {
            case ChessDirection.Up:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Up);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.Down:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Down);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.Left:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Left);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.Right:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Right);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.LeftUp:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.LeftUp);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.RightDown:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.RightDown);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.LeftDown:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.LeftDown);
                        result.AddRange(resultList);
                    }
                }
                break;
            case ChessDirection.RightUp:
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        List<Chess> resultList = GetSameChessByDirection(currentChessTypeList, item, ChessDirection.RightUp);
                        result.AddRange(resultList);
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