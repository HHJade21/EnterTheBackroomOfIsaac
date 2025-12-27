using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] private float alpha = 0.1f;

    public CMYKColor color = CMYKColor.Key;
    public Color nowColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nowColor = new Color(1f, 1f, 1f, alpha);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.GetComponent<SpriteRenderer>().color = nowColor;
    }

    public void ChangeColor(CMYKColor color){
        Color origin, target;
        switch(color){
        case CMYKColor.Cyan:
            target = new Color(0f, 1f, 1f, alpha);
            break;
        case CMYKColor.Magenta:
            target = new Color(1f, 0f, 1f, alpha);
            break;
        case CMYKColor.Yellow:
            target = new Color(1f, 1f, 0f, alpha);
            break;
        default:
            target = new Color(1f, 1f, 1f, alpha);
            break;
        }

        switch(this.color){
        case CMYKColor.Cyan:
            origin = new Color(0f, 1f, 1f, alpha);
            break;
        case CMYKColor.Magenta:
            origin = new Color(1f, 0f, 1f, alpha);
            break;
        case CMYKColor.Yellow:
            origin = new Color(1f, 1f, 0f, alpha);
            break;
        default:
            origin = new Color(1f, 1f, 1f, alpha);
            break;
        }
        this.color = color;
        StartCoroutine(ChangeColorRoutine(origin, target));
    }

    System.Collections.IEnumerator ChangeColorRoutine(Color origin, Color target){
        Color change = (target - origin) / 100;
        for(int i = 0; i < 100; i++){
            nowColor += change;
            yield return new WaitForSeconds(0.01f);
        }
        //transform.GetComponent<SpriteRenderer>().color = target;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("PlayerArea"))
        {
            Vector3 newPos = transform.position;

            // 기준 크기 (그리드 한 칸 사이즈)
            const float width = 19.6f;
            const float height = 11.76f;

            float dx = other.transform.position.x - transform.position.x;
            float dy = other.transform.position.y - transform.position.y;

            // 가로 방향: 완전히 벗어났을 때만 이동 (중심 간 거리가 width 이상일 때)
            if(dx > width){
                newPos.x += width * 2f;
            }
            else if(dx < -width){
                newPos.x -= width * 2f;
            }

            // 세로 방향: 완전히 벗어났을 때만 이동 (중심 간 거리가 height 이상일 때)
            if(dy > height){
                newPos.y += height * 2f;
            }
            else if(dy < -height){
                newPos.y -= height * 2f;
            }

            transform.position = newPos;
        }
    }
}
