using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("Data")]
    public BulletData bulletData;
    private WeaponData.WeaponElement _weaponElement;
    public bool isPlayerBullet = true;
    [Tooltip("투사체 머리 방향: 왼0 위90")]
    public float headAngle = 90f;

    [Header("Settings")]
    public float destroyDelay = 0.4f; // 폭발 애니메이션이 끝날 때까지 기다리는 시간

    // 내부 컴포넌트 참조
    protected Animator anim;
    protected Rigidbody2D rb;
    private bool isHit = false; // 중복 충돌 방지용 플래그

    public float damage => bulletData != null ? bulletData.damage : 1f;
    public WeaponData.WeaponElement weaponElement
    {
        get
        {
            if (bulletData != null && bulletData.weaponElement != WeaponData.WeaponElement.Cyan)
            {
                return bulletData.weaponElement;
            }
            return _weaponElement;
        }
        set => _weaponElement = value;
    }

    void Awake()
    {
        // 애니메이터와 리지드바디 가져오기
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (bulletData != null)
        {
            _weaponElement = bulletData.weaponElement;
        }
        Debug.Log("Start: " + transform.rotation.eulerAngles.z + " + " + headAngle);
        transform.rotation = Quaternion.Euler(0f, 0f, transform.rotation.eulerAngles.z + headAngle);
    }

    // (참고) 만약 Update에서 transform.Translate로 이동 중이었다면,
    // if(!isHit) 감싸서 멈추게 해야 합니다.
    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 충돌한 상태라면 무시 (다단 히트 방지)
        if (isHit) return;

        bool targetHit = false;

        // 플레이어 총알 -> 적/벽 충돌
        if (isPlayerBullet && (other.CompareTag("Enemy") || other.CompareTag("Wall")))
        {
            targetHit = true;
        }
        // 적 총알 -> 플레이어/벽 충돌
        else if (!isPlayerBullet && (other.CompareTag("Player") || other.CompareTag("Wall")))
        {
            targetHit = true;
        }

        if (targetHit)
        {
            // 1. 상태 잠금 (추가 충돌 방지)
            isHit = true;

            // 2. 물리 이동 정지 (Rigidbody를 쓰는 경우)
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // 3. 콜라이더 끄기 (시체에 또 부딪히지 않게)
            GetComponent<Collider2D>().enabled = false;

            // 4. 애니메이션 트리거 작동 (아까 만든 BaseController의 OnHit)
            if (anim != null) anim.SetTrigger("OnHit");
                
              
            // 5. 애니메이션 재생 시간만큼 기다렸다가 삭제
            // (폭발 애니메이션 길이에 맞춰 destroyDelay를 조절하세요)
            Destroy(gameObject, destroyDelay);
        }
    }
}