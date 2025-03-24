using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 棋子类型枚举，黑色或白色
public enum ChessType { Black,White}

public class Chess : MonoBehaviour
{
    public int row; // 棋子所在行
    public int column;  // 棋子所在列
    public ChessType chessType = ChessType.Black;   // 棋子类型

    [PunRPC]
    public void SetPositionInfo(int[] rowColumn)
    {
        row = rowColumn[0];
        column = rowColumn[1];
    }
}
