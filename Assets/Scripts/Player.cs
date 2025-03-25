using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

// ���״̬ö�٣�δ׼��/��׼��
public enum PlayerState
{
    NotReady,
    Ready
}

public class Player : MonoBehaviour
{
    // ������ز���
    public Vector3 zeroPosition;    // �������½�ԭ�����꣨��������ϵ��
    public float cellWidth;         // ÿ������ȣ����絥λ��
    public ChessType chessType = ChessType.Black;   // ���������ɫ����/�ף�
    private List<Chess> chessList = new List<Chess>();   // �ѷ��������б�

    // �������
    [HideInInspector]public PhotonView photonView;  // ��ǰ��Ҷ����PhotonView���
    private NetWorkManager netWorkManager;          // �����е����������

    // ���ӷ�����ر���
    private Vector3 generatePos;    // ��������λ�ã��������꣩
    private int row;                // ��ǰ���λ�õ��кţ�0-14��
    private int column;             // ��ǰ���λ�õ��кţ�0-14��
    private int[] rowColumn = new int[2];   // ���кŵ�������ʽ������RPC������

    // ����������
    private Vector3 mousePos;   // �����λ�ã���Ļ����ϵ��
    private Vector3 offset;     // ���λ�����������ԭ���ƫ����

    // ����Ԥ����
    public GameObject blackChess;   // ����Ԥ����
    public GameObject whiteChess;   // ����Ԥ����
    private GameObject newChess;    // �������ɵ����Ӷ���

    // ���״̬
    public PlayerState playerState = PlayerState.NotReady;  // ���׼��״̬

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        netWorkManager = GameObject.FindObjectOfType<NetWorkManager>();
        // ��ʼ�����UI��ʾ
        if (photonView.IsMine)
        {
            netWorkManager.SetSelfText(chessType);
        }
        else
        {
            netWorkManager.SetHostilefText(chessType);
        }

        // ע����ҵ����������
        Photon.Realtime.Player photonPlayer = photonView.Owner;
        netWorkManager.RegisterPlayer(photonPlayer.ActorNumber, this);
    }

    void Update()
    {
        // ��ҿ���Ȩ��֤ --------------------------
        // ֻ��������Լ�����Ҷ���
        if (!photonView.IsMine) return;
        // ����Ƿ��ֵ���ǰ��һغ�
        if (netWorkManager.playerTurn != chessType) return;
        // ��֤��������Ƿ���׼��
        var players = GameObject.FindObjectsOfType<Player>();
        foreach (var player in players)
        {
            if (player.playerState != PlayerState.Ready) return;
        }
        // ������Ϸ״̬Ϊ��ʼ
        netWorkManager.gameState = GameState.Start;
        // ��Ϸδ��ʼʱ��ֹ����
        if (netWorkManager.gameState != GameState.Start) return;

        // ��������� ------------------------------
        if (Input.GetMouseButtonDown(0))
        {
            // ����ת������ --------------------------
            // ����Ļ����ת��Ϊ��������
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // �������������ԭ���ƫ����
            offset = mousePos - zeroPosition;

            // ���������������к�
            column = (int)Mathf.Round(offset.x / cellWidth);  // X���Ӧ�к�
            row = (int)Mathf.Round(offset.y / cellWidth);     // Y���Ӧ�к�
            rowColumn[0] = row;  // �洢Ϊ����[��,��]
            rowColumn[1] = column;

            // ���ӺϷ�����֤ ------------------------
            // �߽��飨15x15���̣�
            if (row < 0 || row > 14 || column < 0 || column > 14) return;
            // ����Ƿ���������
            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            foreach (var chess in chessList)
            {
                if (chess.row == row && chess.column == column) return;
            }

            // �����������ɵ���������
            generatePos = new Vector3(
                column * cellWidth,  // X��λ�� = �к� * ���
                row * cellWidth,     // Y��λ�� = �к� * ��� 
                0) + zeroPosition;   // ��������ԭ��ƫ��

            // �������Ӷ��� --------------------------
            Chess currentChess = null;
            if (chessType == ChessType.Black && blackChess != null)
            {
                // ���ɺ��岢ͬ��
                newChess = PhotonNetwork.Instantiate(blackChess.name, generatePos, Quaternion.identity);
                newChess.GetComponent<PhotonView>().RPC("SetPositionInfo", RpcTarget.All, rowColumn);
                currentChess = newChess.GetComponent<Chess>();
            }
            else if (whiteChess != null)
            {
                // ���ɰ��岢ͬ��
                newChess = PhotonNetwork.Instantiate(whiteChess.name, generatePos, Quaternion.identity);
                newChess.GetComponent<PhotonView>().RPC("SetPositionInfo", RpcTarget.All, rowColumn);
                currentChess = newChess.GetComponent<Chess>();
            }

            // ��Ϸ�߼����� --------------------------
            // ����������Ч
            netWorkManager.GetComponent<PhotonView>().RPC("PlayMarkingAudio", RpcTarget.All);

            // ʤ���ж�
            chessList = GameObject.FindObjectsOfType<Chess>().ToList();
            bool isFive = JudgeFiveChess(chessList, currentChess);
            if (isFive)
            {
                // ��Ϸ��������
                netWorkManager.GetComponent<PhotonView>().RPC("GameOver", RpcTarget.All, currentChess.chessType);
                netWorkManager.GetComponent<PhotonView>().RPC("ReSetGame", RpcTarget.All);
                return;
            }

            // �л��غ�
            netWorkManager.GetComponent<PhotonView>().RPC("ChangeTurn", RpcTarget.All);
        }
    }

    /// <summary>
    /// ��ȡ��ǰ��ҵĿ����л�״̬���ݣ���������ͬ����
    /// </summary>
    /// <returns>
    /// ���ذ�����ҹؼ�״̬�Ķ������飺
    /// [0] ChessType - ������ɫ����
    /// [1] PlayerState - ���׼��״̬
    /// </returns>
    public object[] GetPlayerState()
    {
        return new object[] {
        chessType,       // ��ҵ�ǰ������ɫ��Black/White��
        playerState      // ���׼��״̬��NotReady/Ready��
    };
    }

    /// <summary>
    /// [PunRPC] ��������������ͣ�����ͬ��������
    /// </summary>
    [PunRPC]
    public void SetChessType(ChessType type)
    {
        chessType = type;  // ���õ�ǰ���������ɫ
    }

    /// <summary>
    /// [PunRPC] �������׼��״̬������ͬ��������
    /// </summary>
    /// <remarks>
    /// ִ�����̣�
    /// 1. �޸ı������׼��״̬
    /// 2. ����Photon�����Զ������ԣ��Զ�ͬ���������ͻ��ˣ�
    /// 3. ���±��غͶ��ֵ�UI��ʾ
    /// </remarks>
    [PunRPC]
    public void SetPlayerReady()
    {
        // �޸ı���״̬Ϊ��׼��
        playerState = PlayerState.Ready;

        // ����Photon�Զ������ԣ��Զ�ͬ�������пͻ��ˣ�
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
    {
        { "IsReady", true }  // ʹ��Hashtable��ֵ�Դ洢׼��״̬
    };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props); // ����OnPlayerPropertiesUpdate�ص�

        // ����UI��ʾ
        if (photonView.IsMine)
        {
            // �����Լ���׼��״̬��ʾ
            netWorkManager.selfReadyText.text = "��׼��";
        }
        else
        {
            // ���¶��ֵ�׼��״̬��ʾ
            netWorkManager.hostileReadyText.text = "��׼��";
        }
    }

    /// <summary>
    /// �ж��Ƿ��γ��������飨����ʤ���ж��㷨��
    /// </summary>
    /// <param name="chessList">���������������б�</param>
    /// <param name="currentChess">��ǰ�����µ�����</param>
    /// <returns>true��ʾ���������ɣ�false��ʾδ���</returns>
    /// <remarks>
    /// ����߼���
    /// 1. ɸѡ����ǰ�����ɫ����������
    /// 2. �ӵ�ǰ���ӳ�������8������ݹ�����������
    /// 3. �ϲ��෴����������������ж��Ƿ�ﵽ5��
    /// </remarks>
    bool JudgeFiveChess(List<Chess> chessList, Chess currentChess)
    {
        bool result = false;
        // ɸѡ��ǰ�����ɫ�����ӣ��Ż��㣺�ɻ�����б�����ظ�ɸѡ��
        List<Chess> currentChessTypeList = chessList.Where(en => en.chessType == chessType).ToList();

        // �˷����⣨ʵ��ֻ���ĸ����߷���ļ�⣩
        List<Chess> upList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Up);     // ���Ϸ�
        List<Chess> downList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Down); // ���·�
        List<Chess> leftList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Left); // ����
        List<Chess> rightList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.Right);// ���ҷ�
        List<Chess> leftUpList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.LeftUp);    // ���Ϸ�
        List<Chess> rightDownList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.RightDown);// ���·�
        List<Chess> leftDownList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.LeftDown);  // ���·�
        List<Chess> rightUpList = GetSameChessByDirection(currentChessTypeList, currentChess, ChessDirection.RightUp);    // ���Ϸ�

        // ������ʤ���ж�����ֱ/ˮƽ/��������б��/��������б�ߣ�
        if (upList.Count + downList.Count + 1 >= 5 ||          // ��ֱ���򣨵�ǰ����+�Ϸ�+�·���
           leftList.Count + rightList.Count + 1 >= 5 ||        // ˮƽ���򣨵�ǰ����+��+�ҷ���
           leftUpList.Count + rightDownList.Count + 1 >= 5 ||  // ��б�ߣ���ǰ����+���Ϸ�+���·���
           leftDownList.Count + rightUpList.Count + 1 >= 5)    // ��б�ߣ���ǰ����+���·�+���Ϸ���
        {
            result = true;
        }

        return result;
    }

    /// <summary>
    /// �ݹ��ȡָ�������ϵ�����ͬɫ���ӣ��������������
    /// </summary>
    /// <param name="currentChessTypeList">��ǰ�����ɫ����������</param>
    /// <param name="currentChess">��ǰ���Ļ�׼����</param>
    /// <param name="direction">��ⷽ�򣨰˷���ö�٣�</param>
    /// <returns>��ָ����������������б���������ǰ���ӣ�</returns>
    /// <remarks>
    /// ʵ��ԭ��
    /// 1. ���ݷ������ȷ���������ӵ�����ƫ����
    /// 2. �ݹ����������ӵ���������
    /// 3. ע�⣺�ݹ�������Ϊ4�㣨���������
    /// </remarks>
    List<Chess> GetSameChessByDirection(List<Chess> currentChessTypeList, Chess currentChess, ChessDirection direction)
    {
        List<Chess> result = new List<Chess>();

        switch (direction)
        {
            case ChessDirection.Up: // ���Ϸ���⣨�к�+1���кŲ��䣩
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column)
                    {
                        result.Add(item);
                        // �ݹ�����Ϸ�������
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Up));
                    }
                }
                break;
            case ChessDirection.Down: // ���·���⣨�к�-1���кŲ��䣩
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Down));
                    }
                }
                break;
            case ChessDirection.Left: // ���󷽼�⣨�к�-1���кŲ��䣩
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Left));
                    }
                }
                break;
            case ChessDirection.Right: // ���ҷ���⣨�к�+1���кŲ��䣩
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.Right));
                    }
                }
                break;
            case ChessDirection.LeftUp: // ���Ϸ���⣨�к�+1���к�-1��
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row + 1 && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.LeftUp));
                    }
                }
                break;
            case ChessDirection.RightDown: // ���·���⣨�к�-1���к�+1��
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column + 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.RightDown));
                    }
                }
                break;
            case ChessDirection.LeftDown: // ���·���⣨�к�-1���к�-1��
                foreach (Chess item in currentChessTypeList)
                {
                    if (item.row == currentChess.row - 1 && item.column == currentChess.column - 1)
                    {
                        result.Add(item);
                        result.AddRange(GetSameChessByDirection(currentChessTypeList, item, ChessDirection.LeftDown));
                    }
                }
                break;
            case ChessDirection.RightUp: // ���Ϸ���⣨�к�+1���к�+1��
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