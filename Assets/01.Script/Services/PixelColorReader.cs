using UnityEngine;

/// <summary>
/// 이미지(Texture2D/Sprite)의 픽셀 컬러를 읽어오는 유틸리티 클래스
/// </summary>
public static class PixelColorReader
{
    /// <summary>
    /// Texture2D에서 특정 좌표의 픽셀 컬러를 읽어옵니다.
    /// </summary>
    /// <param name="texture">읽을 텍스처</param>
    /// <param name="x">픽셀 X 좌표 (0 ~ width-1)</param>
    /// <param name="y">픽셀 Y 좌표 (0 ~ height-1)</param>
    /// <returns>해당 픽셀의 Color (좌표가 범위를 벗어나면 Color.clear 반환)</returns>
    public static Color GetPixelColor(Texture2D texture, int x, int y)
    {
        if (texture == null)
        {
            Debug.LogError("Texture2D가 null입니다.");
            return Color.clear;
        }

        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            Debug.LogWarning($"픽셀 좌표 ({x}, {y})가 텍스처 크기 ({texture.width}, {texture.height})를 벗어났습니다.");
            return Color.clear;
        }

        // 텍스처가 읽기 가능한지 확인 (Read/Write Enabled가 되어 있어야 함)
        if (!texture.isReadable)
        {
            Debug.LogError($"텍스처 '{texture.name}'의 Read/Write가 비활성화되어 있습니다. " +
                          "Texture Import Settings에서 'Read/Write Enabled'를 체크해주세요.");
            return Color.clear;
        }

        return texture.GetPixel(x, y);
    }

    /// <summary>
    /// Sprite에서 특정 좌표의 픽셀 컬러를 읽어옵니다.
    /// </summary>
    /// <param name="sprite">읽을 스프라이트</param>
    /// <param name="x">스프라이트 내부 픽셀 X 좌표 (0 ~ sprite.rect.width-1)</param>
    /// <param name="y">스프라이트 내부 픽셀 Y 좌표 (0 ~ sprite.rect.height-1)</param>
    /// <returns>해당 픽셀의 Color</returns>
    public static Color GetPixelColor(Sprite sprite, int x, int y)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite가 null입니다.");
            return Color.clear;
        }

        Texture2D texture = sprite.texture;
        if (texture == null)
        {
            Debug.LogError("Sprite의 Texture가 null입니다.");
            return Color.clear;
        }

        // 스프라이트의 텍스처 내 실제 좌표로 변환
        Rect spriteRect = sprite.textureRect;
        int textureX = Mathf.RoundToInt(spriteRect.x) + x;
        int textureY = Mathf.RoundToInt(spriteRect.y) + y;

        return GetPixelColor(texture, textureX, textureY);
    }

    /// <summary>
    /// 월드 좌표를 스프라이트 픽셀 좌표로 변환하여 컬러를 읽어옵니다.
    /// </summary>
    /// <param name="sprite">스프라이트</param>
    /// <param name="worldPosition">월드 좌표</param>
    /// <param name="spriteRenderer">스프라이트 렌더러 (트랜스폼 정보 필요)</param>
    /// <returns>해당 위치의 픽셀 컬러</returns>
    public static Color GetPixelColorAtWorldPosition(Sprite sprite, Vector2 worldPosition, SpriteRenderer spriteRenderer)
    {
        if (sprite == null || spriteRenderer == null)
        {
            Debug.LogError("Sprite 또는 SpriteRenderer가 null입니다.");
            return Color.clear;
        }

        // 월드 좌표를 로컬 좌표로 변환
        Vector3 localPos = spriteRenderer.transform.InverseTransformPoint(worldPosition);

        // 픽셀 퍼 유닛(PPU)을 고려하여 픽셀 좌표 계산
        float pixelsPerUnit = sprite.pixelsPerUnit;
        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;

        // 스프라이트의 중심을 기준으로 픽셀 좌표 계산
        int pixelX = Mathf.RoundToInt((localPos.x * pixelsPerUnit) + (spriteWidth * 0.5f));
        int pixelY = Mathf.RoundToInt((localPos.y * pixelsPerUnit) + (spriteHeight * 0.5f));

        // 스프라이트의 픽셀 좌표 범위 내인지 확인
        if (pixelX < 0 || pixelX >= spriteWidth || pixelY < 0 || pixelY >= spriteHeight)
        {
            return Color.clear; // 범위 밖이면 투명 반환
        }

        return GetPixelColor(sprite, pixelX, pixelY);
    }

    /// <summary>
    /// Texture2D의 모든 픽셀 컬러를 배열로 반환합니다.
    /// </summary>
    /// <param name="texture">읽을 텍스처</param>
    /// <returns>픽셀 컬러 배열 (1차원 배열, [y * width + x] 형태)</returns>
    public static Color[] GetAllPixels(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Texture2D가 null입니다.");
            return null;
        }

        if (!texture.isReadable)
        {
            Debug.LogError($"텍스처 '{texture.name}'의 Read/Write가 비활성화되어 있습니다.");
            return null;
        }

        return texture.GetPixels();
    }

    /// <summary>
    /// 특정 컬러와 유사한 픽셀의 좌표를 모두 찾습니다.
    /// </summary>
    /// <param name="texture">검색할 텍스처</param>
    /// <param name="targetColor">찾을 컬러</param>
    /// <param name="tolerance">허용 오차 (0~1, 색상 차이 허용 범위)</param>
    /// <returns>매칭되는 픽셀 좌표 리스트</returns>
    public static System.Collections.Generic.List<Vector2Int> FindPixelsWithColor(
        Texture2D texture, 
        Color targetColor, 
        float tolerance = 0.1f)
    {
        var result = new System.Collections.Generic.List<Vector2Int>();

        if (texture == null || !texture.isReadable)
        {
            Debug.LogError("텍스처를 읽을 수 없습니다.");
            return result;
        }

        Color[] pixels = texture.GetPixels();
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                int index = y * texture.width + x;
                Color pixelColor = pixels[index];

                // 색상 차이 계산 (RGB 거리)
                float colorDistance = Mathf.Sqrt(
                    Mathf.Pow(pixelColor.r - targetColor.r, 2) +
                    Mathf.Pow(pixelColor.g - targetColor.g, 2) +
                    Mathf.Pow(pixelColor.b - targetColor.b, 2)
                );

                if (colorDistance <= tolerance)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }

        return result;
    }
}

