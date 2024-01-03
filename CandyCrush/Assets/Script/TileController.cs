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
    ///  �R���|�[�l���g���L���ɂȂ������ɌĂ΂��
    /// </summary>
    private void Awake( )
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    private void Start( )
    {
        //�J���[�������_���ɐݒ�
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


        targetDistance -= Mathf.Abs(moveDistance);//�c�苗�����v�Z

        //�ڕW����������
        if(targetDistance < 0)
        {
            SnapToTargetPosition();
        }
    }

    /// <summary>
    /// �^�C���ړ�
    /// </summary>
    public void SwapMove(Vector2 position)
    {
        targetPosition = position;
        targetDistance = Vector2.Distance(transform.position , position);

        enabled = true;//���̃R���|�[�l���g��update�ɓ���悤�ɂ���A
    }


    /// <summary>
    /// ���R����
    /// </summary>
    /// <param name="position"></param>
    public void GravityFall(Vector2 position)
    {
        SwapMove(position);
        rigidbody2d.gravityScale = MOVESPEED;
    }


    /// <summary>
    /// �������~�߂Ĉʒu��␳����B
    /// </summary>
    private void SnapToTargetPosition()
    {
        //�d�͂����Z�b�g
        rigidbody2d.gravityScale = 0;

        //�ړ������Z�b�g
        rigidbody2d.velocity = Vector2.zero;
        transform.position = targetPosition;

        enabled = false;//���̃R���|�[�l���g��update�ɓ���Ȃ��悤�ɐݒ�
    }

    /// <summary>
    /// �����鎞�̉��o
    /// </summary>
    public void Delete()
    {
        GetComponent<SpriteRenderer>().sortingOrder = -10;
        GetComponent<BoxCollider2D>().isTrigger = false;

        Vector2 force = new Vector2
            (Random.Range(-500.0f , 500.0f) 
            , Random.Range(-500.0f, 500.0f));
        rigidbody2d.AddForce(force * 0.5f);


        //�d��
        rigidbody2d.gravityScale = MOVESPEED;

        //�y�߂Ɏ��ʂ�ύX���Ă���
        rigidbody2d.mass = 0.6f;

        Destroy(gameObject , 2);

    }
}
