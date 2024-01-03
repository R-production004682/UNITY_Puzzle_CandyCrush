using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int  fieldWidth;
    [SerializeField] private int fieldHeigth;

    [SerializeField] private int matchColorCount;
    [SerializeField] private int deleteScore;
    [SerializeField] private float gameTimer;

    [SerializeField] SpriteRenderer  field;
    [SerializeField] TileController  prefabTile;
    [SerializeField] TextMeshProUGUI textGameScore;
    [SerializeField] TextMeshProUGUI textCombo;
    [SerializeField] TextMeshProUGUI textResultScore;
    [SerializeField] GameObject panelResult;
    [SerializeField] AudioClip  seDelete;


    TileController[,] fieldTiles;


    enum GameMode
    {
        WaitFall,
        Delete,
        Fall,
        Spawn,
        Touch,
        WaitSwap,
        WaitBackSwap
    }

    GameMode gameMode;

    Vector2Int swapIndexA;
    Vector2Int swapIndexB;

    Vector2 touchDownPoint;
    bool isTouchDown;


    private void Start( )
    {
        fieldTiles = new TileController[fieldWidth, fieldHeigth];
        field.transform.localScale = new Vector2(fieldWidth , fieldHeigth);

        panelResult.SetActive( false );

        SpawnTile();

        gameMode = GameMode.Touch;
    }

    private void Update( )
    {
        if(gameMode == GameMode.WaitFall)
        {
            WaitFallMode();
        }
        else if(gameMode == GameMode.Delete)
        {
            DeleteMode();
        }
        else if(gameMode == GameMode.Fall)
        {
            FallMode();
        }
        else if(gameMode == GameMode.Spawn)
        {
            SpawnMode();
        }
        else if(gameMode == GameMode.Touch)
        {
            TouchMode();
        }
        else if(gameMode == GameMode.WaitSwap)
        {
            WaitSwapMode();
        }
        else if(gameMode == GameMode.WaitBackSwap)
        {
            WaitBackSwapMode();
        }
    }


    /// <summary>
    /// インデックス座標をWorld座標に変換
    /// </summary>
    /// <returns></returns>
    public Vector2 IndexToWorldPosition(int x , int y)
    {
        Vector2 position = new Vector2();

        position.x = x + 0.5f - fieldWidth  / 2.0f;
        position.y = y + 0.5f - fieldHeigth / 2.0f;

        return position;
    }


    /// <summary>
    /// タイル生成
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private TileController SpawnTile(int x , int y)
    {
        Vector2 position = IndexToWorldPosition(x , y);
        TileController tile = Instantiate(prefabTile , position , Quaternion.identity);
        return tile;
    }


    /// <summary>
    /// 配列外かどうかチェック
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool IsOutOfRange( int x , int y )
    {
        if( x < 0 || fieldWidth  - 1 < x || 
            y < 0 || fieldHeigth - 1 < y)
        {
            return true;
        }
        return false;
    }


    /// <summary>
    /// フィールドにデータを取得（配列外またはデータがない場合Null）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public TileController GetFieldTile(int x , int y)
    {
        if (IsOutOfRange(x, y)) return null;

        return fieldTiles[x, y];
    }


    /// <summary>
    /// フィールドデータのセット（デフォルトはクリア）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void SetFieldTile(int x , int y , TileController tile = null)
    {
        if (IsOutOfRange(x, y)) return;
        fieldTiles[x, y] = tile;
    }


    /// <summary>
    /// 足りないタイルを生成
    /// </summary>
    private void SpawnTile()
    {
        for(int x = 0; x < fieldWidth; x++)
        {
            int emptyCount = 0;

            for (int y = 0; y < fieldHeigth; y++)
            {
                if (GetFieldTile(x, y)) continue;

                TileController tile = SpawnTile(x, fieldHeigth + emptyCount);

                tile.GravityFall(IndexToWorldPosition(x, y));
                SetFieldTile(x, y, tile);

                emptyCount++;
            }
        }
    }


    /// <summary>
    /// 全タイルが落ちるのを待つモード
    /// </summary>
    private void WaitFallMode()
    {

    }

    /// <summary>
    /// 削除モード
    /// </summary>
    private void DeleteMode( )
    {
        //デフォルトでタッチモードへ
        gameMode = GameMode.Touch;
        List<Vector2Int> deleteTiles = GetDeleteTiles();

        if (deleteTiles.Count > 0)
        {
            DeleteTiles(deleteTiles);
            //次のモードに遷移
            gameMode = GameMode.Fall;
        }
    }

    /// <summary>
    /// タイル落下モード
    /// </summary>
    private void FallMode()
    {
        //全タイル落下させる
        FallTiles();
        gameMode = GameMode.Spawn;
    }

    /// <summary>
    /// タイル生成モード
    /// </summary>
    private void SpawnMode()
    {

    }

    /// <summary>
    /// タッチされた時のモード
    /// </summary>
    private void TouchMode()
    {
        if(Input.GetMouseButtonDown(0))
        {
            //スクリーン座標からワールド座標に変換
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint , Vector2.zero);

            if(hit)
            {
                swapIndexA = WorldToIndexPosition(hit.transform.position);
                touchDownPoint = worldPoint;
                isTouchDown = true;
            }
        }
        else if(Input.GetMouseButtonUp(0))
        {
            //タッチフラグのチェック
            if (!isTouchDown) return;
        

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 vec = (worldPoint - touchDownPoint);

            Vector2Int indexDirection = Vector2Int.zero;

            //縦軸
            if (Mathf.Abs(vec.x) < Mathf.Abs(vec.y))
            {
                indexDirection = Vector2Int.up;

                if(vec.y < 0)
                {
                    indexDirection = Vector2Int.down;
                }
            }
            //横軸
            else if(Mathf.Abs(vec.y) < Mathf.Abs(vec.x))
            {
                indexDirection = Vector2Int.right;

                if (vec.x < 0)
                {
                    indexDirection = Vector2Int.left;
                }
            }

            //交換するタイルのインデックス
            swapIndexB = swapIndexA + indexDirection;

            //タイル入れ替え
            SwapTile(swapIndexA, swapIndexB);
            isTouchDown = false;

            gameMode = GameMode.WaitSwap;//次のモードに遷移
        }
    }


    /// <summary>
    /// タイルの移動を待つモード
    /// </summary>
    private void WaitSwapMode()
    {
        //移動が終了していなかった場合
        if (!IsEndMoveTiles()) return;


        List<Vector2Int> deleteTiles = GetDeleteTiles();

        if(deleteTiles.Count > 0)
        {
            gameMode = GameMode.Delete;
        }
        //削除できなければ元に戻す
        else
        {
            SwapTile(swapIndexA, swapIndexB);//削除出来なければ元に戻す。
            gameMode = GameMode.WaitBackSwap;//次のモードに遷移
        }
    }


    /// <summary>
    /// タイルが元の乗タイに戻るのを待つ処理
    /// </summary>
    private void WaitBackSwapMode()
    {
        if (!IsEndMoveTiles()) return;

        gameMode = GameMode.Touch;
    }


    private Vector2Int WorldToIndexPosition(Vector2 position)
    {
        Vector2Int index = new Vector2Int();

        float x = position.x - 0.5f + fieldWidth  / 2.0f;
        float y = position.y - 0.5f + fieldHeigth / 2.0f;

        index.x = (int)x;
        index.y = (int)y;

        return index;
    }

    /// <summary>
    /// 2つのタイルデータを入れ替える（視覚的なポジションの移動はしない）
    /// </summary>
    /// <param name="indexA"></param>
    /// <param name="indexB"></param>
    /// <returns></returns>
    private bool SwapTileDatas(Vector2Int indexA , Vector2Int indexB)
    {
        if (IsOutOfRange(indexA.x, indexA.y) || IsOutOfRange(indexB.x, indexB.y)) return false;
        if (indexA == indexB) return false;

        (fieldTiles[indexA.x, indexA.y], fieldTiles[indexB.x, indexB.y])
            = (fieldTiles[indexB.x, indexB.y], fieldTiles[indexA.x, indexA.y]);

        return true;
    }


    /// <summary>
    /// 2つのタイルデータとポジションを入れ替える
    /// </summary>
    /// <param name="indexA"></param>
    /// <param name="indexB"></param>
    private void SwapTile(Vector2Int indexA , Vector2Int indexB)
    {
        bool isSwapTileDatas = SwapTileDatas(indexA , indexB);

        if (!isSwapTileDatas) return;

        fieldTiles[indexA.x, indexA.y].SwapMove(fieldTiles[indexB.x, indexB.y].transform.position);
        fieldTiles[indexB.x, indexB.y].SwapMove(fieldTiles[indexA.x, indexA.y].transform.position);
    }

    /// <summary>
    /// 全てのタイルの移動が完了したかどうか
    /// </summary>
    /// <returns></returns>
    bool IsEndMoveTiles( )
    {
        //2次元配列の全要素にアクセス
        foreach (var item in fieldTiles)
        {
            //データが無い場合スキップする
            if (!item) continue;

            //コンポーネントが有効な場合
            if (item.enabled) return false;
        }
        return true;
    }



    /// <summary>
    /// マッチするタイルを見つける
    /// </summary>
    /// <param name="index"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private List<Vector2Int> GetMatchTiles(Vector2Int index , List<Vector2Int> direction)
    {
        List<Vector2Int> matchTile = new List<Vector2Int>() { index };

        int mainColor = GetFieldTile(index.x , index.y).ColorType;


        foreach(var dir in direction)
        {
            Vector2Int checkIndex = index + dir;

            //まだ、追加されてなければループする
            while(!matchTile.Contains(checkIndex))
            {
                TileController tile = GetFieldTile(checkIndex.x , checkIndex.y);

                if (!tile) break;
                if (mainColor != tile.ColorType) break;

                matchTile.Add(checkIndex);

                checkIndex += dir;
            }
        }

        return matchTile;
    }



    /// <summary>
    /// アイテムを見つける。
    /// </summary>
    /// <param name="targetList"></param>
    /// <param name="items"></param>
    private void AddNewItem(List<Vector2Int> targetList , List<Vector2Int> items)
    {
        foreach(var item in items)
        {
            if (targetList.Contains(item)) continue;
            targetList.Add(item);
        }
    }



    /// <summary>
    /// 全体から削除可能なタイルを返す
    /// </summary>
    /// <returns></returns>
    private List<Vector2Int> GetDeleteTiles()
    {
        List<Vector2Int> deleteLine = new List<Vector2Int>();

        foreach(var tile in fieldTiles)
        {
            if (!tile) continue;

            Vector2Int index = WorldToIndexPosition(tile.transform.position);

            //左右
            List<Vector2Int> direction = new List<Vector2Int>()
            {
                Vector2Int.left ,
                Vector2Int.right
            };

            List<Vector2Int> matchTiles = GetMatchTiles(index , direction);

            if(matchColorCount <= matchTiles.Count)
            {
                //かぶりのないリストを作成
                AddNewItem(deleteLine, matchTiles);
            }


            //上下
            direction = new List<Vector2Int>()
            {
                Vector2Int.up ,
                Vector2Int.down
            };

            matchTiles = GetMatchTiles(index, direction);

            if (matchColorCount <= matchTiles.Count)
            {
                AddNewItem(deleteLine, matchTiles);
            }
        }

        return deleteLine;
    }
 
    
    /// <summary>
    /// タイルを削除
    /// </summary>
    void DeleteTiles(List<Vector2Int> deleteTiles)
    {
        foreach(var item in deleteTiles)
        {
            TileController tile = GetFieldTile(item.x , item.y);
            if (!tile) continue;

            tile.Delete();

            //内部データを削除
            SetFieldTile(item.x , item.y);
        }

        //TODO :スコアとコンボ計算
    }


    /// <summary>
    /// 指定されたタイルの一番下の空いてるタイルのY座標を返す。
    /// </summary>
    /// <returns></returns>
    private int GetBottomY(int x , int y)
    {
        //返却するY座標
        int bottomY = -1;

        //一番下のyを探す。
        for(int checkY = y - 1; 0 <= checkY ; checkY--)
        {
            if (IsOutOfRange(x, checkY)) continue;
            if (!GetFieldTile(x, checkY)) { bottomY = checkY; }
        }
        return bottomY;
    }

    private void FallTiles()
    {
        foreach(var tile in fieldTiles)
        {
            //タイルデータ無し
            if (!tile) continue;

            Vector2Int indexA = WorldToIndexPosition(tile.transform.position);
            
            int bottomY = GetBottomY(indexA.x , indexA.y);
            if (bottomY == -1) continue;

            Vector2Int indexB = new Vector2Int(indexA.x, bottomY);

            SwapTileDatas(indexA, indexB);
            tile.GravityFall(IndexToWorldPosition(indexB.x , indexB.y));
        }
    }
}
