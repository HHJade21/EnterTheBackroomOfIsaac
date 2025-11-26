using UnityEngine;
using System.Collections;

public class RoomController : MonoBehaviour
{
    public bool isCleared = false;
    public bool isClosed = false;

    public CMYKColor roomColor;
    
    [Header("Door Settings")]
    public GameObject[] doors; // Room의 자식 Door 오브젝트

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!isCleared && !isClosed && other.CompareTag("PlayerFeet"))
        {
            CloseRoom();
            StartCoroutine(_tmpWaitAndClear());
        }
    }

    private IEnumerator _tmpWaitAndClear(){
        yield return new WaitForSeconds(5f);
        isCleared = true;
        OpenRoom();
    }

    private void CloseRoom(){
        isClosed = true;
        isCleared = false;
        foreach(var door in doors)
        {
            if(door != null) door.SetActive(true);
        }
    }

    private void OpenRoom(){
        isClosed = false;
        foreach(var door in doors)
        {
            if(door != null) door.SetActive(false);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // doors가 null이면 빈 배열 할당 (null 체크 방지)
        if(doors == null)
        {
            doors = new GameObject[0];
        }
        OpenRoom();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
