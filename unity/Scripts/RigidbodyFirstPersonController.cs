using System.IO;
using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        [Serializable]
        public class MovementSettings
        {
            public float ForwardSpeed = 8.0f;
            public float BackwardSpeed = 4.0f;
            public float StrafeSpeed = 4.0f;
            public float RunMultiplier = 2.0f;
            public KeyCode RunKey = KeyCode.LeftShift;
            public float JumpForce = 30f;
            public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
            [HideInInspector] public float CurrentTargetSpeed = 8f;

#if !MOBILE_INPUT
            private bool m_Running;
#endif

            public void UpdateDesiredTargetSpeed(Vector2 input)
            {
                if (input == Vector2.zero) return;
                if (input.x != 0)
                {
                    CurrentTargetSpeed = input.x > 0 ? ForwardSpeed : BackwardSpeed;
                }
#if !MOBILE_INPUT
                if (Input.GetKey(RunKey))
                {
                    CurrentTargetSpeed *= RunMultiplier;
                    m_Running = true;
                }
                else
                {
                    m_Running = false;
                }
#endif
            }

#if !MOBILE_INPUT
            public bool Running => m_Running;
#endif
        }

        [Serializable]
        public class AdvancedSettings
        {
            public float groundCheckDistance = 0.01f;
            public float stickToGroundHelperDistance = 0.5f;
            public float slowDownRate = 20f;
            public bool airControl;
            [Tooltip("set it to 0.1 or more if you get stuck in wall")]
            public float shellOffset;
        }

        public Camera cam;
        public MovementSettings movementSettings = new MovementSettings();
        public MouseLook mouseLook = new MouseLook();
        public AdvancedSettings advancedSettings = new AdvancedSettings();

        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private Vector3 m_GroundContactNormal;
        private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;

        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init(transform, cam.transform);
        }

        private void Update()
        {
            RotateView();
            if (CrossPlatformInputManager.GetButtonDown("Jump") && !m_Jump)
            {
                m_Jump = true;
            }
        }

        private void FixedUpdate()
        {
            GroundCheck();
            Vector2 input = GetInput();

            if ((Mathf.Abs(input.x) > float.Epsilon) && (advancedSettings.airControl || m_IsGrounded))
            {
                Vector3 desiredMove = cam.transform.forward * input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;
                desiredMove *= movementSettings.CurrentTargetSpeed;

                if (m_RigidBody.velocity.sqrMagnitude < (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed))
                {
                    m_RigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
                }
            }

            if (m_IsGrounded)
            {
                m_RigidBody.drag = 5f;

                if (m_Jump)
                {
                    m_RigidBody.drag = 0f;
                    m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
                    m_RigidBody.AddForce(Vector3.up * movementSettings.JumpForce, ForceMode.Impulse);
                    m_Jumping = true;
                }

                if (!m_Jumping && Mathf.Abs(input.x) < float.Epsilon && m_RigidBody.velocity.magnitude < 1f)
                {
                    m_RigidBody.Sleep();
                }
            }
            else
            {
                m_RigidBody.drag = 0f;
                if (m_PreviouslyGrounded && !m_Jumping)
                {
                    StickToGroundHelper();
                }
            }

            m_Jump = false;
        }

        private float SlopeMultiplier()
        {
            float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
            return movementSettings.SlopeCurveModifier.Evaluate(angle);
        }

        private void StickToGroundHelper()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                   ((m_Capsule.height / 2f) - m_Capsule.radius) + advancedSettings.stickToGroundHelperDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
                {
                    m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
                }
            }
        }

        private Vector2 GetInput()
        {
            string path = Application.dataPath + "/serial_output.txt";
            float gx = 0f, gy = 0f;

            try
            {
                string raw = File.ReadAllText(path).Trim();
                string[] values = raw.Split(',');
                if (values.Length >= 3)
                {
                    bool gxParsed = float.TryParse(values[0], out gx);
                    bool gyParsed = float.TryParse(values[1], out gy);

                    if (!gxParsed || !gyParsed)
                    {
                        Debug.LogWarning($"Failed to parse gx or gy: gx='{values[0]}', gy='{values[1]}'");
                        return Vector2.zero;
                    }
                }
                else
                {
                    Debug.LogWarning("IMU file does not contain enough values.");
                    return Vector2.zero;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error reading IMU data: " + e.Message);
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            float gyroDeadZone = 300f;

            if (Mathf.Abs(gx) > gyroDeadZone)
                input.x = gx < 0 ? 1f : -1f;

            if (Mathf.Abs(gy) > gyroDeadZone)
                m_YRotation -= gy * 0.001f;

            movementSettings.UpdateDesiredTargetSpeed(input);
            return input;
        }

        private void RotateView()
        {
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            transform.rotation = Quaternion.Euler(0f, m_YRotation, 0f);
            cam.transform.localRotation = Quaternion.identity;

            if (m_IsGrounded || advancedSettings.airControl)
            {
                Quaternion velRotation = Quaternion.Euler(0f, m_YRotation, 0f);
                m_RigidBody.velocity = velRotation * m_RigidBody.velocity;
            }
        }

        private void GroundCheck()
        {
            m_PreviouslyGrounded = m_IsGrounded;
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                   ((m_Capsule.height / 2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }

            if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
            {
                m_Jumping = false;
            }
        }

        public bool Grounded => m_IsGrounded;
        public bool Running => movementSettings.Running;
        public Vector3 Velocity => m_RigidBody.velocity;
    }
}