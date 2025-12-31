using UnityEngine;

public class SwapEffectController : MonoBehaviour
{
    public void DestroyEffect()
    {
        gameObject.SetActive(false);
    }
}
