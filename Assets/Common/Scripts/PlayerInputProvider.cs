using Fusion;
using Fusion.XR.Host.Rig;
using UnityEngine;

namespace Projectiles
{
    public enum EInputButtons
    {
        Fire = 0,
    }

    public struct PlayerInput : INetworkInput
    {
        public Vector2 LookRotationDelta;
        public Vector2 MoveDirection;
        public NetworkButtons Buttons;

        public bool Fire { get { return Buttons.IsSet(EInputButtons.Fire); } set { Buttons.Set((int)EInputButtons.Fire, value); } }
    }

    public class PlayerInputProvider : SimulationBehaviour, ISpawned, IDespawned, IBeforeUpdate
    {
        // PUBLIC MEMBERS

        // Cached input holds combined input for all render frames from last fixed update
        public PlayerInput CachedInput => _cachedInput;

        // PRIVATE MEMBERS

        [SerializeField]
        private DebugInputControl _inputControl;

        private PlayerInput _cachedInput;
        private bool _resetCachedInput;

        // NETWORK INTERFACES

        void ISpawned.Spawned()
        {
            // Reset to default state (in case this object was cached)
            _cachedInput = default;

            if (Runner.LocalPlayer == Object.InputAuthority)
            {
                /*
				var events = Runner.GetComponent<NetworkEvents>();

				events.OnInput.RemoveListener(OnInput);
				events.OnInput.AddListener(OnInput);
				*/

                _inputControl.RequestCursorLock();
            }
        }

        void IDespawned.Despawned(NetworkRunner runner, bool hasState)
        {
            /*
			var events = Runner.GetComponent<NetworkEvents>();
			events.OnInput.RemoveListener(OnInput);
			*/

            if (Runner.LocalPlayer == Object.InputAuthority)
            {
                _inputControl.RequestCursorRelease();
            }
        }

        void IBeforeUpdate.BeforeUpdate()
        {
            if (Object == null || Object.HasInputAuthority == false)
                return;

            if (_resetCachedInput == true)
            {
                _resetCachedInput = false;
                _cachedInput = default;
            }

            // Input is tracked only if the runner should provide input (important in multipeer mode)
            if (Runner.ProvideInput == false || _inputControl.IsLocked == false)
                return;

            ProcessKeyboardInput();
        }

        // PRIVATE METHODS

        /*
		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			// Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
			// This is the reason for having a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).
			_resetCachedInput = true;

			networkInput.Set(_cachedInput);

			// Input consumed by OnInput() call will be read in OnFixedUpdate() and immediately propagated to KCC.
			// Here we should reset RELATIVE input properties so they are not applied twice (fixed + render update)
			_cachedInput.LookRotationDelta = default;
		}
		*/

        public void OnInputMethod(ref RigInput rigInput)
        {
            // Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
            // This is the reason for having a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).
            _resetCachedInput = true;

            rigInput.Buttons = _cachedInput.Buttons;
            rigInput.Fire = _cachedInput.Fire;
            rigInput.LookRotationDelta = _cachedInput.LookRotationDelta;
            rigInput.MoveDirection = _cachedInput.MoveDirection;

            // Input consumed by OnInput() call will be read in OnFixedUpdate() and immediately propagated to KCC.
            // Here we should reset RELATIVE input properties so they are not applied twice (fixed + render update)
            _cachedInput.LookRotationDelta = default;
        }

        private void ProcessKeyboardInput()
        {
            if (Input.GetKey(KeyCode.Space) == true || Input.GetMouseButton(0) == true)
            {
                _cachedInput.Fire = true;
            }

            Vector2 moveDirection = default;

            if (Input.GetKey(KeyCode.W) == true) { moveDirection += Vector2.up; }
            if (Input.GetKey(KeyCode.S) == true) { moveDirection += Vector2.down; }
            if (Input.GetKey(KeyCode.A) == true) { moveDirection += Vector2.left; }
            if (Input.GetKey(KeyCode.D) == true) { moveDirection += Vector2.right; }

            _cachedInput.MoveDirection = moveDirection.normalized;

            var lookDelta = new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"));
            _cachedInput.LookRotationDelta += lookDelta;
        }

        public void OnPressFire()
        {
            _cachedInput.Fire = true;
        }
    }
}
