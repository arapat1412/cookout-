using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveBurnPlashingBarUI : MonoBehaviour
{
    private const string IS_PLASHING = "IsPlashing";
    [SerializeField] private StoveCounter stoveCounter;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        stoveCounter.OnProgressChanged += StoveCounter_OnProgessChanged;
        animator.SetBool(IS_PLASHING, false);
    }

    private void StoveCounter_OnProgessChanged(object sender, IHasProgress.OnProgessChangedEventArgs e)
    {
        float burnShowProgessAmout = .5f;
        bool show = stoveCounter.IsFried() && e.progessNormalized >= burnShowProgessAmout;
        animator.SetBool(IS_PLASHING, show);
    }
   
}
