using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] TextMeshProUGUI textGameTimer;
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


    int gameScore;
    int comboCouont;

    AudioSource source;


    private void Start( )
    {
        source = GetComponent<AudioSource>();

        fieldTiles = new TileController[fieldWidth, fieldHeigth];
        field.transform.localScale = new Vector2(fieldWidth , fieldHeigth);


        //�X�R�A�ƃR���{�̏�����
        gameScore   = 0;
        comboCouont = 0;

        UpdateTextCombo();

        panelResult.SetActive( false );

        SpawnTile();

        gameMode = GameMode.WaitFall;
    }

    private void Update( )
    {
        //�^�C�����~�b�g
        gameTimer -= Time.deltaTime;

        if(gameTimer < 0)
        {
            GameResult();
            gameTimer = -1;
        }

        //�^�C�}�[�\���̍X�V
        textGameTimer.text = "" + (int)(gameTimer + 1);


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
    /// �C���f�b�N�X���W��World���W�ɕϊ�
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
    /// �^�C������
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
    /// �z��O���ǂ����`�F�b�N
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
    /// �t�B�[���h�Ƀf�[�^���擾�i�z��O�܂��̓f�[�^���Ȃ��ꍇNull�j
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
    /// �t�B�[���h�f�[�^�̃Z�b�g�i�f�t�H���g�̓N���A�j
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void SetFieldTile(int x , int y , TileController tile = null)
    {
        if (IsOutOfRange(x, y)) return;
        fieldTiles[x, y] = tile;
    }


    /// <summary>
    /// ����Ȃ��^�C���𐶐�
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
    /// �S�^�C����������̂�҂��[�h
    /// </summary>
    private void WaitFallMode()
    {
        //�ړ��I����҂�
        if (!IsEndMoveTiles()) return;

        gameMode = GameMode.Delete;
    }

    /// <summary>
    /// �폜���[�h
    /// </summary>
    private void DeleteMode( )
    {
        //�f�t�H���g�Ń^�b�`���[�h��
        gameMode = GameMode.Touch;
        List<Vector2Int> deleteTiles = GetDeleteTiles();

        if (deleteTiles.Count > 0)
        {
            DeleteTiles(deleteTiles);
            //���̃��[�h�ɑJ��
            gameMode = GameMode.Fall;
        }
        //�^�b�`���[�h�J�ڑO
        else
        {
            //�ړ��\�ȃ^�C��
            List<Vector2Int> movableTiles = GetMovableTiles();

            if(movableTiles.Count < 1)
            {
                deleteTiles.Clear();

                foreach(var tile in fieldTiles)
                {
                    Vector2Int index = WorldToIndexPosition(tile.transform.position);
                    deleteTiles.Add(index);
                }

                DeleteTiles(deleteTiles);
                gameMode=GameMode.Fall;
            }
        }
    }

    /// <summary>
    /// �^�C���������[�h
    /// </summary>
    private void FallMode()
    {
        //�S�^�C������������
        FallTiles();
        gameMode = GameMode.Spawn;
    }

    /// <summary>
    /// �^�C���������[�h
    /// </summary>
    private void SpawnMode()
    {
        //�󂢂Ă�t�B�[���h�Ƀ^�C���𐶐�
        SpawnTile();

        gameMode = GameMode.WaitFall;
    }

    /// <summary>
    /// �^�b�`���ꂽ���̃��[�h
    /// </summary>
    private void TouchMode()
    {
        if(Input.GetMouseButtonDown(0))
        {
            //�X�N���[�����W���烏�[���h���W�ɕϊ�
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
            //�^�b�`�t���O�̃`�F�b�N
            if (!isTouchDown) return;
        

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 vec = (worldPoint - touchDownPoint);

            Vector2Int indexDirection = Vector2Int.zero;

            //�c��
            if (Mathf.Abs(vec.x) < Mathf.Abs(vec.y))
            {
                indexDirection = Vector2Int.up;

                if(vec.y < 0)
                {
                    indexDirection = Vector2Int.down;
                }
            }
            //����
            else if(Mathf.Abs(vec.y) < Mathf.Abs(vec.x))
            {
                indexDirection = Vector2Int.right;

                if (vec.x < 0)
                {
                    indexDirection = Vector2Int.left;
                }
            }

            //��������^�C���̃C���f�b�N�X
            swapIndexB = swapIndexA + indexDirection;

            //�^�C������ւ�
            SwapTile(swapIndexA, swapIndexB);
            isTouchDown = false;

            //�R���{�̃��Z�b�g
            comboCouont = 0;
            UpdateTextCombo();


            gameMode = GameMode.WaitSwap;//���̃��[�h�ɑJ��
        }
    }


    /// <summary>
    /// �^�C���̈ړ���҂��[�h
    /// </summary>
    private void WaitSwapMode()
    {
        //�ړ����I�����Ă��Ȃ������ꍇ
        if (!IsEndMoveTiles()) return;


        List<Vector2Int> deleteTiles = GetDeleteTiles();

        if(deleteTiles.Count > 0)
        {
            gameMode = GameMode.Delete;
        }
        //�폜�ł��Ȃ���Ό��ɖ߂�
        else
        {
            SwapTile(swapIndexA, swapIndexB);//�폜�o���Ȃ���Ό��ɖ߂��B
            gameMode = GameMode.WaitBackSwap;//���̃��[�h�ɑJ��
        }
    }


    /// <summary>
    /// �^�C�������̏�^�C�ɖ߂�̂�҂���
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
    /// 2�̃^�C���f�[�^�����ւ���i���o�I�ȃ|�W�V�����̈ړ��͂��Ȃ��j
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
    /// 2�̃^�C���f�[�^�ƃ|�W�V���������ւ���
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
    /// �S�Ẵ^�C���̈ړ��������������ǂ���
    /// </summary>
    /// <returns></returns>
    bool IsEndMoveTiles( )
    {
        //2�����z��̑S�v�f�ɃA�N�Z�X
        foreach (var item in fieldTiles)
        {
            //�f�[�^�������ꍇ�X�L�b�v����
            if (!item) continue;

            //�R���|�[�l���g���L���ȏꍇ
            if (item.enabled) return false;
        }
        return true;
    }



    /// <summary>
    /// �}�b�`����^�C����������
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

            //�܂��A�ǉ�����ĂȂ���΃��[�v����
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
    /// �A�C�e����������B
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
    /// �S�̂���폜�\�ȃ^�C����Ԃ�
    /// </summary>
    /// <returns></returns>
    private List<Vector2Int> GetDeleteTiles()
    {
        List<Vector2Int> deleteLine = new List<Vector2Int>();

        foreach(var tile in fieldTiles)
        {
            if (!tile) continue;

            Vector2Int index = WorldToIndexPosition(tile.transform.position);

            //���E
            List<Vector2Int> direction = new List<Vector2Int>()
            {
                Vector2Int.left ,
                Vector2Int.right
            };

            List<Vector2Int> matchTiles = GetMatchTiles(index , direction);

            if(matchColorCount <= matchTiles.Count)
            {
                //���Ԃ�̂Ȃ����X�g���쐬
                AddNewItem(deleteLine, matchTiles);
            }


            //�㉺
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
    /// �^�C�����폜
    /// </summary>
    void DeleteTiles(List<Vector2Int> deleteTiles)
    {
        foreach(var item in deleteTiles)
        {
            TileController tile = GetFieldTile(item.x , item.y);
            if (!tile) continue;

            tile.Delete();

            //�����f�[�^���폜
            SetFieldTile(item.x , item.y);
        }

        //�R���{���X�V
        comboCouont++;
        UpdateTextCombo();

        int baseScore  = deleteTiles.Count * deleteScore;
        int comboScore = comboCouont * deleteScore;

        gameScore += baseScore + comboScore;
        textGameScore.text = "" + gameScore;

        source.PlayOneShot(seDelete);

    }


    /// <summary>
    /// �w�肳�ꂽ�^�C���̈�ԉ��̋󂢂Ă�^�C����Y���W��Ԃ��B
    /// </summary>
    /// <returns></returns>
    private int GetBottomY(int x , int y)
    {
        //�ԋp����Y���W
        int bottomY = -1;

        //��ԉ���y��T���B
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
            //�^�C���f�[�^����
            if (!tile) continue;

            Vector2Int indexA = WorldToIndexPosition(tile.transform.position);
            
            int bottomY = GetBottomY(indexA.x , indexA.y);
            if (bottomY == -1) continue;

            Vector2Int indexB = new Vector2Int(indexA.x, bottomY);

            SwapTileDatas(indexA, indexB);
            tile.GravityFall(IndexToWorldPosition(indexB.x , indexB.y));
        }
    }


    /// <summary>
    /// �R���{�\��
    /// </summary>
    private void UpdateTextCombo()
    {
        string text = "" + comboCouont + " Combo!!";

        if(comboCouont < 2)
        {
            text = "";
        }
        
        textCombo.text = text;
    }


    private List<Vector2Int> GetMovableTiles()
    {
        List<Vector2Int> movableTiles = new List<Vector2Int>();

        //�S�^�C���𑖍�����
        foreach(var tile in fieldTiles)
        {
            Vector2Int indexA = WorldToIndexPosition(tile.transform.position);

            //�㉺���E�ɓ�����
            List<Vector2Int> directions = new List<Vector2Int>()
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down,
            };

            //�S���ʃ`�F�b�N
            foreach(var dir in directions)
            {
                Vector2Int indexB = indexA + dir;
                SwapTileDatas(indexA , indexB);

                //��ł�������
                if(GetDeleteTiles().Count > 0)
                {
                    if(!movableTiles.Contains(indexA))
                    {
                        movableTiles.Add(indexA);
                    }
                }

                //���ɖ߂�
                SwapTileDatas(indexA , indexB);
            }
        }
        return movableTiles;
    }


    /// <summary>
    /// �Q�[���I��
    /// </summary>
    private void GameResult()
    {
        textResultScore.text = "" + gameScore;
        panelResult.SetActive(true);

        enabled = false;
    }

    /// <summary>
    /// ���g���C�{�^���������ꂽ���̏���
    /// </summary>
    public void OnClickRetryButton()
    {
        SceneManager.LoadScene("CandyCrushGame");
    }
}
