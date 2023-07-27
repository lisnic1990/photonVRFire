using UnityEngine;
using Fusion;

namespace Projectiles.NetworkObjectExample
{
	// PhysicsProjectile is a NetworkObject spawned in a scene. It uses NetworkRigidbody to synchronize
	// its position and rotation constantly to all clients. This is inefficient and should really be used only
	// for special scenarios (e.g. large rolling projectile that needs to use Rigidbody) or when simplicity is the key.
	// See Projectiles Advanced how even grenades can be done without spawning separate NetworkObjects.
	[RequireComponent(typeof(NetworkRigidbody))]
	public class PhysicsProjectile : NetworkBehaviour
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

		[Networked]
		private TickTimer _lifeCooldown { get; set; }
		[Networked(OnChanged = nameof(OnDestroyChanged))]
		private NetworkBool _isDestroyed { get; set; }

		private Rigidbody _rigidbody;
		private Collider _collider;

		// PUBLIC METHODS

		public void Fire()
		{
			// Rigidbody values will be synchronized to other clients via NetworkRigidbody
			// If physics prediction would be turned off in NetworkProjectConfig, it would be
			// more efficient to just use NetworkTransform to synchronize position/rotation
			_rigidbody.AddForce(transform.forward * _initialImpulse, ForceMode.Impulse);

			// Set cooldown after which the projectile should be despawned
			if (_lifeTime > 0f)
			{
				_lifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTime);
			}
		}

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			// Collider is enabled even on proxies because projectiles are predicted
			// (physics prediction is turned on)
			_collider.enabled = _isDestroyed == false;

			if (_lifeCooldown.IsRunning == true && _lifeCooldown.Expired(Runner) == true)
			{
				Runner.Despawn(Object);
			}
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_rigidbody = GetComponent<Rigidbody>();
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
				ProcessHit();
			}
		}

		// PRIVATE METHODS

		private void ProcessHit()
		{
			// Save destroyed flag so hit effects can be shown on other clients as well
			_isDestroyed = true;

			_lifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTimeAfterHit);

			// Stop the movement
			_rigidbody.isKinematic = true;
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

		public static void OnDestroyChanged(Changed<PhysicsProjectile> changed)
		{
			changed.Behaviour.ShowDestroyEffect();
		}
	}
}
