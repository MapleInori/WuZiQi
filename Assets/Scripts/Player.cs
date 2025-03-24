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
    public Vector3 zeroPosition;    // ���̲ο�ԭ�㣬��Ϊ���½�
    public float cellWidth;         // ÿ�����ӵĿ��
    public ChessType chessType = ChessType.Black;   // ���Ĭ����������
    public List<Chess> chessList = new List<Chess>();   // ���������ӹ��ɵ��б�

    private PhotonView photonView;  // player�����PhotonView
    private Vector3 generatePos;    // ��������λ��
    private int row;                // ����������
    private int column;             // ����������
    private int[] rowColumn = new int[2];   // ������������

    private Vector3 mousePos;   // �����λ��
    private Vector3 offset;     // ���λ��������������½ǵľ���ƫ��

    public GameObject blackChess;   // ������Ϸ����
    public GameObject whiteChess;   // ������Ϸ����
    private GameObject newChess;    // �½���������Ϸ����

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
        // ���Player�ǵ�ǰ�ͻ��˴����ģ����ܿ��ƣ������ܿ��ơ�
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
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // �����Ļ����ת��Ϊ��������
            offset = mousePos - zeroPosition;  // ���λ���������������ԭ���ƫ��

            column = (int)Mathf.Round(offset.x / cellWidth);    // ���λ��������������x���ϼ��񣬼��ڼ���
            row = (int)Mathf.Round(offset.y / cellWidth);    // ���λ��������������y���ϼ��񣬼��ڼ���
            // �������������д洢Ϊ���飬����RPC���ݲ���
            rowColumn[0] = row;
            rowColumn[1] = column;

            // �߽��ж�
            if (row < 0 || row > 14 || column < 0 || column > 14) return;

            // ��ȡ�����������б�
            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            // ��λ���Ѵ���������������
            foreach (var chess in chessList)
            {
                if (chess.row == row && chess.column == column) return;
            }
            // ������*ÿ���ȣ�= ����������������λ�� + ��������������λ�ã������ͣ�������������������������������ƶ��� = ���������ϵ�����λ��
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
            GameObject.FindObjectOfType<NetWorkManager>().selfReadyText.text = "��׼��";
        }
        else
        {
            GameObject.FindObjectOfType<NetWorkManager>().hostileReadyText.text = "��׼��";
        }
    }

    // �ж��Ƿ���������
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

    // ��ȡ�˷���������ͬɫ���ӣ�����ͬɫ�����б�
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

// ����Ѱ�ҷ���
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