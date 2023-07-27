using UnityEngine;
using Fusion;

namespace Projectiles.NetworkObjectExample
{
	// PhysicsProjectile_Predicted is a NetworkObject spawned in a scene. It uses NetworkRigidbody to synchronize
	// its position and rotation constantly to all clients. This is inefficient and should really be used only
	// for special scenarios (e.g. large rolling projectile that needs to use Rigidbody).
	// See AdvancedSample how even grenades can be done without spawning separate NetworkObjects.
	[RequireComponent(typeof(NetworkRigidbody))]
	public class PhysicsProjectile_Predicted : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _initialImpulse = 100f;
		[SerializeField]
		private float _lifeTime = 4f;
		[SerializeField]
		private GameObject _visualsRoot;
		[SerializeField]
		private GameObject _hitEffect;
		[SerializeField]
		private float _lifeTimeAfterHit = 2f;

		[Networked(OnChanged = nameof(OnIsFiredChanged))]
		private NetworkBool _isFired { get; set; }
		[Networked]
		private TickTimer _lifeCooldown { get; set; }
		[Networked(OnChanged = nameof(OnDestroyChanged))]
		private NetworkBool _isDestroyed { get; set; }

		private NetworkRigidbody _networkRigidbody;
		private Collider _collider;

		// PUBLIC METHODS

		public void Fire(Vector3 position, Quaternion rotation)
		{
			gameObject.SetActive(true);

			_networkRigidbody.TeleportToPositionRotation(position, rotation);

			// Initial physics impulse
			// Rigidbody values will be synchronized to other clients via NetworkRigidbody
			// If physics prediction would be turned off in NetworkProjectConfig, it would be
			// more efficient to just use NetworkTransform to synchronize position/rotation
			_networkRigidbody.Rigidbody.AddForce(transform.forward * _initialImpulse, ForceMode.Impulse);

			if (_lifeTime > 0f)
			{
				_lifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTime);
			}

			_isFired = true;
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (_isFired == false)
			{
				// Hide object on all clients for now
				// Object will be activated in Render after actually fired
				gameObject.SetActive(false);
			}
		}

		public override void FixedUpdateNetwork()
		{
			// Collider is enabled even on proxies because projectiles are predicted
			// (physics prediction is turned on)
			_collider.enabled = _isFired == true && _isDestroyed == false;

			if (_lifeCooldown.IsRunning == true && _lifeCooldown.Expired(Runner) == true)
			{
				Runner.Despawn(Object);
			}
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_networkRigidbody = GetComponent<NetworkRigidbody>();
			_collider = GetComponentInChildren<Collider>();

			_collider.enabled = false;

			if (_hitEffect != null)
			{
				_hitEffect.SetActive(false);
			}
		}

		protected void OnCollisionEnter(Collision collision)
		{
			if (collision.rigidbody != null && Object != null)
			{
				ProcessHit(collision.collider);
			}
		}

		// PRIVATE METHODS

		private void ProcessHit(Collider collider)
		{
			// Save destroyed flag so hit effects can be shown on other clients as well
			_isDestroyed = true;

			_lifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTimeAfterHit);

			// Stop the movement
			_networkRigidbody.Rigidbody.isKinematic = true;
			_collider.enabled = false;
		}

		private void ShowDestroyEffect()
		{
			if (_hitEffect != null)
			{
				_hitEffect.SetActive(true);
			}

			// Hide projectile visual
			if (_visualsRoot != null)
			{
				_visualsRoot.SetActive(false);
			}
		}

		// NETWORK CALLBACKS

		public static void OnDestroyChanged(Changed<PhysicsProjectile_Predicted> changed)
		{
			changed.Behaviour.ShowDestroyEffect();
		}

		public static void OnIsFiredChanged(Changed<PhysicsProjectile_Predicted> changed)
		{
			changed.Behaviour.gameObject.SetActive(changed.Behaviour._isFired);
		}
	}
}
