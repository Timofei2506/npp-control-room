using NPP.Core;
using NPP.Room;
using NPP.UI;
using UnityEngine;

namespace NPP.Player
{
    /// <summary>Ходьба по блочному щиту, взгляд мышью, взаимодействие с приборами.</summary>
    public class FirstPersonPlayer : MonoBehaviour
    {
        public float walkSpeed = 2.5f;
        public float runSpeed = 4.3f;
        public float mouseSensitivity = 2.2f;
        public float eyeHeight = 1.68f;

        CharacterController _cc;
        Camera _cam;
        float _yaw = 180f;
        float _pitch;
        float _vy;

        IInteractable _hover;
        float _lastMouseX, _lastMouseY;
        bool _dragging;
        float _fovBase = 66f;

        public bool InputLocked { get; set; }

        public static FirstPersonPlayer Create(Vector3 pos, float yaw)
        {
            var go = new GameObject("Player");
            go.transform.position = pos;
            var p = go.AddComponent<FirstPersonPlayer>();
            p._yaw = yaw;
            go.AddComponent<CharacterController>();
            p.Init();
            return p;
        }

        void Init()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = 0.32f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
            _cc.slopeLimit = 45f;
            _cc.stepOffset = 0.3f;

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            _cam = camGo.AddComponent<Camera>();
            _cam.nearClipPlane = 0.03f;
            _cam.farClipPlane = 120f;
            _cam.fieldOfView = _fovBase;
            _cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            mouseSensitivity = PlayerPrefs.GetFloat("npp.sens", 2.2f);
            transform.localEulerAngles = new Vector3(0f, _yaw, 0f);
        }

        void Update()
        {
            if (InputLocked)
            {
                if (_cam != null) _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _fovBase, Time.deltaTime * 8f);
                return;
            }

            // ---- обзор ----
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _yaw += mx;
            _pitch = Mathf.Clamp(_pitch - my, -84f, 84f);
            transform.localEulerAngles = new Vector3(0f, _yaw, 0f);
            _cam.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

            // ---- движение ----
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 move = transform.right * h + transform.forward * v;
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runSpeed : walkSpeed;

            if (_cc.isGrounded)
            {
                _vy = -0.5f;
                if (Input.GetKeyDown(KeyCode.Space)) _vy = 3.2f;
            }
            else _vy -= 9.81f * Time.deltaTime;

            Vector3 vel = move * speed + Vector3.up * _vy;
            _cc.Move(vel * Time.deltaTime);

            // ---- приближение ----
            bool zoom = Input.GetMouseButton(1);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, zoom ? 38f : _fovBase, Time.deltaTime * 10f);

            // ---- взаимодействие ----
            UpdateInteract();
        }

        void UpdateInteract()
        {
            _hover = null;
            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2.7f, ~0, QueryTriggerInteraction.Ignore))
            {
                var it = hit.collider.GetComponentInParent<IInteractable>();
                if (it != null) _hover = it;
            }

            var hud = GameManager.Instance != null ? GameManager.Instance.Hud : null;
            if (hud != null) hud.SetHint(_hover != null ? _hover.Hint() : "");

            if (_hover != null && Input.GetMouseButtonDown(0) && !_dragging)
            {
                _hover.Click();
                _dragging = true;
                _lastMouseX = Input.mousePosition.x;
                _lastMouseY = Input.mousePosition.y;
            }

            if (_dragging)
            {
                if (Input.GetMouseButton(0) && _hover != null)
                {
                    float dx = Input.mousePosition.x - _lastMouseX;
                    float dy = Input.mousePosition.y - _lastMouseY;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > 0.5f) _hover.Drag(new Vector2(dx, dy));
                    _lastMouseX = Input.mousePosition.x;
                    _lastMouseY = Input.mousePosition.y;
                }
                if (!Input.GetMouseButton(0)) _dragging = false;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (_hover != null && Mathf.Abs(scroll) > 0.01f) _hover.Scroll(scroll);
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            _cc.enabled = false;
            transform.position = pos;
            _yaw = yaw;
            transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            _cc.enabled = true;
        }

        public Camera Cam { get { return _cam; } }
    }
}
