using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public enum GameState
{
    Ready = 1,
    GameOver = 3
}

public class NetWorkManager : MonoBehaviourPunCallbacks
{
    public GameObject player;   // ���
    public ChessType playerTurn = ChessType.Black;  // ��Ϸ�ִΣ�Ĭ�Ϻڷ�����

    public GameState gameState = GameState.Ready;   // ��Ϸ״̬

    public TextMeshProUGUI readyText;   // ׼����ť��Text
    public TextMeshProUGUI selfChessText;   // �Լ�����������Text
    public TextMeshProUGUI selfReadyText;   // �Լ���׼��״̬Text
    public TextMeshProUGUI hostileChessText;    // �Է�����������Text
    public TextMeshProUGUI hostileReadyText;    // �Է���׼��״̬Text
    public TextMeshProUGUI turnText;            // �ֵ�˭��Text
    public TextMeshProUGUI gameOverText;        // ��Ϸ����ʱ��Text
    public TextMeshProUGUI winText;             // ��Ϸ����ʱ��ʾʤ��Text

    public AudioSource markingAudio;    // ������Ч


    // Start is called before the first frame update
    void Start()
    {
        SetUIState();   // ��ʼ��UI
        PhotonNetwork.ConnectUsingSettings(); //���ӵ�Photon�ķ���
    }

    public override void OnConnectedToMaster()
    {
        // ���ӵ���������
        base.OnConnectedToMaster();
        print("OnConnectedToMaster");
        // ���뷿�䣬���������򴴽������Ҽ���
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // ���÷����������
        PhotonNetwork.JoinOrCreateRoom("WuZiQi", roomOptions, TypedLobby.Default);
        // �����ص�OnJoinedRoom
    }

    public override void OnJoinedRoom()
    {
        // ���뷿������
        base.OnJoinedRoom();
        print("OnJoinedRoom");

        // �쳣����û�����Ԥ����ʱ����
        if (player == null) return;
        GameObject newPlayer = PhotonNetwork.Instantiate(player.name, Vector3.zero, Quaternion.identity);


        if (PhotonNetwork.IsMasterClient)
        {
            //newPlayer.GetComponent<Player>().chessType = ChessType.Black;
            newPlayer.GetComponent<PhotonView>().RPC("SetChessType",RpcTarget.All,ChessType.Black);
        }
        else
        {
            newPlayer.GetComponent<PhotonView>().RPC("SetChessType", RpcTarget.All, ChessType.White);
        }

    }

    [PunRPC]
    public void ChangeTurn()
    {
        playerTurn = playerTurn == ChessType.Black ? ChessType.White : ChessType.Black;
        turnText.text = playerTurn == ChessType.Black ? "��ڷ�����" : "��׷�����";
    }
    [PunRPC]
    public void GameOver(ChessType winChessType)
    {
        gameState = GameState.GameOver;
        if(gameOverText)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Game Over";
            winText.text = winChessType == ChessType.Black ? "�ڷ���ʤ" : "�׷���ʤ";
        }
    }
    [PunRPC]
    public void PlayMarkingAudio()
    {
        if (markingAudio == null) return;
        markingAudio.Play();
    }

    public void OnClickReadyButton()
    {
        readyText.text = "��׼��";
        var players = GameObject.FindObjectsOfType<Player>();
        foreach (Player p in players)
        {
            if(p.GetComponent<PhotonView>().IsMine)
            {
                p.GetComponent<PhotonView>().RPC("SetPlayerReady",RpcTarget.All);
            }
        }
    }

    public void SetUIState()
    {
        readyText.text = "׼��";
        selfChessText.text = "";
        selfReadyText.text = "";
        hostileChessText.text = "";
        hostileReadyText.text = "";
        turnText.text = "��ڷ�����";
        gameOverText.gameObject.SetActive(false);
        winText.text = "";
    }

    public void SetSelfText(ChessType chessType)
    {
        selfChessText.text = chessType == ChessType.Black ? "�ڷ�" : "�׷�";
        selfReadyText.text = "δ׼��";
    }

    public void SetHostilefText(ChessType chessType)
    {
        hostileChessText.text = chessType == ChessType.Black ? "�ڷ�" : "�׷�";
        hostileReadyText.text = "δ׼��";
    }
}
