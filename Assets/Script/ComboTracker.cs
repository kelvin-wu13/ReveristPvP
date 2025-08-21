using UnityEngine;

public class ComboTracker : MonoBehaviour
{
    private Animator animator;
    private int currentComboIndex = 0;
    private float timeElapsed;
    [SerializeField] private float waitTime = 1f;
    [SerializeField] private int comboAmount = 3;

    private readonly int ComboIndex = Animator.StringToHash("ComboIndex");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= waitTime)
        {
            currentComboIndex = 0;
        }
    }

    public void TriggerCombo()
    {
        timeElapsed = 0f;
        currentComboIndex++;
        if (currentComboIndex > comboAmount)
            currentComboIndex = 1;
    }

    public int GetCurrentComboIndex()
    {
        return currentComboIndex;
    }
}