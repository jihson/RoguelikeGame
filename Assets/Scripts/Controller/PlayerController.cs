using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    PlayerStat playerstat;

    public float _jumpPower; //플레이어 점프 파워

    private BoxCollider2D boxCol2D;
    private Rigidbody2D rigid; //플레이어  rigid body
    private Animator animator; //플레이어 애니메이션
    private PossessionController possession; // 플레이어 빙의

    private bool inputRight = false;
    private bool inputLeft = false;
    private bool inputJump = false;
    private bool isjumping; // 스크립트 내부 점프 상태 제어
    public bool ispossession; // 스크립트 내부 빙의 상태 제어
    private string animationState = "AnimationState";

    //플레이어 상태들
    enum States
    {
        Idle = 0,
        Run = 1,
        Attack = 2,
        Skill = 3,
        Die = 4,
    }

    void Init() //플레이어 컴포넌트 연결부분
    {
        _jumpPower = 100;

        boxCol2D = GetComponent<BoxCollider2D>();
        playerstat = GetComponent<PlayerStat>();
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        possession = GetComponent<PossessionController>();
    }

    void Start()
    {
        Init();
        //Input Manager 이용 
        //키 감지
        Managers.Input.KeyAction -= OnKeyBoard; // 이미 작동된 실수 방지
        Managers.Input.KeyAction += OnKeyBoard;
        //키 없는 상태 감지
        Managers.Input.NonKeyAction -= NonKeyBoard;
        Managers.Input.NonKeyAction += NonKeyBoard;
        //마우스 드래그 , 클릭 감지 ( Define 클래스 참고 )
        Managers.Input.MouseAction -= OnMouseClicked;
        Managers.Input.MouseAction += OnMouseClicked;

        Debug.Log("Start Again");
       
    }

    private void Update()
    {
        RaycastHit2D raycasHit = Physics2D.BoxCast(boxCol2D.bounds.center, boxCol2D.bounds.size, 0f, Vector2.down, 0.02f, LayerMask.GetMask("Floor"));
        if (raycasHit.collider != null)
            animator.SetBool("isJumping", false);
        else animator.SetBool("isJumping", true);
    }

    private void FixedUpdate()
    {
        if(inputRight)
        {
            inputRight = false;
            rigid.MovePosition(rigid.position + Vector2.right * playerstat.MoveSpeed * Time.deltaTime);
        }
        if(inputLeft)
        {
            inputLeft = false;
            rigid.MovePosition(rigid.position + Vector2.left * playerstat.MoveSpeed * Time.deltaTime);
        }
        if(inputJump)
        {
            inputJump = false;
            rigid.AddForce(Vector2.up * _jumpPower, ForceMode2D.Impulse);
        }

        if (rigid.velocity.x >= 2.5f) rigid.velocity = new Vector2(2.5f, rigid.velocity.y);
        else if (rigid.velocity.x <= -2.5f) rigid.velocity = new Vector2(-2.5f, rigid.velocity.y);
        //rigid.MovePosition(rigid.position + dir * playerstat.MoveSpeed * Time.deltaTime);
    }

    // collider에 닿았을때
    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if(ispossession)
    //        Debug.Log("OnCollisionEnter2D");

    //    if (collision.gameObject.layer == (int)Define.Layer.Enemy)
    //        Debug.Log($"{collision.gameObject.name}");
    // }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Trigger : {collision.gameObject}");
        if (collision.gameObject.layer == (int)Define.Layer.Enemy)
            collision.GetComponentInParent<Stat>().Hp -= playerstat.Attack;

        if (playerstat.Hp <= 0)
        {
            animator.SetTrigger("isDie");
            Managers.Input.KeyAction -= OnKeyBoard;
            Managers.Input.NonKeyAction -= NonKeyBoard;
            Managers.Input.MouseAction -= OnMouseClicked;
            return;
        }
    }

    //키보드에 뭔가가 들어왔을 때 실행
    void OnKeyBoard()
    {
        ispossession = false;
        if (Input.GetKey(KeyCode.A)) // 왼쪽 이동
        {
            inputLeft = true;
            animator.SetInteger(animationState, (int)States.Run);
            transform.localScale = new Vector3(-1, 1, 1); //왼쪽 바라보는 방향
        }
        if (Input.GetKey(KeyCode.D)) //오른쪽 이동
        {
            inputRight = true;
            animator.SetInteger(animationState, (int)States.Run);
            transform.localScale = new Vector3(1, 1, 1); //오른쪽 바라보는 방향
        }

        if (Input.GetKeyDown(KeyCode.Space) && !animator.GetBool("isJumping")) //점프
        {
            inputJump = true;
            //if (!isjumping) // 점프상태가 아니었을 때
            //{
            //    //스크립트 내부 점프상태 전환
            //    isjumping = true;
            //    // 위로 힘을 줌
            //    rigid.AddForce(Vector2.up * _jumpPower, ForceMode2D.Impulse);
            //}

            animator.SetBool("isJumping", true); // 플레이어 점프 상태로 전환
        }
        //빙의 테스트
        if (Input.GetKey(KeyCode.LeftShift))
        {
            ispossession = true;
        }
    }

    //키보드 키 감지가 없는경우 실행
    void NonKeyBoard()
    {
        //플레이어 기본 상태
        animator.SetInteger(animationState, (int)States.Idle);
    }

    //마우스에 (드래그 , 클릭) 들어왔을 때
    void OnMouseClicked(Define.MouseEvent mouse)
    {
        // 클릭상태이고 현재 플레이어가 Attack 상태가 아닐 때
        if(mouse == Define.MouseEvent.Click)
        {
            //빙의 가능한 상태
            if (ispossession)
            {
                // 반환되는 오브젝트가 적이다?
                if(possession.GetClickedObject().layer == (int)Define.Layer.Enemy)
                {
                    possession.Possession(possession.GetClickedObject());

                    animator.SetTrigger("isDie");
                    Managers.Input.KeyAction -= OnKeyBoard;
                    Managers.Input.NonKeyAction -= NonKeyBoard;
                    Managers.Input.MouseAction -= OnMouseClicked;

                    gameObject.layer = (int)Define.Layer.Enemy;
                    gameObject.tag = "Untagged";

                    Destroy(gameObject, 3f);
                }
            }
            else
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                {
                    animator.SetTrigger("isAttack");
                    Debug.Log("Attack On!!");
                }
                    
            }
        }
            
    }

    
}
