using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;


/// <summary>
/// ��Ϸ״̬ö�٣���Ӧ��Ϸ���̽׶Σ�
/// </summary>
public enum GameState
{
    Ready = 1,     // ׼���׶Σ����δ׼����
    Start = 2,     // ��Ϸ���н׶Σ�˫����׼����
    GameOver = 3   // ��Ϸ�����׶Σ��ѷֳ�ʤ����
}


/// <summary>
/// �����������ࣨ�̳�Photon�ص��ӿڣ�
/// ���ܣ������������ӡ���ҹ�����Ϸ״̬ͬ��
/// </summary>
public class NetWorkManager : MonoBehaviourPunCallbacks
{
    // ���Ԥ��������
    public GameObject player;   // ��ҽ�ɫԤ���壨����ǰ��ק��ֵ��

    // ��Ϸ�غϿ���
    public ChessType playerTurn = ChessType.Black;  // ��ǰ�غ�����������ͣ�Ĭ�Ϻڷ����֣�

    // ��Ϸ״̬����
    public GameState gameState = GameState.Ready;   // ��ǰ��Ϸ״̬

    // UI�����
    public TextMeshProUGUI readyText;        // ׼����ť�ı����
    public TextMeshProUGUI selfChessText;    // ��ʾ��������������͵��ı�
    public TextMeshProUGUI selfReadyText;    // �������׼��״̬�ı�
    public TextMeshProUGUI hostileChessText; // ��ʾ�����������͵��ı�
    public TextMeshProUGUI hostileReadyText; // ����׼��״̬�ı�
    public TextMeshProUGUI turnText;         // �غ���ʾ�ı�
    public TextMeshProUGUI gameOverText;     // ��Ϸ������ʾ�ı�
    public TextMeshProUGUI winText;          // ʤ������ʾ�ı�


    // ��Ч���
    public AudioSource markingAudio;    // ������Ч���

    // ���ӳ���ά��Photon����뱾��Playerʵ���Ĺ�ϵ��
    // Key: Photon Player��ActorNumber, Value: ��Ӧ��MyGame.Playerʵ��
    private Dictionary<int, Player> photonPlayerToLocalPlayer = new Dictionary<int, Player>();


    /// <summary>
    /// ע�����ӳ���ϵ�����ڵ�������Ҷ���ʱ��¼ӳ��
    /// </summary>
    /// <param name="actorNumber">Photon��ҵ�Ψһ��ʶ</param>
    /// <param name="localPlayer">��Ӧ�ı���Playerʵ��</param>
    public void RegisterPlayer(int actorNumber, Player localPlayer)
    {
        photonPlayerToLocalPlayer[actorNumber] = localPlayer;
    }

    void Start()
    {
        SetUIState();   // ��ʼ��UI״̬
        PhotonNetwork.ConnectUsingSettings(); // ���ӵ�Photon�Ʒ���
    }

    /// <summary>
    /// �ɹ�����Photon���������ص�
    /// ����ʱ��������������ֲ����ӵ����������
    /// </summary>
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("�ɹ�������Photon��������");

        // ���÷������
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // ������������Ϊ2��������˫�˶�ս��

        // ����򴴽����䣨������"WuZiQi"������ΪĬ�ϴ�����
        PhotonNetwork.JoinOrCreateRoom("WuZiQi", roomOptions, TypedLobby.Default);
    }

    /// <summary>
    /// �ɹ����뷿��ص�
    /// ����ʱ����������Ҽ���ָ�������
    /// </summary>
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("�ɹ����뷿�䣬��ǰ����������" + PhotonNetwork.CurrentRoom.PlayerCount);

        // ��ȫ��飺ȷ�����Ԥ����������
        if (player == null)
        {
            Debug.LogError("���Ԥ����δ���ã�");
            return;
        }

        // ʵ����������Ҷ��������пͻ���ͬ�����ɣ�
        GameObject newPlayer = PhotonNetwork.Instantiate(player.name, Vector3.zero, Quaternion.identity);

        // ��������������ɫ��ͬ��
        if (PhotonNetwork.IsMasterClient)
        {
            // ʹ��RPCͬ���������ã�All��ʾ���пͻ���ִ�У�
            newPlayer.GetComponent<PhotonView>().RPC(
                "SetChessType",
                RpcTarget.All,
                ChessType.Black
            );
        }
        else
        {
            // �Ƿ������ð���
            newPlayer.GetComponent<PhotonView>().RPC(
                "SetChessType",
                RpcTarget.All,
                ChessType.White
            );
        }
    }



    /// <summary>
    /// [PunRPC] �л��غϿ���Ȩ������ͬ��������
    /// </summary>
    /// <remarks>
    /// �����пͻ���ͬ�����»غ�״̬��UI��ʾ
    /// </remarks>
    [PunRPC]
    public void ChangeTurn()
    {
        // �л���ǰ�غ������ɫ
        playerTurn = playerTurn == ChessType.Black ? ChessType.White : ChessType.Black;
        // ���»غ���ʾ�ı�
        turnText.text = playerTurn == ChessType.Black ? "��ڷ�����" : "��׷�����";
    }

    /// <summary>
    /// [PunRPC] ��Ϸ������������ͬ��������
    /// </summary>
    /// <param name="winChessType">ʤ����������ɫ</param>
    /// <remarks>
    /// �������пͻ�����ʾ��Ϸ��������
    /// </remarks>
    [PunRPC]
    public void GameOver(ChessType winChessType)
    {
        // ������Ϸ״̬
        gameState = GameState.GameOver;

        // ��ȫ���UI�������
        if (gameOverText)
        {
            // ��ʾ��Ϸ��������
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Game Over";
            // ����ʤ�����ı�
            winText.text = winChessType == ChessType.Black ? "�ڷ���ʤ" : "�׷���ʤ";
        }
    }

    /// <summary>
    /// [PunRPC] ������Ϸ����ʼ״̬������ͬ��������
    /// </summary>
    /// <remarks>
    /// 1. ����UI״̬
    /// 2. �����������׼��״̬
    /// 3. �ָ�Ĭ�ϻغ�˳��
    /// </remarks>
    [PunRPC]
    public void ReSetGame()
    {
        // ����׼�����UI
        readyText.text = "׼��";
        selfReadyText.text = "δ׼��";
        hostileReadyText.text = "δ׼��";

        // �����������״̬
        List<Player> players = GameObject.FindObjectsOfType<Player>().ToList();
        foreach (Player p in players)
        {
            p.playerState = PlayerState.NotReady;
        }

        // �ָ���ʼ�غ�����
        playerTurn = ChessType.Black;
        turnText.text = "��ڷ�����";
    }

    /// <summary>
    /// [PunRPC] ����������Ч������ͬ��������
    /// </summary>
    /// <remarks>
    /// ���пͻ���ͬ��������Ч
    /// </remarks>
    [PunRPC]
    public void PlayMarkingAudio()
    {
        if (markingAudio == null) return;  // ��ֵ����
        markingAudio.Play();  // ������Ч�ļ�
    }

    /// <summary>
    /// ׼����ť����¼�����
    /// </summary>
    /// <remarks>
    /// ִ�����̣�
    /// 1. ��ֹ�ظ�׼��
    /// 2. ���±���UI
    /// 3. ����ͬ��׼��״̬
    /// 4. ������Ϸ״̬
    /// </remarks>
    public void OnClickReadyButton()
    {
        // ��ֹ�ظ����
        if (readyText.text == "��׼��") return;

        // ����׼����ť״̬
        readyText.text = "��׼��";

        // ����������Ҷ���
        var players = GameObject.FindObjectsOfType<Player>();
        foreach (Player p in players)
        {
            // ֻͬ����ǰ�ͻ��˵����״̬
            if (p.GetComponent<PhotonView>().IsMine)
            {
                p.GetComponent<PhotonView>().RPC("SetPlayerReady", RpcTarget.All);
            }
        }

        // ������Ϸ״̬
        gameState = GameState.Ready;

        // ���ؽ�������
        gameOverText.gameObject.SetActive(false);

        // ������������������
        List<Chess> chessList = GameObject.FindObjectsOfType<Chess>().ToList();
        foreach (Chess chess in chessList)
        {
            GameObject.Destroy(chess.gameObject);
        }
    }

    /// <summary>
    /// ��ʼ������UI״̬
    /// </summary>
    public void SetUIState()
    {
        // ׼����ؿؼ�
        readyText.text = "׼��";
        selfChessText.text = "";
        selfReadyText.text = "";

        // ������Ϣ�ؼ�
        hostileChessText.text = "";
        hostileReadyText.text = "";

        // ��Ϸ���̿ؼ�
        turnText.text = "��ڷ�����";
        gameOverText.gameObject.SetActive(false);
        winText.text = "";
    }

    /// <summary>
    /// ���ñ��������Ϣ��ʾ
    /// </summary>
    /// <param name="chessType">��ǰ��ҵ�������ɫ</param>
    public void SetSelfText(ChessType chessType)
    {
        selfChessText.text = chessType == ChessType.Black ? "�ڷ�" : "�׷�";
        selfReadyText.text = "δ׼��";
    }

    /// <summary>
    /// ���ö��������Ϣ��ʾ
    /// </summary>
    /// <param name="chessType">������ҵ�������ɫ</param>
    public void SetHostilefText(ChessType chessType)
    {
        hostileChessText.text = chessType == ChessType.Black ? "�ڷ�" : "�׷�";
        hostileReadyText.text = "δ׼��";
    }


    /// <summary>
    /// ��������Ҽ��뷿��ʱ�Ļص�������Photon�����¼���
    /// </summary>
    /// <param name="newPhotonPlayer">�¼����Photon��Ҷ���</param>
    /// <remarks>
    /// ���Ĺ��ܣ������������ͬ��������ҵ�״̬
    /// ִ���߼���
    /// 1. ���ɷ���ִ��ͬ������
    /// 2. �������������������������
    /// 3. ͨ��ActorNumberӳ���ҵ���Ӧ�ı���Playerʵ��
    /// 4. ������Ҷ�����ͬ������
    /// </remarks>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPhotonPlayer)
    {
        base.OnPlayerEnteredRoom(newPhotonPlayer);

        if (PhotonNetwork.IsMasterClient)
        {
            // ��������������Photon��ң������Լ���
            foreach (Photon.Realtime.Player photonPlayer in PhotonNetwork.PlayerList)
            {
                // �����¼������ң�ֻ��Ҫͬ���������״̬��
                if (photonPlayer != newPhotonPlayer)
                {
                    // ͨ��Photon��ActorNumber���Ҷ�Ӧ����Ϸ��Player����
                    if (photonPlayerToLocalPlayer.TryGetValue(photonPlayer.ActorNumber, out Player localPlayer))
                    {
                        // ������Ҷ�����RPC����������ҽ��գ�
                        photonView.RPC(
                            "SyncPlayerState",
                            newPhotonPlayer,          // ָ�������ߣ������
                            photonPlayer.ActorNumber, // Ŀ����ҵ�Ψһ�����ʶ
                            localPlayer.chessType,    // ���������ɫ״̬
                            localPlayer.playerState   // ���׼��״̬
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// [PunRPC] ͬ�����״̬���ݣ�����ͬ��������
    /// </summary>
    /// <param name="targetActorNumber">Ŀ����ҵ�Photon ActorNumber</param>
    /// <param name="chessType">��Ҫͬ����������ɫ</param>
    /// <param name="playerState">��Ҫͬ����׼��״̬</param>
    /// <remarks>
    /// ���Ĺ��ܣ���������ͬ�����ݸ��±������״̬��UI
    /// ע�⣺�����Ƕ����ͣ��÷���ֻ�����¼���Ŀͻ���ִ��
    /// </remarks>
    [PunRPC]
    private void SyncPlayerState(int targetActorNumber, ChessType chessType, PlayerState playerState)
    {
        // ͨ��ActorNumber���Ҷ�Ӧ�ı���Playerʵ��
        if (photonPlayerToLocalPlayer.TryGetValue(targetActorNumber, out Player targetPlayer))
        {
            // ���±���������У�������ҵ�״̬
            targetPlayer.chessType = chessType;
            targetPlayer.playerState = playerState;

            // ����UI����ʵ�Ѿ��ų��Լ��ˣ�targetPlayerһ���������Լ�
            if (targetPlayer.photonView.IsMine)
            {
                // ���±������UI
                selfChessText.text = chessType == ChessType.Black ? "�ڷ�" : "�׷�";
                selfReadyText.text = playerState == PlayerState.NotReady ? "δ׼��" : "��׼��";
            }
            else
            {
                // ���¶������UI
                hostileChessText.text = chessType == ChessType.Black ? "�ڷ�" : "�׷�";
                hostileReadyText.text = playerState == PlayerState.NotReady ? "δ׼��" : "��׼��";
            }
        }
    }
    /// <summary>
    /// ��������뿪����ʱ�Ļص�������Photon�����¼���
    /// </summary>
    /// <param name="otherPlayer">�뿪��Photon��Ҷ���</param>
    /// <remarks>
    /// ά��ӳ���ϵ����ʱ�������뿪��ҵ�����
    /// ��ֹ�����������ʵ���Ч�������
    /// </remarks>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // ��ӳ���ֵ����Ƴ��뿪�����
        photonPlayerToLocalPlayer.Remove(otherPlayer.ActorNumber);
    }

    /// <summary>
    /// �������л�ʱ�Ļص�������Photon�����¼���
    /// </summary>
    /// <param name="newMaster">���η�����Photon��Ҷ���</param>
    /// <remarks>
    /// ���⴦���·�����Ҫ����ͬ���������״̬
    /// ��ǰʵ�ֲ��ԣ�
    /// 1. �·�����������ά�������ӳ���
    /// 2. �������ͻ���ͬ��ÿ����ҵ�����״̬
    /// �Ż����飺���ڴ˴�ʵ�ֻ������߼�����ǰδʵ�֣�
    /// </remarks>
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // ��������ά�����������ӳ���ϵ
            foreach (var kvp in photonPlayerToLocalPlayer)
            {
                // �������ͻ���ͬ�����״̬
                photonView.RPC(
                    "SyncPlayerState",
                    RpcTarget.Others,    // ���͸����Լ�����������
                    kvp.Key,             // ActorNumber
                    kvp.Value.chessType, // ������ɫ
                    kvp.Value.playerState// ׼��״̬
                );
            }
        }
    }
}
