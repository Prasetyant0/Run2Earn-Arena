using System;
using System.Collections;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Gameplay.Race;
using GameBalap.Gameplay.PowerUps;
using GameBalap.Customization;
using GameBalap.Audio;

namespace GameBalap.Gameplay.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Configuration")]
        [SerializeField] private float baseSpeed = GameConstants.DEFAULT_RUN_SPEED;
        [SerializeField] private float maxSpeed = GameConstants.MAX_RUN_SPEED;
        [SerializeField] private float acceleration = GameConstants.SPEED_ACCELERATION;
        [SerializeField] private float jumpForce = GameConstants.JUMP_FORCE;
        [SerializeField] private float gravity = GameConstants.GRAVITY;

        [Header("Lane & State")]
        [SerializeField] private Lane currentLane = Lane.Middle;
        [SerializeField] private CharacterMovementState currentState = CharacterMovementState.Running;
        [SerializeField] private float currentSpeed = 0f;

        // References & Internal
        private CharacterController characterController;
        private SwipeInputHandler inputHandler;
        private BlockyCharacterAnimator characterAnimator;
        private RunnerParticleEffects particleEffects;
        private PlayerPowerUpManager powerUpManager;
        private float verticalVelocity = 0f;
        private bool isRacing = false;
        private Coroutine slideCoroutine;
        private Coroutine stumbleCoroutine;

        // Collider size defaults
        private float originalHeight = 1.9f;
        private Vector3 originalCenter = new Vector3(0, 0.95f, 0);

        // Telemetry
        public int CollisionCount { get; private set; } = 0;
        public float DistanceTravelled => transform.position.z;
        public float CurrentSpeed => currentSpeed;
        public CharacterMovementState CurrentState => currentState;
        public Lane CurrentLane => currentLane;
        public PlayerPowerUpManager PowerUpManager => powerUpManager;

        // Events
        public event Action<CharacterMovementState> OnStateChanged;
        public event Action OnStumbled;

        private void Awake()
        {
            gameObject.tag = "Player";

            characterController = GetComponent<CharacterController>();
            characterController.radius = 0.38f;
            characterController.height = originalHeight;
            characterController.center = originalCenter;
            characterController.stepOffset = 0.35f;

            inputHandler = GetComponent<SwipeInputHandler>();
            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<SwipeInputHandler>();
            }

            // Pasang model karakter bergaya Roblox/Minecraft
            characterAnimator = GetComponent<BlockyCharacterAnimator>();
            if (characterAnimator == null)
            {
                characterAnimator = gameObject.AddComponent<BlockyCharacterAnimator>();
            }
            characterAnimator.SetupCharacter(asRival: false);

            // Pasang efek partikel (debu, slide sparks, speed lines, hit)
            particleEffects = GetComponent<RunnerParticleEffects>();
            if (particleEffects == null)
            {
                particleEffects = gameObject.AddComponent<RunnerParticleEffects>();
            }

            // Pasang manager power-up
            powerUpManager = GetComponent<PlayerPowerUpManager>();
            if (powerUpManager == null)
            {
                powerUpManager = gameObject.AddComponent<PlayerPowerUpManager>();
            }
        }

        private void OnEnable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnSwipeLeft += ChangeLaneLeft;
                inputHandler.OnSwipeRight += ChangeLaneRight;
                inputHandler.OnSwipeUp += Jump;
                inputHandler.OnSwipeDown += Slide;
            }
        }

        private void OnDisable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnSwipeLeft -= ChangeLaneLeft;
                inputHandler.OnSwipeRight -= ChangeLaneRight;
                inputHandler.OnSwipeUp -= Jump;
                inputHandler.OnSwipeDown -= Slide;
            }
        }

        private void Update()
        {
            // Proteksi agar pemain tidak jatuh ke ruang hampa tanpa batas
            if (transform.position.y < -3.5f)
            {
                transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
                verticalVelocity = 0f;
            }

            // Jika balapan sudah selesai, perlambat gerak hingga berhenti sempurna (decelerasi selebrasi)
            if (currentState == CharacterMovementState.Finished)
            {
                if (currentSpeed > 0f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, 15f * Time.deltaTime);
                    Vector3 decelMove = Vector3.forward * currentSpeed;

                    if (characterController.isGrounded) verticalVelocity = -1f;
                    else verticalVelocity += gravity * Time.deltaTime;
                    decelMove.y = verticalVelocity;

                    characterController.Move(decelMove * Time.deltaTime);
                }

                characterAnimator?.UpdateAnimation(currentState, currentSpeed);
                return;
            }

            if (!isRacing)
            {
                characterAnimator?.UpdateAnimation(currentState, 0f);
                return;
            }

            // Akselerasi kecepatan lari dengan bonus upgrade toko
            float maxSpeedMult = ShopManager.Instance != null ? ShopManager.Instance.GetStatMultiplier(StatUpgradeType.MaxSpeed) : 1.0f;
            float effectiveMaxSpeed = maxSpeed * maxSpeedMult;

            float accelMult = ShopManager.Instance != null ? ShopManager.Instance.GetStatMultiplier(StatUpgradeType.Acceleration) : 1.0f;
            float effectiveAccel = acceleration * accelMult;

            if (currentSpeed < effectiveMaxSpeed && currentState != CharacterMovementState.Stumbled)
            {
                currentSpeed += effectiveAccel * Time.deltaTime;
            }

            // Pergerakan Maju dengan pengganda Speed Boost (jika aktif)
            float effectiveSpeed = currentSpeed * (powerUpManager != null ? powerUpManager.SpeedBoostMultiplier : 1.0f);
            Vector3 moveVector = Vector3.forward * effectiveSpeed;

            // Hitung perpindahan horizontal ke jalur target
            float targetX = (int)currentLane * GameConstants.LANE_WIDTH;
            float newX = Mathf.MoveTowards(transform.position.x, targetX, GameConstants.LANE_CHANGE_SPEED * Time.deltaTime);
            moveVector.x = (newX - transform.position.x) / Time.deltaTime;

            // Gravitasi & Lompat
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -1f;

                if (currentState == CharacterMovementState.Jumping)
                {
                    SetState(CharacterMovementState.Running);
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            moveVector.y = verticalVelocity;
            characterController.Move(moveVector * Time.deltaTime);

            // Update animasi dinamis anggota tubuh karakter (termasuk kemiringan badan saat belok)
            characterAnimator?.UpdateAnimation(currentState, currentSpeed, moveVector.x);

            // Update efek partikel (debu lari, percikan slide, speed lines)
            particleEffects?.UpdateEffects(currentState, currentSpeed, characterController.isGrounded);
        }

        public void StartRacing()
        {
            isRacing = true;
            currentSpeed = baseSpeed;
            SetState(CharacterMovementState.Running);
        }

        public void StopRacing()
        {
            isRacing = false;
            SetState(CharacterMovementState.Finished);
            // Karakter akan mengalami deselerasi halus di Update() dan berselebrasi di garis finish
        }

        public void ResetPosition(Vector3 startPos)
        {
            characterController.enabled = false;
            transform.position = startPos;
            currentLane = Lane.Middle;
            currentSpeed = 0f;
            verticalVelocity = 0f;
            CollisionCount = 0;
            powerUpManager?.ResetAll();
            characterController.enabled = true;
            SetState(CharacterMovementState.Running);
            characterAnimator?.UpdateAnimation(currentState, 0f);
        }

        private void ChangeLaneLeft()
        {
            if (!isRacing || currentState == CharacterMovementState.Finished) return;
            if (currentLane == Lane.Right) currentLane = Lane.Middle;
            else if (currentLane == Lane.Middle) currentLane = Lane.Left;
        }

        private void ChangeLaneRight()
        {
            if (!isRacing || currentState == CharacterMovementState.Finished) return;
            if (currentLane == Lane.Left) currentLane = Lane.Middle;
            else if (currentLane == Lane.Middle) currentLane = Lane.Right;
        }

        private void Jump()
        {
            if (!isRacing || currentState == CharacterMovementState.Finished) return;

            if (characterController.isGrounded && currentState != CharacterMovementState.Sliding)
            {
                verticalVelocity = jumpForce;
                SetState(CharacterMovementState.Jumping);
                AudioManager.Instance?.PlayJump();
            }
        }

        private void Slide()
        {
            if (!isRacing || currentState == CharacterMovementState.Finished) return;

            // Jika sedang di udara, percepat jatuh ke tanah
            if (!characterController.isGrounded)
            {
                verticalVelocity = -jumpForce * 1.5f;
            }

            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideRoutine());
            AudioManager.Instance?.PlaySlide();
        }

        private IEnumerator SlideRoutine()
        {
            SetState(CharacterMovementState.Sliding);

            // Kecilkan ukuran collider saat berseluncur agar bisa lewat di bawah balok atas
            characterController.height = 0.8f;
            characterController.center = new Vector3(originalCenter.x, 0.4f, originalCenter.z);

            yield return new WaitForSeconds(GameConstants.SLIDE_DURATION);

            // Kembalikan ukuran collider
            characterController.height = originalHeight;
            characterController.center = originalCenter;

            if (currentState == CharacterMovementState.Sliding)
            {
                SetState(CharacterMovementState.Running);
            }
            slideCoroutine = null;
        }

        public void OnHitObstacle(Obstacle obstacle)
        {
            if (currentState == CharacterMovementState.Finished) return;

            // Jika perisai energi atau speed boost menyerap tabrakan
            if (powerUpManager != null && powerUpManager.TryAbsorbCollision())
            {
                return;
            }

            CollisionCount++;
            Debug.LogWarning($"[PlayerController] Menabrak rintangan {obstacle.Type}! Total tabrakan: {CollisionCount}");

            particleEffects?.PlayHitEffect();
            AudioManager.Instance?.PlayHit();

            if (stumbleCoroutine != null) StopCoroutine(stumbleCoroutine);
            stumbleCoroutine = StartCoroutine(StumbleRoutine());

            OnStumbled?.Invoke();
        }

        private IEnumerator StumbleRoutine()
        {
            SetState(CharacterMovementState.Stumbled);

            // Penalti kecepatan berkurang 50%
            float originalSpeed = currentSpeed;
            currentSpeed *= GameConstants.STUMBLE_SPEED_PENALTY;

            yield return new WaitForSeconds(GameConstants.STUMBLE_DURATION);

            // Pulihkan kecepatan secara bertahap
            currentSpeed = originalSpeed * 0.85f;
            if (currentState == CharacterMovementState.Stumbled)
            {
                SetState(CharacterMovementState.Running);
            }
            stumbleCoroutine = null;
        }

        private void SetState(CharacterMovementState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
