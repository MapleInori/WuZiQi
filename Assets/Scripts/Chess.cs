using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// ��������ö�٣���ɫ���ɫ
public enum ChessType { Black,White}

public class Chess : MonoBehaviour
{
    public int row; // ����������
    public int column;  // ����������
    public ChessType chessType = ChessType.Black;   // ��������

    [PunRPC]
    public void SetPositionInfo(int[] rowColumn)
    {
        row = rowColumn[0];
        column = rowColumn[1];
    }
}
