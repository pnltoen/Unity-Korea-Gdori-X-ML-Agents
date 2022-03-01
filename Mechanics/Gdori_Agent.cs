using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.InputSystem;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class Gdori_Agent : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

               
        
        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        // ML-Agents Variables
        int actionX;
        int actionY;
        bool bool_jumpUp;
        bool bool_jumpDown;
        bool control; // Heursitc 점프 제어

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }


        private void OnMovement(InputValue value)
        {
            Debug.Log("----------movement-------");
        }

        private void OnJump(InputValue value)
        {
            Debug.Log("-----------jump---------");
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            control = true;
            actionX = actions.DiscreteActions[0];
            actionY = actions.DiscreteActions[1];

            Debug.Log("actionX : " + actionX);
            Debug.Log("actionY : " + actionY);
        }

        public void OnactionX()
        {
            // 추천 행동 설정
            switch (actionX)
            {
                case 0: move.x = -1; break;
                case 1: move.x = 1; break;
            }

            //Heursitc - actionX 변수 초기 값 설정을 통한 움직임 제어
            /*switch (actionX)
            {
                case 0: break;
                case 1: move.x = -1; break;
                case 2: move.x = 1; break;
            }*/
        }


        protected override void Update()
        {
            if (controlEnabled && control)
            {
                OnactionX();
                switch (actionY)
                {
                    case 0: bool_jumpDown = true; bool_jumpUp = false; break;
                    case 1: bool_jumpDown = false; bool_jumpUp = true; break;
                }
                if (jumpState == JumpState.Grounded && bool_jumpDown)
                    jumpState = JumpState.PrepareToJump;

                else if (bool_jumpUp)
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}