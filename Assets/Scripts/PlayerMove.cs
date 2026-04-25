using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어의 이동과 충전-발사 시스템을 처리하는 컴포넌트
// - 바닥 감지, 발사 충전, 시각적 피드백(인디케이터, 궤적)을 포함
public class PlayerMove : MonoBehaviour
{
    [Header("Physics Settings")]
    // 물리 관련 설정: Rigidbody2D의 기본 속성들
    [SerializeField] private float mass = 1.0f;                // 질량
    [SerializeField] private float gravityScale = 3.0f;        // 중력 배율
    [SerializeField] private float linearDamping = 0.5f;      // 선형 감쇠
    [SerializeField] private LayerMask groundLayer;           // 바닥 판정을 위한 레이어 마스크
    [SerializeField] private float groundCheckDistance = 0.6f; // 바닥 레이캐스트 거리

    [Header("Charging & Launch")]
    // 충전 및 발사 관련 설정
    [SerializeField] private float minLaunchForce = 5.0f;     // 최소 발사 힘
    [SerializeField] private float maxLaunchForce = 20.0f;    // 최대 발사 힘
    [SerializeField] private float maxChargeTime = 1.5f;      // 최대 충전 시간 (초)
    [SerializeField] private float chargeScaleMultiplier = 2.5f; // 충전 시 인디케이터 크기 배율
    [SerializeField] private float launchCooldown = 0.2f;     // 연속 발사 방지 쿨다운(초)

    [Header("Visual Feedback")]
    // 시각적 피드백 관련 오브젝트
    [SerializeField] private GameObject chargeIndicator;     // 충전 시 표시할 원형 인디케이터
    [SerializeField] private LineRenderer trajectoryLine;    // 발사 방향을 보여주는 궤적 선
    [SerializeField] private Color baseColor = Color.white;  // (미사용) 기본 색상
    [SerializeField] private Color chargingColor = Color.yellow; // (미사용) 충전 중 색상

    // 런타임 상태 변수
    private Rigidbody2D rb;
    private Vector2 lastCheckpoint; // 마지막 체크포인트 위치
    private float currentCharge;    // 현재 충전 시간
    private float lastLaunchTime;   // 마지막 발사 시각
    private bool isCharging;        // 충전 중 여부
    private bool isGrounded;        // 바닥에 닿아 있는지 여부

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 초기 물리 설정 적용
        rb.mass = mass;
        rb.gravityScale = gravityScale;
        rb.linearDamping = linearDamping;

        lastCheckpoint = transform.position;

        // 인디케이터와 궤적 선 초기화
        if (chargeIndicator != null)
        {
            chargeIndicator.SetActive(false);
            chargeIndicator.transform.localScale = Vector3.zero;
        }

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }

    void Update()
    {
        // 프레임마다 바닥 체크 및 입력(충전) 처리
        CheckGrounded();
        HandleCharging();
    }

    // 바닥에 닿아 있는지 레이캐스트로 판별하고, 그에 따라 감쇠를 조절
    private void CheckGrounded()
    {
        // 플레이어의 너비(정사각형)에 따라 자동 계산
        float playerWidth = transform.localScale.x / 2f;

        // 중앙에서 바닥 감지
        RaycastHit2D hitCenter = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        
        // 좌우에서도 바닥 감지 (끝부분 감지용)
        Vector2 leftPos = (Vector2)transform.position + Vector2.left * playerWidth;
        Vector2 rightPos = (Vector2)transform.position + Vector2.right * playerWidth;
        
        RaycastHit2D hitLeft = Physics2D.Raycast(leftPos, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightPos, Vector2.down, groundCheckDistance, groundLayer);
        
        // 세 곳 중 하나라도 ground에 닿으면 grounded 판정
        isGrounded = hitCenter.collider != null || hitLeft.collider != null || hitRight.collider != null;

        // 바닥에 있을 때 물리 감쇄 조절 (정지 상태 보정)
        if (isGrounded && !isCharging)
        {
            rb.linearDamping = linearDamping * 2f;
        }
        else
        {
            rb.linearDamping = linearDamping;
        }
    }

    // 키 입력에 따른 충전(스페이스바) 처리 및 시각적 피드백 업데이트
    private void HandleCharging()
    {
        // 쿨타임 및 바닥 상태 체크
        if (Time.time < lastLaunchTime + launchCooldown || !isGrounded)
        {
            if (isCharging) CancelCharge();
            return;
        }

        // 스페이스바 입력 감지 (Input System 방식)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isCharging = true;
            currentCharge = 0f;
            if (chargeIndicator != null) chargeIndicator.SetActive(true);
            if (trajectoryLine != null) trajectoryLine.enabled = true;
        }

        if (isCharging)
        {
            // 충전 시간 누적 및 비율 계산
            currentCharge += Time.deltaTime;
            float chargePercent = Mathf.Clamp01(currentCharge / maxChargeTime);

            // 마우스 위치 및 방향 계산 (발사 방향)
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
            mousePos.z = 0f;

            Vector2 launchDirection = ((Vector2)mousePos - (Vector2)transform.position).normalized;

            // 시각적 피드백: 원 크기 (충전량에 따라 확대)
            if (chargeIndicator != null)
            {
                float scale = chargePercent * chargeScaleMultiplier;
                chargeIndicator.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // 시각적 피드백: 궤적 가이드 (발사 힘에 따라 길이 조정)
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = true;
                float displayForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargePercent);
                Vector3 endPoint = transform.position + (Vector3)launchDirection * (displayForce * 0.15f);
                trajectoryLine.SetPosition(0, transform.position);
                trajectoryLine.SetPosition(1, endPoint);
            }

            // 스페이스바를 뗐을 때 발사
            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            {
                Launch(launchDirection, chargePercent);
            }
        }
    }

    // 실제 발사 처리: 현재 속도 초기화 후 충전량에 따른 힘을 Impulse로 추가
    private void Launch(Vector2 direction, float chargePercent)
    {
        rb.linearVelocity = Vector2.zero; // 이전 속도 초기화

        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargePercent);
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        lastLaunchTime = Time.time;
        CancelCharge();
    }

    // 충전 취소 시 상태 초기화 및 시각 요소 비활성화
    private void CancelCharge()
    {
        isCharging = false;
        currentCharge = 0f;
        if (chargeIndicator != null) chargeIndicator.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    // 체크포인트 위치 설정
    public void SetCheckpoint(Vector2 newPos)
    {
        lastCheckpoint = newPos;
    }

    // 플레이어 사망 시 체크포인트로 리스폰
    public void Die()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = lastCheckpoint;
        CancelCharge();
    }

    // 에디터에서 바닥 체크용 기즈모 표시
    private void OnDrawGizmos()
    {
        // 플레이어의 너비(정사각형)에 따라 자동 계산
        float playerWidth = transform.localScale.x / 2f;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        
        // 중앙
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        
        // 좌측
        Vector3 leftPos = transform.position + Vector3.left * playerWidth;
        Gizmos.DrawLine(leftPos, leftPos + Vector3.down * groundCheckDistance);
        
        // 우측
        Vector3 rightPos = transform.position + Vector3.right * playerWidth;
        Gizmos.DrawLine(rightPos, rightPos + Vector3.down * groundCheckDistance);
    }
}