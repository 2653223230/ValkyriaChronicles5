using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.FX
{
    /// <summary>
    /// Text number FX that appear when a card receives damage
    /// </summary>

    public class DamageFX : MonoBehaviour
    {
        public Text text_value;
        private Animator animator;
        private bool completionReported;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (completionReported || animator == null || animator.IsInTransition(0))
                return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime < 1f)
                return;
            completionReported = true;
            Vc5DemoTutorialOverlay.NotifyDamagePresentationComplete();
        }

        public void SetValue(int value)
        {
            if (text_value != null)
                text_value.text = value.ToString();
        }

        public void SetValue(string value)
        {
            if (text_value != null)
                text_value.text = value;
        }
    }
}
