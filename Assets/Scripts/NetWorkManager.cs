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
    public GameObject player;   // 玩家
    public ChessType playerTurn = ChessType.Black;  // 游戏轮次，默认黑方先手

    public GameState gameState = GameState.Ready;   // 游戏状态

    public TextMeshProUGUI readyText;   // 准备按钮的Text
    public TextMeshProUGUI selfChessText;   // 自己的棋子类型Text
    public TextMeshProUGUI selfReadyText;   // 自己的准备状态Text
    public TextMeshProUGUI hostileChessText;    // 对方的棋子类型Text
    public TextMeshProUGUI hostileReadyText;    // 对方的准备状态Text
    public TextMeshProUGUI turnText;            // 轮到谁了Text
    public TextMeshProUGUI gameOverText;        // 游戏结束时的Text
    public TextMeshProUGUI winText;             // 游戏结束时显示胜者Text

    public AudioSource markingAudio;    // 落子音效


    // Start is called before the first frame update
    void Start()
    {
        SetUIState();   // 初始化UI
        PhotonNetwork.ConnectUsingSettings(); //连接到Photon的服务
    }

    public override void OnConnectedToMaster()
    {
        // 连接到主服务器
        base.OnConnectedToMaster();
        print("OnConnectedToMaster");
        // 加入房间，若不存在则创建房间且加入
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // 设置房间最大人数
        PhotonNetwork.JoinOrCreateRoom("WuZiQi", roomOptions, TypedLobby.Default);
        // 加入后回调OnJoinedRoom
    }

    public override void OnJoinedRoom()
    {
        // 加入房间后调用
        base.OnJoinedRoom();
        print("OnJoinedRoom");

        // 异常处理，没有玩家预制体时返回
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
        turnText.text = playerTurn == ChessType.Black ? "请黑方落子" : "请白方落子";
    }
    [PunRPC]
    public void GameOver(ChessType winChessType)
    {
        gameState = GameState.GameOver;
        if(gameOverText)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Game Over";
            winText.text = winChessType == ChessType.Black ? "黑方获胜" : "白方获胜";
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
        readyText.text = "已准备";
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
        readyText.text = "准备";
        selfChessText.text = "";
        selfReadyText.text = "";
        hostileChessText.text = "";
        hostileReadyText.text = "";
        turnText.text = "请黑方落子";
        gameOverText.gameObject.SetActive(false);
        winText.text = "";
    }

    public void SetSelfText(ChessType chessType)
    {
        selfChessText.text = chessType == ChessType.Black ? "黑方" : "白方";
        selfReadyText.text = "未准备";
    }

    public void SetHostilefText(ChessType chessType)
    {
        hostileChessText.text = chessType == ChessType.Black ? "黑方" : "白方";
        hostileReadyText.text = "未准备";
    }
}
