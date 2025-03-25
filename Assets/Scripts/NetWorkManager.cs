using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;


/// <summary>
/// 游戏状态枚举（对应游戏流程阶段）
/// </summary>
public enum GameState
{
    Ready = 1,     // 准备阶段（玩家未准备）
    Start = 2,     // 游戏进行阶段（双方已准备）
    GameOver = 3   // 游戏结束阶段（已分出胜负）
}


/// <summary>
/// 网络管理核心类（继承Photon回调接口）
/// 功能：处理网络连接、玩家管理、游戏状态同步
/// </summary>
public class NetWorkManager : MonoBehaviourPunCallbacks
{
    // 玩家预制体配置
    public GameObject player;   // 玩家角色预制体（需提前拖拽赋值）

    // 游戏回合控制
    public ChessType playerTurn = ChessType.Black;  // 当前回合玩家棋子类型（默认黑方先手）

    // 游戏状态管理
    public GameState gameState = GameState.Ready;   // 当前游戏状态

    // UI组件绑定
    public TextMeshProUGUI readyText;        // 准备按钮文本组件
    public TextMeshProUGUI selfChessText;    // 显示本机玩家棋子类型的文本
    public TextMeshProUGUI selfReadyText;    // 本机玩家准备状态文本
    public TextMeshProUGUI hostileChessText; // 显示对手棋子类型的文本
    public TextMeshProUGUI hostileReadyText; // 对手准备状态文本
    public TextMeshProUGUI turnText;         // 回合提示文本
    public TextMeshProUGUI gameOverText;     // 游戏结束提示文本
    public TextMeshProUGUI winText;          // 胜利者显示文本


    // 音效组件
    public AudioSource markingAudio;    // 落子音效组件

    // 玩家映射表（维护Photon玩家与本地Player实例的关系）
    // Key: Photon Player的ActorNumber, Value: 对应的MyGame.Player实例
    private Dictionary<int, Player> photonPlayerToLocalPlayer = new Dictionary<int, Player>();


    /// <summary>
    /// 注册玩家映射关系，用于当生成玩家对象时记录映射
    /// </summary>
    /// <param name="actorNumber">Photon玩家的唯一标识</param>
    /// <param name="localPlayer">对应的本地Player实例</param>
    public void RegisterPlayer(int actorNumber, Player localPlayer)
    {
        photonPlayerToLocalPlayer[actorNumber] = localPlayer;
    }

    void Start()
    {
        SetUIState();   // 初始化UI状态
        PhotonNetwork.ConnectUsingSettings(); // 连接到Photon云服务
    }

    /// <summary>
    /// 成功连接Photon主服务器回调
    /// 触发时机：完成网络握手并连接到区域服务器
    /// </summary>
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("成功连接至Photon主服务器");

        // 配置房间参数
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // 设置最大玩家数为2（五子棋双人对战）

        // 加入或创建房间（房间名"WuZiQi"，类型为默认大厅）
        PhotonNetwork.JoinOrCreateRoom("WuZiQi", roomOptions, TypedLobby.Default);
    }

    /// <summary>
    /// 成功加入房间回调
    /// 触发时机：本地玩家加入指定房间后
    /// </summary>
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("成功加入房间，当前房间人数：" + PhotonNetwork.CurrentRoom.PlayerCount);

        // 安全检查：确保玩家预制体已配置
        if (player == null)
        {
            Debug.LogError("玩家预制体未配置！");
            return;
        }

        // 实例化网络玩家对象（在所有客户端同步生成）
        GameObject newPlayer = PhotonNetwork.Instantiate(player.name, Vector3.zero, Quaternion.identity);

        // 房主设置棋子颜色并同步
        if (PhotonNetwork.IsMasterClient)
        {
            // 使用RPC同步黑棋设置（All表示所有客户端执行）
            newPlayer.GetComponent<PhotonView>().RPC(
                "SetChessType",
                RpcTarget.All,
                ChessType.Black
            );
        }
        else
        {
            // 非房主设置白棋
            newPlayer.GetComponent<PhotonView>().RPC(
                "SetChessType",
                RpcTarget.All,
                ChessType.White
            );
        }
    }



    /// <summary>
    /// [PunRPC] 切换回合控制权（网络同步方法）
    /// </summary>
    /// <remarks>
    /// 在所有客户端同步更新回合状态和UI提示
    /// </remarks>
    [PunRPC]
    public void ChangeTurn()
    {
        // 切换当前回合玩家颜色
        playerTurn = playerTurn == ChessType.Black ? ChessType.White : ChessType.Black;
        // 更新回合提示文本
        turnText.text = playerTurn == ChessType.Black ? "请黑方落子" : "请白方落子";
    }

    /// <summary>
    /// [PunRPC] 游戏结束处理（网络同步方法）
    /// </summary>
    /// <param name="winChessType">胜利方棋子颜色</param>
    /// <remarks>
    /// 会在所有客户端显示游戏结束界面
    /// </remarks>
    [PunRPC]
    public void GameOver(ChessType winChessType)
    {
        // 更新游戏状态
        gameState = GameState.GameOver;

        // 安全检查UI组件引用
        if (gameOverText)
        {
            // 显示游戏结束界面
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Game Over";
            // 设置胜利者文本
            winText.text = winChessType == ChessType.Black ? "黑方获胜" : "白方获胜";
        }
    }

    /// <summary>
    /// [PunRPC] 重置游戏到初始状态（网络同步方法）
    /// </summary>
    /// <remarks>
    /// 1. 重置UI状态
    /// 2. 重置所有玩家准备状态
    /// 3. 恢复默认回合顺序
    /// </remarks>
    [PunRPC]
    public void ReSetGame()
    {
        // 重置准备相关UI
        readyText.text = "准备";
        selfReadyText.text = "未准备";
        hostileReadyText.text = "未准备";

        // 重置所有玩家状态
        List<Player> players = GameObject.FindObjectsOfType<Player>().ToList();
        foreach (Player p in players)
        {
            p.playerState = PlayerState.NotReady;
        }

        // 恢复初始回合设置
        playerTurn = ChessType.Black;
        turnText.text = "请黑方落子";
    }

    /// <summary>
    /// [PunRPC] 播放落子音效（网络同步方法）
    /// </summary>
    /// <remarks>
    /// 所有客户端同步播放音效
    /// </remarks>
    [PunRPC]
    public void PlayMarkingAudio()
    {
        if (markingAudio == null) return;  // 空值保护
        markingAudio.Play();  // 播放音效文件
    }

    /// <summary>
    /// 准备按钮点击事件处理
    /// </summary>
    /// <remarks>
    /// 执行流程：
    /// 1. 防止重复准备
    /// 2. 更新本地UI
    /// 3. 网络同步准备状态
    /// 4. 重置游戏状态
    /// </remarks>
    public void OnClickReadyButton()
    {
        // 防止重复点击
        if (readyText.text == "已准备") return;

        // 更新准备按钮状态
        readyText.text = "已准备";

        // 遍历所有玩家对象
        var players = GameObject.FindObjectsOfType<Player>();
        foreach (Player p in players)
        {
            // 只同步当前客户端的玩家状态
            if (p.GetComponent<PhotonView>().IsMine)
            {
                p.GetComponent<PhotonView>().RPC("SetPlayerReady", RpcTarget.All);
            }
        }

        // 更新游戏状态
        gameState = GameState.Ready;

        // 隐藏结束界面
        gameOverText.gameObject.SetActive(false);

        // 清理棋盘上所有棋子
        List<Chess> chessList = GameObject.FindObjectsOfType<Chess>().ToList();
        foreach (Chess chess in chessList)
        {
            GameObject.Destroy(chess.gameObject);
        }
    }

    /// <summary>
    /// 初始化所有UI状态
    /// </summary>
    public void SetUIState()
    {
        // 准备相关控件
        readyText.text = "准备";
        selfChessText.text = "";
        selfReadyText.text = "";

        // 对手信息控件
        hostileChessText.text = "";
        hostileReadyText.text = "";

        // 游戏进程控件
        turnText.text = "请黑方落子";
        gameOverText.gameObject.SetActive(false);
        winText.text = "";
    }

    /// <summary>
    /// 设置本机玩家信息显示
    /// </summary>
    /// <param name="chessType">当前玩家的棋子颜色</param>
    public void SetSelfText(ChessType chessType)
    {
        selfChessText.text = chessType == ChessType.Black ? "黑方" : "白方";
        selfReadyText.text = "未准备";
    }

    /// <summary>
    /// 设置对手玩家信息显示
    /// </summary>
    /// <param name="chessType">对手玩家的棋子颜色</param>
    public void SetHostilefText(ChessType chessType)
    {
        hostileChessText.text = chessType == ChessType.Black ? "黑方" : "白方";
        hostileReadyText.text = "未准备";
    }


    /// <summary>
    /// 当有新玩家加入房间时的回调方法（Photon网络事件）
    /// </summary>
    /// <param name="newPhotonPlayer">新加入的Photon玩家对象</param>
    /// <remarks>
    /// 核心功能：房主向新玩家同步现有玩家的状态
    /// 执行逻辑：
    /// 1. 仅由房主执行同步操作
    /// 2. 遍历除新玩家外的所有已有玩家
    /// 3. 通过ActorNumber映射找到对应的本地Player实例
    /// 4. 向新玩家定向发送同步数据
    /// </remarks>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPhotonPlayer)
    {
        base.OnPlayerEnteredRoom(newPhotonPlayer);

        if (PhotonNetwork.IsMasterClient)
        {
            // 遍历房间内所有Photon玩家（包括自己）
            foreach (Photon.Realtime.Player photonPlayer in PhotonNetwork.PlayerList)
            {
                // 跳过新加入的玩家（只需要同步现有玩家状态）
                if (photonPlayer != newPhotonPlayer)
                {
                    // 通过Photon的ActorNumber查找对应的游戏内Player对象
                    if (photonPlayerToLocalPlayer.TryGetValue(photonPlayer.ActorNumber, out Player localPlayer))
                    {
                        // 向新玩家定向发送RPC（仅限新玩家接收）
                        photonView.RPC(
                            "SyncPlayerState",
                            newPhotonPlayer,          // 指定接收者：新玩家
                            photonPlayer.ActorNumber, // 目标玩家的唯一网络标识
                            localPlayer.chessType,    // 玩家棋子颜色状态
                            localPlayer.playerState   // 玩家准备状态
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// [PunRPC] 同步玩家状态数据（网络同步方法）
    /// </summary>
    /// <param name="targetActorNumber">目标玩家的Photon ActorNumber</param>
    /// <param name="chessType">需要同步的棋子颜色</param>
    /// <param name="playerState">需要同步的准备状态</param>
    /// <remarks>
    /// 核心功能：根据网络同步数据更新本地玩家状态和UI
    /// 注意：由于是定向发送，该方法只会在新加入的客户端执行
    /// </remarks>
    [PunRPC]
    private void SyncPlayerState(int targetActorNumber, ChessType chessType, PlayerState playerState)
    {
        // 通过ActorNumber查找对应的本地Player实例
        if (photonPlayerToLocalPlayer.TryGetValue(targetActorNumber, out Player targetPlayer))
        {
            // 更新本地玩家眼中，其他玩家的状态
            targetPlayer.chessType = chessType;
            targetPlayer.playerState = playerState;

            // 更新UI，其实已经排除自己了，targetPlayer一定不会是自己
            if (targetPlayer.photonView.IsMine)
            {
                // 更新本机玩家UI
                selfChessText.text = chessType == ChessType.Black ? "黑方" : "白方";
                selfReadyText.text = playerState == PlayerState.NotReady ? "未准备" : "已准备";
            }
            else
            {
                // 更新对手玩家UI
                hostileChessText.text = chessType == ChessType.Black ? "黑方" : "白方";
                hostileReadyText.text = playerState == PlayerState.NotReady ? "未准备" : "已准备";
            }
        }
    }
    /// <summary>
    /// 当有玩家离开房间时的回调方法（Photon网络事件）
    /// </summary>
    /// <param name="otherPlayer">离开的Photon玩家对象</param>
    /// <remarks>
    /// 维护映射关系：及时清理已离开玩家的数据
    /// 防止后续操作访问到无效玩家引用
    /// </remarks>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // 从映射字典中移除离开的玩家
        photonPlayerToLocalPlayer.Remove(otherPlayer.ActorNumber);
    }

    /// <summary>
    /// 当房主切换时的回调方法（Photon网络事件）
    /// </summary>
    /// <param name="newMaster">新任房主的Photon玩家对象</param>
    /// <remarks>
    /// 特殊处理：新房主需要重新同步所有玩家状态
    /// 当前实现策略：
    /// 1. 新房主遍历本地维护的玩家映射表
    /// 2. 向其他客户端同步每个玩家的最新状态
    /// 优化建议：可在此处实现换先手逻辑（当前未实现）
    /// </remarks>
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 遍历本地维护的所有玩家映射关系
            foreach (var kvp in photonPlayerToLocalPlayer)
            {
                // 向其他客户端同步玩家状态
                photonView.RPC(
                    "SyncPlayerState",
                    RpcTarget.Others,    // 发送给除自己外的其他玩家
                    kvp.Key,             // ActorNumber
                    kvp.Value.chessType, // 棋子颜色
                    kvp.Value.playerState// 准备状态
                );
            }
        }
    }
}
