using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class TileController : MonoBehaviour
{
    [SerializeField] List<Sprite> matchColors;

    public int ColorType;

    private float targetDistance;
    private const float MOVESPEED = 3.5f;

    Vector2 targetPosition;
    Rigidbody2D rigidbody2d;


    /// <summary>
    ///  コンポーネントが有効になった時に呼ばれる
    /// </summary>
    private void Awake( )
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    private void Start( )
    {
        //カラーをランダムに設定
        ColorType = Random.Range(0, matchColors.Count);
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = matchColors[ColorType];
    }

    private void Update( )
    {
        float moveDistance = rigidbody2d.velocity.y * Time.deltaTime;

        if(rigidbody2d.gravityScale < 1)
        {
            moveDistance = MOVESPEED * Time.deltaTime;

            transform.position = Vector2.MoveTowards(transform.position , targetPosition , moveDistance);
        }


        targetDistance -= Mathf.Abs(moveDistance);//残り距離を計算

        //目標距離を消化
        if(targetDistance < 0)
        {
            SnapToTargetPosition();
        }
    }

    /// <summary>
    /// タイル移動
    /// </summary>
    public void SwapMove(Vector2 position)
    {
        targetPosition = position;
        targetDistance = Vector2.Distance(transform.position , position);

        enabled = true;//このコンポーネントがupdateに入るようにする、
    }


    /// <summary>
    /// 自由落下
    /// </summary>
    /// <param name="position"></param>
    public void GravityFall(Vector2 position)
    {
        SwapMove(position);
        rigidbody2d.gravityScale = MOVESPEED;
    }


    /// <summary>
    /// 動きを止めて位置を補正する。
    /// </summary>
    private void SnapToTargetPosition()
    {
        //重力をリセット
        rigidbody2d.gravityScale = 0;

        //移動をリセット
        rigidbody2d.velocity = Vector2.zero;
        transform.position = targetPosition;

        enabled = false;//このコンポーネントがupdateに入らないように設定
    }

    /// <summary>
    /// 消える時の演出
    /// </summary>
    public void Delete()
    {
        GetComponent<SpriteRenderer>().sortingOrder = -10;
        GetComponent<BoxCollider2D>().isTrigger = false;

        Vector2 force = new Vector2
            (Random.Range(-500.0f , 500.0f) 
            , Random.Range(-500.0f, 500.0f));
        rigidbody2d.AddForce(force * 0.5f);


        //重力
        rigidbody2d.gravityScale = MOVESPEED;

        //軽めに質量を変更しておく
        rigidbody2d.mass = 0.6f;

        Destroy(gameObject , 2);

    }
}
