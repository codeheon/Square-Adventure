using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float mass = 1.0f;
    [SerializeField] private float gravityScale = 3.0f;
    [SerializeField] private float linearDamping = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.6f;

    [Header("Charging & Launch")]
    [SerializeField] private float minLaunchForce = 5.0f;
    [SerializeField] private float maxLaunchForce = 20.0f;
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float chargeScaleMultiplier = 2.5f;
    [SerializeField] private float launchCooldown = 0.2f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject chargeIndicator;
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color chargingColor = Color.yellow;

    private Rigidbody2D rb;
    private Vector2 lastCheckpoint;
    private float currentCharge;
    private float lastLaunchTime;
    private bool isCharging;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 초기 물리 설정 적용
        rb.mass = mass;
        rb.gravityScale = gravityScale;
        rb.linearDamping = linearDamping;

        lastCheckpoint = transform.position;

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
        CheckGrounded();
        HandleCharging();
    }

    private void CheckGrounded()
    {
        // 바닥 감지 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;

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
            currentCharge += Time.deltaTime;
            float chargePercent = Mathf.Clamp01(currentCharge / maxChargeTime);

            // 마우스 위치 및 방향 계산
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
            mousePos.z = 0f;

            Vector2 launchDirection = ((Vector2)mousePos - (Vector2)transform.position).normalized;

            // 시각적 피드백: 원 크기
            if (chargeIndicator != null)
            {
                float scale = chargePercent * chargeScaleMultiplier;
                chargeIndicator.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // 시각적 피드백: 궤적 가이드
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

    private void Launch(Vector2 direction, float chargePercent)
    {
        rb.linearVelocity = Vector2.zero; // 이전 속도 초기화

        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargePercent);
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        lastLaunchTime = Time.time;
        CancelCharge();
    }

    private void CancelCharge()
    {
        isCharging = false;
        currentCharge = 0f;
        if (chargeIndicator != null) chargeIndicator.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    public void SetCheckpoint(Vector2 newPos)
    {
        lastCheckpoint = newPos;
    }

    public void Die()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = lastCheckpoint;
        CancelCharge();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}