//using UnityEngine;
//using SkillSystem;

//public class SkillIndicator : MonoBehaviour
//{
//    [Header("Indicator Settings")]
//    [SerializeField] private Transform indicatorAnchor;
//    [SerializeField] private SkillCast skillCast;

//    [Header("Indicator Sprites")]
//    [SerializeField] private SpriteRenderer firstSkillRenderer;
//    [SerializeField] private SpriteRenderer secondSkillRenderer;

//    [Header("Skill State Sprites")]
//    [SerializeField] private Sprite emptySprite;
//    [SerializeField] private Sprite qSkillSprite;
//    [SerializeField] private Sprite wSkillSprite;
//    [SerializeField] private Sprite eSkillSprite;

//    [Header("Visual Settings")]
//    [SerializeField] private float indicatorSpacing = 0.5f;
//    [SerializeField] private bool hideWhenEmpty = true;
//    [SerializeField] private float fadeAlpha = 0.5f;

//    private SkillType[] previousQueue = new SkillType[2];

//    private Camera mainCamera;

//    private void Start()
//    {
//        if (skillCast == null)
//        {
//            skillCast = FindObjectOfType<SkillCast>();
//        }

//        mainCamera = Camera.main;

//        // Initialize previous queue
//        previousQueue[0] = SkillType.None;
//        previousQueue[1] = SkillType.None;

//        // Initial update
//        UpdateIndicatorDisplay();
//    }

//    private void Update()
//    {
//        UpdatePosition();

//        if (skillCast != null)
//        {
//            CheckForSkillQueueChanges();
//        }
//    }

//    private void UpdatePosition()
//    {
//        if (indicatorAnchor == null) return;

//        transform.position = indicatorAnchor.position;

//        if (mainCamera != null)
//        {
//            transform.rotation = mainCamera.transform.rotation;
//        }
//    }

//    private void CheckForSkillQueueChanges()
//    {
//        SkillType[] currentQueue = skillCast.GetSkillQueue();

//        // Check if queue has changed
//        if (currentQueue[0] != previousQueue[0] || currentQueue[1] != previousQueue[1])
//        {
//            previousQueue[0] = currentQueue[0];
//            previousQueue[1] = currentQueue[1];
//            UpdateIndicatorDisplay();
//        }
//    }

//    private void UpdateIndicatorDisplay()
//    {
//        if (skillCast == null) return;

//        SkillType[] queue = skillCast.GetSkillQueue();

//        // Update first skill indicator
//        UpdateSkillRenderer(firstSkillRenderer, queue[0]);

//        // Update second skill indicator
//        UpdateSkillRenderer(secondSkillRenderer, queue[1]);

//        // Handle visibility
//        UpdateIndicatorVisibility(queue);

//        // Position the individual indicators
//        PositionIndicators();
//    }

//    private void UpdateSkillRenderer(SpriteRenderer renderer, SkillType skillType)
//    {
//        if (renderer == null) return;

//        Sprite spriteToUse = skillType switch
//        {
//            SkillType.Q => qSkillSprite,
//            SkillType.W => wSkillSprite,
//            SkillType.E => eSkillSprite,
//            SkillType.None => emptySprite,
//            _ => emptySprite
//        };

//        renderer.sprite = spriteToUse;

//        // Adjust alpha based on skill state
//        Color color = renderer.color;
//        if (skillType == SkillType.None && hideWhenEmpty)
//        {
//            color.a = fadeAlpha;
//        }
//        else
//        {
//            color.a = 1f;
//        }
//        renderer.color = color;
//    }

//    private void UpdateIndicatorVisibility(SkillType[] queue)
//    {
//        bool hasAnySkill = queue[0] != SkillType.None || queue[1] != SkillType.None;

//        if (hideWhenEmpty && !hasAnySkill)
//        {
//            // Hide completely when no skills are queued
//            firstSkillRenderer.gameObject.SetActive(false);
//            secondSkillRenderer.gameObject.SetActive(false);
//        }
//        else
//        {
//            // Show indicators
//            firstSkillRenderer.gameObject.SetActive(true);
//            secondSkillRenderer.gameObject.SetActive(true);
//        }
//    }

//    private void PositionIndicators()
//    {
//        if (firstSkillRenderer != null && secondSkillRenderer != null)
//        {
//            // Position first indicator to the left
//            firstSkillRenderer.transform.localPosition = new Vector3(-indicatorSpacing / 2f, 0, 0);

//            // Position second indicator to the right
//            secondSkillRenderer.transform.localPosition = new Vector3(indicatorSpacing / 2f, 0, 0);
//        }
//    }
//}