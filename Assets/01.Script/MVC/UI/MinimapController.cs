using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("Minimap Settings")]
    [Tooltip("The UI Panel that will contain all minimap elements.")]
    public RectTransform container;
    [Tooltip("The color for the player's icon on the minimap.")]
    public Color playerColor = Color.red;
    [Tooltip("How much to scale down the world positions and sizes to fit the minimap.")]
    public float mapScale = 5.0f;

    [Header("Minimap Room Style")]
    [Tooltip("Highlight color for the room the player is currently in.")]
    public Color currentPlayerRoomColor = new Color(1f, 1f, 0.7f, 0.9f); // Light Yellow
    [Tooltip("Colors for uncleared rooms, ordered K, C, M, Y.")]
    public Color[] unclearedCmykColors = {
        new Color(0.3f, 0.3f, 0.3f, 0.7f),
        new Color(0.0f, 0.5f, 0.5f, 0.7f),
        new Color(0.5f, 0.0f, 0.5f, 0.7f),
        new Color(0.5f, 0.5f, 0.0f, 0.7f)
    };
    [Tooltip("Colors for cleared rooms, ordered K, C, M, Y.")]
    public Color[] clearedCmykColors = {
        new Color(0.6f, 0.6f, 0.6f, 0.7f),
        new Color(0.5f, 1.0f, 1.0f, 0.7f),
        new Color(1.0f, 0.5f, 1.0f, 0.7f),
        new Color(1.0f, 1.0f, 0.5f, 0.7f)
    };
    
    [Header("Minimap Room Outline")]
    [Tooltip("How much darker the outline is compared to the fill color. (e.g., 0.7 is 70% as bright).")]
    [Range(0f, 1f)]
    public float outlineDarknessFactor = 0.7f;
    [Tooltip("The thickness and direction of the outline effect.")]
    public Vector2 outlineEffectDistance = new Vector2(1.5f, -1.5f);

    [Header("Minimap Corridor Style")]
    [Tooltip("Color for corridors on the minimap.")]
    public Color corridorColor = Color.grey;

    private Transform playerTransform;
    private RectTransform playerIcon;
    private RectTransform minimapContent; // Parent for all map icons, which will be moved.
    private Dictionary<RoomController, Image> roomIconMap = new Dictionary<RoomController, Image>();
    private RoomController currentPlayerRoom = null;

    void Start()
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("MinimapController: Player not found in the scene!");
            return;
        }

        if (container != null)
        {
            // Create a parent object for all map icons that will move
            GameObject contentObj = new GameObject("MinimapContent");
            contentObj.transform.SetParent(container, false);
            minimapContent = contentObj.AddComponent<RectTransform>();
            minimapContent.anchorMin = minimapContent.anchorMax = minimapContent.pivot = new Vector2(0.5f, 0.5f);
            minimapContent.anchoredPosition = Vector2.zero;
            minimapContent.sizeDelta = Vector2.zero;

            // Create the player icon, which stays fixed in the center
            GameObject playerIconObj = new GameObject("Player Icon");
            playerIconObj.transform.SetParent(container, false);
            Image playerImage = playerIconObj.AddComponent<Image>();
            playerImage.color = playerColor;
            playerIcon = playerIconObj.GetComponent<RectTransform>();
            playerIcon.sizeDelta = new Vector2(10, 10);
            playerIcon.anchorMin = playerIcon.anchorMax = playerIcon.pivot = new Vector2(0.5f, 0.5f);
            playerIcon.anchoredPosition = Vector2.zero;

            // Ensure content is rendered behind the player icon
            minimapContent.SetAsFirstSibling();
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // --- Update Minimap Position ---
        // Instead of moving every icon, we move the single parent container.
        if (minimapContent != null)
        {
            Vector2 playerPos = new Vector2(playerTransform.position.x, playerTransform.position.y);
            minimapContent.anchoredPosition = -playerPos * mapScale;
        }

        // --- Update Room Colors ---
        RoomController roomPlayerIsIn = null;
        // This check can be expensive, but it's the most reliable way to find the current room
        foreach (var room in roomIconMap.Keys)
        {
            SpriteRenderer floor = room.transform.Find("Floor")?.GetComponent<SpriteRenderer>();
            if (floor != null && floor.bounds.Contains(playerTransform.position))
            {
                roomPlayerIsIn = room;
                break;
            }
        }

        if (roomPlayerIsIn != currentPlayerRoom)
        {
            if (currentPlayerRoom != null && roomIconMap.ContainsKey(currentPlayerRoom))
            {
                SetRoomColor(roomIconMap[currentPlayerRoom], GetRoomColor(currentPlayerRoom, currentPlayerRoom.isCleared));
            }
            if (roomPlayerIsIn != null && roomIconMap.ContainsKey(roomPlayerIsIn))
            {
                SetRoomColor(roomIconMap[roomPlayerIsIn], currentPlayerRoomColor);
            }
            currentPlayerRoom = roomPlayerIsIn;
        }
        
        // This loop ensures that as rooms are cleared, their color updates on the minimap
        foreach(var entry in roomIconMap)
        {
            if (entry.Key != currentPlayerRoom && entry.Key.isCleared)
            {
                Color clearedColor = GetRoomColor(entry.Key, true);
                if (entry.Value.color != clearedColor)
                {
                    SetRoomColor(entry.Value, clearedColor);
                }
            }
        }
    }

    public void Generate(List<RoomController> rooms)
    {
        if (container == null || minimapContent == null) return;

        // Clear previous minimap elements from the content container
        foreach (Transform child in minimapContent)
        {
            Destroy(child.gameObject);
        }
        roomIconMap.Clear();

        HashSet<Vector3> generatedCorridorPositions = new HashSet<Vector3>();

        // Get DungeonController to access map-level corridors
        DungeonController dungeonController = Object.FindAnyObjectByType<DungeonController>();
        if (dungeonController != null && dungeonController.corridorParent != null)
        {
            // First pass: Generate map-level corridors to ensure they are drawn below rooms
            for (int i = 0; i < dungeonController.corridorParent.childCount; i++)
            {
                GameObject corridorGO = dungeonController.corridorParent.GetChild(i).gameObject;

                if (corridorGO != null && corridorGO.activeInHierarchy)
                {
                    // Prevent duplicate corridors from being drawn (e.g., if a dungeon generation bug causes it)
                    if (generatedCorridorPositions.Contains(corridorGO.transform.position)) continue;
                    generatedCorridorPositions.Add(corridorGO.transform.position);

                    // --- Create corridor icon using the same Mask/Fill/Outline method as rooms ---
                    GameObject maskObj = new GameObject("Corridor");
                    maskObj.transform.SetParent(minimapContent, false); // Parent to the moving content
                    Image maskImage = maskObj.AddComponent<Image>();
                    Mask mask = maskObj.AddComponent<Mask>();
                    mask.showMaskGraphic = false;

                    SpriteRenderer corridorRenderer = corridorGO.transform.Find("Floor")?.GetComponent<SpriteRenderer>();
                    if (corridorRenderer != null && corridorRenderer.sprite != null)
                    {
                        maskImage.sprite = corridorRenderer.sprite;
                    }

                    // Create the visible fill image as a child of the mask
                    GameObject fillObj = new GameObject("Fill");
                    fillObj.transform.SetParent(maskObj.transform, false);
                    Image fillImage = fillObj.AddComponent<Image>();
                    fillImage.color = corridorColor;

                    // Add the outline to the MASK object
                    Outline outline = maskObj.AddComponent<Outline>();
                    Color outlineColor = new Color(corridorColor.r * outlineDarknessFactor, corridorColor.g * outlineDarknessFactor, corridorColor.b * outlineDarknessFactor, 1f);
                    outline.effectColor = outlineColor;
                    outline.effectDistance = outlineEffectDistance;

                    // Set sizes and position
                    RectTransform maskRect = maskObj.GetComponent<RectTransform>();
                    RectTransform fillRect = fillObj.GetComponent<RectTransform>();

                    if (corridorRenderer != null)
                    {
                        maskRect.sizeDelta = new Vector2(corridorRenderer.bounds.size.x, corridorRenderer.bounds.size.y) * mapScale;
                    }
                    else
                    {
                        maskRect.sizeDelta = new Vector2(corridorGO.transform.localScale.x, corridorGO.transform.localScale.y) * mapScale;
                    }
                    // The fill rect should stretch to fill its parent (the mask)
                    fillRect.anchorMin = Vector2.zero;
                    fillRect.anchorMax = Vector2.one;
                    fillRect.sizeDelta = Vector2.zero;

                    // Set position based on world coordinates
                    maskRect.anchoredPosition = new Vector2(corridorGO.transform.position.x, corridorGO.transform.position.y) * mapScale;
                }
            }
        }
        else
        {
            Debug.LogWarning("MinimapController: DungeonController or its corridorParent not found. Corridors will not be displayed on minimap.");
        }

        // Second pass: Generate rooms
        foreach (RoomController room in rooms)
        {
            GameObject maskObj = new GameObject($"Room ({room.transform.position.x}, {room.transform.position.y})");
            maskObj.transform.SetParent(minimapContent, false); // Parent to the moving content
            Image maskImage = maskObj.AddComponent<Image>();
            Mask mask = maskObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            SpriteRenderer floorRenderer = room.transform.Find("Floor")?.GetComponent<SpriteRenderer>();
            if (floorRenderer != null && floorRenderer.sprite != null)
            {
                maskImage.sprite = floorRenderer.sprite;
            }

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(maskObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            
            Color initialColor = GetRoomColor(room, room.isCleared);
            fillImage.color = initialColor;

            Outline outline = maskObj.AddComponent<Outline>();
            Color outlineColor = new Color(initialColor.r * outlineDarknessFactor, initialColor.g * outlineDarknessFactor, initialColor.b * outlineDarknessFactor, 1f);
            outline.effectColor = outlineColor;
            outline.effectDistance = outlineEffectDistance;

            RectTransform maskRect = maskObj.GetComponent<RectTransform>();
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();

            if (floorRenderer != null)
            {
                maskRect.sizeDelta = new Vector2(floorRenderer.bounds.size.x, floorRenderer.bounds.size.y) * mapScale;
            }
            else
            {
                maskRect.sizeDelta = new Vector2(20, 20) * mapScale;
            }
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // Set position based on world coordinates
            maskRect.anchoredPosition = new Vector2(room.transform.position.x, room.transform.position.y) * mapScale;

            roomIconMap[room] = fillImage; // Store for color updates
        }
        
        if (playerIcon != null) playerIcon.transform.SetAsLastSibling();
        
        LateUpdate(); // Initial update
    }

    private Color GetRoomColor(RoomController room, bool isCleared)
    {
        int colorIndex = (int)room.roomColor;
        Color fallbackColor = isCleared ? Color.white : Color.gray;
        
        if (isCleared)
        {
            if (clearedCmykColors != null && clearedCmykColors.Length > colorIndex)
                return clearedCmykColors[colorIndex];
        }
        else
        {
            if (unclearedCmykColors != null && unclearedCmykColors.Length > colorIndex)
                return unclearedCmykColors[colorIndex];
        }
        return fallbackColor;
    }

    // Helper to set both fill and outline color
    private void SetRoomColor(Image fillImage, Color fillColor)
    {
        if (fillImage == null) return;
        fillImage.color = fillColor;
        
        // The outline is on the parent object
        Outline outline = fillImage.GetComponentInParent<Outline>();
        if (outline != null)
        {
            // Set outline alpha to 1 for visibility, but base color on the fill
            Color outlineColor = new Color(fillColor.r * outlineDarknessFactor, 
                                         fillColor.g * outlineDarknessFactor, 
                                         fillColor.b * outlineDarknessFactor, 
                                         1f); // Use full alpha for outline
            outline.effectColor = outlineColor;
        }
    }
}
