using UnityEngine;

/// <summary>
/// PixelColorReader 사용 예제
/// </summary>
public class PixelColorReaderExample : MonoBehaviour
{
    [Header("테스트용 스프라이트")]
    public Sprite testSprite;
    
    [Header("테스트용 텍스처")]
    public Texture2D testTexture;

    [Header("찾을 색상")]
    public Color targetColor = Color.red;

    [Header("색상 허용 오차")]
    [Range(0f, 1f)]
    public float colorTolerance = 0.1f;

    void Start()
    {
        // 예제 1: Sprite에서 특정 픽셀 컬러 읽기
        if (testSprite != null)
        {
            ExampleReadSpritePixel();
        }

        // 예제 2: Texture2D에서 특정 픽셀 컬러 읽기
        if (testTexture != null)
        {
            ExampleReadTexturePixel();
        }

        // 예제 3: 특정 색상의 모든 픽셀 찾기
        if (testTexture != null)
        {
            ExampleFindPixelsWithColor();
        }
    }

    void ExampleReadSpritePixel()
    {
        Debug.Log("=== Sprite 픽셀 읽기 예제 ===");
        
        // 스프라이트의 중앙 픽셀 읽기
        int centerX = Mathf.RoundToInt(testSprite.rect.width / 2);
        int centerY = Mathf.RoundToInt(testSprite.rect.height / 2);
        
        Color centerColor = PixelColorReader.GetPixelColor(testSprite, centerX, centerY);
        Debug.Log($"스프라이트 중앙 픽셀 ({centerX}, {centerY}) 컬러: {centerColor}");
        
        // 스프라이트의 좌상단 픽셀 읽기
        Color topLeftColor = PixelColorReader.GetPixelColor(testSprite, 0, 0);
        Debug.Log($"스프라이트 좌상단 픽셀 (0, 0) 컬러: {topLeftColor}");
    }

    void ExampleReadTexturePixel()
    {
        Debug.Log("=== Texture2D 픽셀 읽기 예제 ===");
        
        // 텍스처의 중앙 픽셀 읽기
        int centerX = testTexture.width / 2;
        int centerY = testTexture.height / 2;
        
        Color centerColor = PixelColorReader.GetPixelColor(testTexture, centerX, centerY);
        Debug.Log($"텍스처 중앙 픽셀 ({centerX}, {centerY}) 컬러: {centerColor}");
    }

    void ExampleFindPixelsWithColor()
    {
        Debug.Log("=== 특정 색상 픽셀 찾기 예제 ===");
        
        var matchingPixels = PixelColorReader.FindPixelsWithColor(testTexture, targetColor, colorTolerance);
        Debug.Log($"'{targetColor}' 색상과 유사한 픽셀 개수: {matchingPixels.Count}");
        
        // 처음 10개만 출력
        int count = Mathf.Min(10, matchingPixels.Count);
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"  픽셀 {i + 1}: ({matchingPixels[i].x}, {matchingPixels[i].y})");
        }
    }

    // 월드 좌표를 사용한 예제 (런타임에서 사용)
    void Update()
    {
        // 마우스 클릭 시 해당 위치의 픽셀 컬러 읽기
        if (Input.GetMouseButtonDown(0) && testSprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite == testSprite)
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Color pixelColor = PixelColorReader.GetPixelColorAtWorldPosition(
                    testSprite, 
                    mouseWorldPos, 
                    spriteRenderer
                );
                
                Debug.Log($"마우스 클릭 위치의 픽셀 컬러: {pixelColor}");
            }
        }
    }

    // 실제 게임에서 활용할 수 있는 예제: 스프라이트의 평균 색상 계산
    public Color GetAverageColor(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null || !sprite.texture.isReadable)
        {
            return Color.white;
        }

        Color[] pixels = PixelColorReader.GetAllPixels(sprite.texture);
        if (pixels == null) return Color.white;

        float r = 0f, g = 0f, b = 0f, a = 0f;
        int count = 0;

        Rect spriteRect = sprite.textureRect;
        for (int y = (int)spriteRect.y; y < spriteRect.y + spriteRect.height; y++)
        {
            for (int x = (int)spriteRect.x; x < spriteRect.x + spriteRect.width; x++)
            {
                int index = y * sprite.texture.width + x;
                if (index >= 0 && index < pixels.Length)
                {
                    Color pixel = pixels[index];
                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                    a += pixel.a;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            return new Color(r / count, g / count, b / count, a / count);
        }

        return Color.white;
    }
}

