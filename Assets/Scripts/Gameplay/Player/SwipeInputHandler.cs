using System;
using UnityEngine;

namespace GameBalap.Gameplay.Player
{
    public class SwipeInputHandler : MonoBehaviour
    {
        public event Action OnSwipeLeft;
        public event Action OnSwipeRight;
        public event Action OnSwipeUp;
        public event Action OnSwipeDown;

        [Header("Touch Settings")]
        [SerializeField] private float minSwipeDistance = 40f; // pixel

        private Vector2 touchStartPos;
        private bool isSwiping = false;

        private void Update()
        {
            HandleKeyboardInput();
            HandleTouchInput();
        }

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnSwipeLeft?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnSwipeRight?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
            {
                OnSwipeUp?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                OnSwipeDown?.Invoke();
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isSwiping = true;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (!isSwiping) return;

                    Vector2 delta = touch.position - touchStartPos;
                    if (delta.magnitude >= minSwipeDistance)
                    {
                        // Tentukan apakah swipe horizontal atau vertikal
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            if (delta.x > 0) OnSwipeRight?.Invoke();
                            else OnSwipeLeft?.Invoke();
                        }
                        else
                        {
                            if (delta.y > 0) OnSwipeUp?.Invoke();
                            else OnSwipeDown?.Invoke();
                        }
                    }
                    isSwiping = false;
                    break;
            }
        }
    }
}
