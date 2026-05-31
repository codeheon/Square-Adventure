using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0, 1, -10);
    public float smoothSpeedX = 5f;    // 수평은 빠르게
    public float smoothSpeedY = 3f;    // 수직은 조금 더 부드럽게

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmoothTime = 0.5f;

    [Header("Vertical Dead Zone")]
    public float deadZoneHeight = 1f;  // 이 높이 안에서는 카메라 Y축 고정

    [Header("Clamp")]
    public bool clampCamera = true;
    public Vector2 minClamp;  // 최소 좌표
    public Vector2 maxClamp;  // 최대 좌표

    [Header("Ground Check (점프 시 예측)")]
    public bool useJumpPrediction = true;
    public float jumpPredictionHeight = 2f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 lookAheadVelocity;
    private Vector3 currentLookAhead;
    private float targetLookAhead;
    private Rigidbody2D playerRb;
    private Vector3 playerPreviousPos;

    void Start()
    {
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerPreviousPos = player.position;
            transform.position = player.position + offset;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 플레이어 이동 방향 계산
        float moveDirection = Mathf.Sign(player.position.x - playerPreviousPos.x);
        playerPreviousPos = player.position;

        // Look Ahead 계산 (이동 방향 예측)
        if (Mathf.Abs(player.position.x - playerPreviousPos.x) > 0.01f)
        {
            targetLookAhead = moveDirection * lookAheadDistance;
        }

        currentLookAhead.x = Mathf.SmoothDamp(currentLookAhead.x, targetLookAhead, ref lookAheadVelocity.x, lookAheadSmoothTime);

        // 점프 예측
        if (useJumpPrediction && playerRb != null)
        {
            if (playerRb.linearVelocity.y > 0.1f) // 상승 중
            {
                currentLookAhead.y = jumpPredictionHeight;
            }
            else
            {
                currentLookAhead.y = Mathf.Lerp(currentLookAhead.y, 0, Time.deltaTime * 3f);
            }
        }

        // 목표 위치 계산
        Vector3 targetPosition = player.position + offset + currentLookAhead;

        // Vertical Dead Zone 적용
        float cameraYDelta = targetPosition.y - transform.position.y;
        if (Mathf.Abs(cameraYDelta) < deadZoneHeight * 0.5f)
        {
            targetPosition.y = transform.position.y; // Y축 고정
        }

        // 부드러운 이동
        Vector3 newPosition = transform.position;
        newPosition.x = Mathf.Lerp(transform.position.x, targetPosition.x, smoothSpeedX * Time.deltaTime);

        // 데드존을 벗어난 경우에만 Y축 이동
        if (Mathf.Abs(cameraYDelta) >= deadZoneHeight * 0.5f)
        {
            newPosition.y = Mathf.Lerp(transform.position.y, targetPosition.y, smoothSpeedY * Time.deltaTime);
        }

        transform.position = newPosition;

        // 카메라 영역 제한
        if (clampCamera)
        {
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, minClamp.x, maxClamp.x);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, minClamp.y, maxClamp.y);
            transform.position = clampedPosition;
        }
    }

    // 씬 뷰에서 클램프 영역 시각화
    void OnDrawGizmosSelected()
    {
        if (clampCamera)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3((minClamp.x + maxClamp.x) / 2, (minClamp.y + maxClamp.y) / 2, 0),
                new Vector3(maxClamp.x - minClamp.x, maxClamp.y - minClamp.y, 0)
            );
        }
    }
}