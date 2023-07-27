using Fusion.KCC;
using UnityEngine;
using Fusion;
using Fusion.XR.Host.Rig;

namespace Projectiles
{
    [OrderBefore(typeof(KCC))]
    public class SimplePlayer : NetworkBehaviour
    {
        // PRIVATE MEMBERS

        [SerializeField]
        private Transform _cameraPivot;
        [SerializeField]
        private MeshRenderer[] _thirdPersonRenderers;

        [Networked]
        private NetworkButtons _lastButtonsInput { get; set; }

        private PlayerInputProvider _inputProvider;
        // private KCC _kcc;

        private WeaponBase _weapon;
        // private Transform _cameraTransform;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            /* if (HasInputAuthority == true && _cameraTransform == null)
			{
				var scene = Runner.SimulationUnityScene.GetComponent<SimpleScene>();
				_cameraTransform = scene.Camera.transform;
			}*/
        }

        public override void FixedUpdateNetwork()
        {
            if (IsProxy == true)
                return;

            /*
			var input = GetInput<PlayerInput>();
			if (input.HasValue == true)
			{
				ProcessInput(input.Value);
            }
			*/

            var input = GetInput<RigInput>();
            if (input.HasValue == true)
            {
                ProcessInput(input.Value);
            }

        }

        public override void Render()
        {
            /*
            // Look rotation have to be updated to get smooth camera rotation

            // Get look rotation from last fixed update and add cached render delta (render delta from last fixed update)
            var lookRotation = _kcc.FixedData.GetLookRotation(true, true);
            var lookRotationDelta = KCCUtility.GetClampedLookRotationDelta(lookRotation, _inputProvider.CachedInput.LookRotationDelta, -90f, 90f);

            _kcc.SetLookRotation(lookRotation + lookRotationDelta);

            // Update camera pitch
            Vector2 pitchRotation = _kcc.Data.GetLookRotation(true, false);
            _cameraPivot.localRotation = Quaternion.Euler(pitchRotation);
            */
        }

        // MONOBEHAVIOUR

        protected void Awake()
        {
            _weapon = GetComponentInChildren<WeaponBase>();
            _inputProvider = GetComponent<PlayerInputProvider>();
            // _kcc = GetComponent<KCC>();
        }

        protected void LateUpdate()
        {
            /*
			if (_cameraTransform != null)
			{
				_cameraTransform.position = _cameraPivot.position;
				_cameraTransform.rotation = _cameraPivot.rotation;
			}
			*/

            if (Object != null)
            {
                // Hide meshes that should be visible only for third person players
                for (int i = 0; i < _thirdPersonRenderers.Length; i++)
                {
                    _thirdPersonRenderers[i].enabled = Runner.IsVisible == true && Object.HasInputAuthority == false;
                }
            }
        }

        // PRIVATE METHODS
        /*
		private void ProcessInput(PlayerInput input)
		{
			// Add clamped look rotation
			var lookRotation = _kcc.FixedData.GetLookRotation(true, true);
			var lookRotationDelta = KCCUtility.GetClampedLookRotationDelta(lookRotation, input.LookRotationDelta, -90f, 90f);
			_kcc.AddLookRotation(lookRotationDelta);

			// Calculate input direction based on recently updated look rotation (the change propagates internally also to KCCData.TransformRotation)
			Vector3 inputDirection = _kcc.FixedData.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			_kcc.SetInputDirection(inputDirection);

			// Update fire transform before fire
			Vector2 pitchRotation = _kcc.FixedData.GetLookRotation(true, false);
			_cameraPivot.localRotation = Quaternion.Euler(pitchRotation);

			// KCC position is not updated yet at this point and positions of all
			// enemies are not updated as well so fire will be one tick off.
			// Check PlayerAgent in AdvancedSample where Fire is initiated precisely.
			if (input.Buttons.WasPressed(_lastButtonsInput, EInputButtons.Fire) == true)
			{
				_weapon.Fire();
			}

			_lastButtonsInput = input.Buttons;
		}
		*/

        private void ProcessInput(RigInput input)
        {
            /*
            // Add clamped look rotation
            var lookRotation = _kcc.FixedData.GetLookRotation(true, true);
            var lookRotationDelta = KCCUtility.GetClampedLookRotationDelta(lookRotation, input.LookRotationDelta, -90f, 90f);
            _kcc.AddLookRotation(lookRotationDelta);

            // Calculate input direction based on recently updated look rotation (the change propagates internally also to KCCData.TransformRotation)
            Vector3 inputDirection = _kcc.FixedData.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            _kcc.SetInputDirection(inputDirection);

            // Update fire transform before fire
            Vector2 pitchRotation = _kcc.FixedData.GetLookRotation(true, false);
            _cameraPivot.localRotation = Quaternion.Euler(pitchRotation);
			*/

            // KCC position is not updated yet at this point and positions of all
            // enemies are not updated as well so fire will be one tick off.
            // Check PlayerAgent in AdvancedSample where Fire is initiated precisely.
            if (input.Buttons.WasPressed(_lastButtonsInput, EInputButtons.Fire) == true)
            {
                _weapon.Fire();
            }

            if (input.leftHandCommand.triggerCommand > 0.25f || input.rightHandCommand.triggerCommand > 0.25f)
            {
                if ((Runner.Tick.Raw - _lastFireTime) > 50)
                {
                    _weapon.Fire();
                    _lastFireTime = Runner.Tick.Raw;
                }
            }

            _lastButtonsInput = input.Buttons;
        }

        public int _lastFireTime = 0;
    }
}
