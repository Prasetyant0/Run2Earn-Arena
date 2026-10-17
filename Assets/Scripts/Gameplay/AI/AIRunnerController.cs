using System.Collections;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Gameplay.Race;
using GameBalap.Gameplay.Player;

namespace GameBalap.Gameplay.AI
{
    [RequireComponent(typeof(CharacterController))]
    public class AIRunnerController : MonoBehaviour
    {
        [Header("AI Identity & Profile")]
        [SerializeField] private string botName = "Bot_Speedy";
        [Range(0.6f, 1.0f)]
        [SerializeField] private float skillLevel = 0.88f; // Probabilitas menghindari rintangan dengan tepat

        [Header("Movement")]
        [SerializeField] private float baseSpeed = GameConstants.DEFAULT_RUN_SPEED * 0.98f;
        [SerializeField] private float maxSpeed = GameConstants.MAX_RUN_SPEED * 0.95f;
        [SerializeField] private float acceleration = GameConstants.SPEED_ACCELERATION;
        [SerializeField] private float jumpForce = GameConstants.JUMP_FORCE;
        [SerializeField] private float gravity = GameConstants.GRAVITY;

        [Header("State")]
        [SerializeField] private Lane currentLane = Lane.Right;
        [SerializeField] private CharacterMovementState currentState = CharacterMovementState.Running;
        [SerializeField] private float currentSpeed = 0f;

        private CharacterController characterController;
        private BlockyCharacterAnimator characterAnimator;
        private float verticalVelocity = 0f;
        private bool isRacing = false;
        private float detectionDistance = 14f;

        public string BotName => botName;
        public float DistanceTravelled => transform.position.z;
        public CharacterMovementState CurrentState => currentState;

        private void Awake()
        {
            gameObject.tag = "Opponent";

            characterController = GetComponent<CharacterController>();
            characterController.radius = 0.38f;
            characterController.height = 1.9f;
            characterController.center = new Vector3(0, 0.95f, 0);
            characterController.stepOffset = 0.35f;

            characterAnimator = GetComponent<BlockyCharacterAnimator>();
            if (characterAnimator == null)
            {
                characterAnimator = gameObject.AddComponent<BlockyCharacterAnimator>();
            }
            characterAnimator.SetupCharacter(asRival: true);

            var particles = GetComponent<RunnerParticleEffects>();
            if (particles == null)
            {
                gameObject.AddComponent<RunnerParticleEffects>();
            }
        }

        private void Update()
        {
            // Proteksi agar bot tidak jatuh ke ruang hampa
            if (transform.position.y < -3.5f)
            {
                transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
                verticalVelocity = 0f;
            }

            // Jika finish, deselerasi hingga berhenti
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

            // Akselerasi
            if (currentSpeed < maxSpeed && currentState != CharacterMovementState.Stumbled)
            {
                currentSpeed += acceleration * Time.deltaTime;
            }

            // Pergerakan Maju
            Vector3 moveVector = Vector3.forward * currentSpeed;

            // Pergerakan Jalur Horizontal
            float targetX = (int)currentLane * GameConstants.LANE_WIDTH;
            float newX = Mathf.MoveTowards(transform.position.x, targetX, GameConstants.LANE_CHANGE_SPEED * Time.deltaTime);
            moveVector.x = (newX - transform.position.x) / Time.deltaTime;

            // Gravitasi & Lompat
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -1f;

                if (currentState == CharacterMovementState.Jumping)
                {
                    currentState = CharacterMovementState.Running;
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            moveVector.y = verticalVelocity;
            characterController.Move(moveVector * Time.deltaTime);

            // Sensor deteksi rintangan di depan jalur saat ini
            DetectAndReactToObstacles();

            characterAnimator?.UpdateAnimation(currentState, currentSpeed, moveVector.x);
        }

        private void DetectAndReactToObstacles()
        {
            if (currentState == CharacterMovementState.Jumping || currentState == CharacterMovementState.Sliding)
                return;

            Vector3 rayOrigin = transform.position + Vector3.up * 1f;
            if (Physics.Raycast(rayOrigin, Vector3.forward, out RaycastHit hit, detectionDistance))
            {
                if (hit.collider.TryGetComponent<Obstacle>(out var obs))
                {
                    // Uji kecerdasan berdasarkan skillLevel
                    bool reactsSuccessfully = Random.value <= skillLevel;

                    if (reactsSuccessfully)
                    {
                        switch (obs.Type)
                        {
                            case ObstacleType.HurdleLow:
                                Jump();
                                break;
                            case ObstacleType.HighBeam:
                                Slide();
                                break;
                            case ObstacleType.BarrierFull:
                                DodgeLane();
                                break;
                        }
                    }
                }
            }
        }

        private void DodgeLane()
        {
            if (currentLane == Lane.Middle)
            {
                currentLane = Random.value > 0.5f ? Lane.Left : Lane.Right;
            }
            else
            {
                currentLane = Lane.Middle;
            }
        }

        private void Jump()
        {
            if (characterController.isGrounded && currentState != CharacterMovementState.Sliding)
            {
                verticalVelocity = jumpForce;
                currentState = CharacterMovementState.Jumping;
            }
        }

        private void Slide()
        {
            StartCoroutine(AISlideRoutine());
        }

        private IEnumerator AISlideRoutine()
        {
            currentState = CharacterMovementState.Sliding;
            yield return new WaitForSeconds(GameConstants.SLIDE_DURATION);
            if (currentState == CharacterMovementState.Sliding)
            {
                currentState = CharacterMovementState.Running;
            }
        }

        public void OnHitObstacle()
        {
            if (currentState == CharacterMovementState.Finished) return;
            StartCoroutine(AIStumbleRoutine());
        }

        private IEnumerator AIStumbleRoutine()
        {
            currentState = CharacterMovementState.Stumbled;
            float origSpeed = currentSpeed;
            currentSpeed *= GameConstants.STUMBLE_SPEED_PENALTY;

            yield return new WaitForSeconds(GameConstants.STUMBLE_DURATION);

            currentSpeed = origSpeed * 0.85f;
            if (currentState == CharacterMovementState.Stumbled)
            {
                currentState = CharacterMovementState.Running;
            }
        }

        public void StartRacing()
        {
            isRacing = true;
            currentSpeed = baseSpeed;
            currentState = CharacterMovementState.Running;
        }

        public void StopRacing()
        {
            isRacing = false;
            currentState = CharacterMovementState.Finished;
        }

        public void ResetPosition(Vector3 startPos, Lane lane, string name, float botSkill)
        {
            characterController.enabled = false;
            transform.position = startPos;
            currentLane = lane;
            botName = name;
            skillLevel = botSkill;
            currentSpeed = 0f;
            verticalVelocity = 0f;
            characterController.enabled = true;
            currentState = CharacterMovementState.Running;
            characterAnimator?.UpdateAnimation(currentState, 0f);
        }
    }
}
