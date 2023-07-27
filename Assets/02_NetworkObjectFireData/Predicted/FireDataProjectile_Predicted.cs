using Fusion;
using UnityEngine;

namespace Projectiles.NetworkObjectFireData
{
	// FireDataProjectile_Predicted is still a NetworkObject spawned in the scene, but it does not use NetworkRigidbody
	// or NetworkTransform to constantly synchronize position and rotation to all peers. Instead only fire data
	// (fire position, fire rotation) is saved on the start and it's position for specific time is calculated based
	// on this data separately on all peers.
	// This approach is more suitable than to use NetworkTransform but there is still overhead when spawning new
	// NetworkObject for each projectile. For hitscan projectiles, solutions from example 03 and 04 are much more
	// efficient and easier. For kinematic projectiles use this solution only when the projectile needs to live for
	// a long time or simplicity is a key. Otherwise kinematic projectile data buffer (example 05) is a better option.

	public struct FireData : INetworkStruct
	{
		public int FireTick;
		public Vector3 FirePosition;
		public Vector3 FireVelocity;
		public NetworkBool IsDestroyed;
		public TickTimer LifeCooldown;
		public Vector3 HitPosition;
	}

	public class FireDataProjectile_Predicted : NetworkBehaviour, IPredictedSpawnBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _speed = 50f;
		[SerializeField]
		private float _lifeTime = 4f;
		[SerializeField]
		private LayerMask _hitMask;
		[SerializeField]
		private float _hitImpulse = 50f;
		[SerializeField]
		private GameObject _hitEffect;
		[SerializeField]
		private float _lifeTimeAfterHit = 2f;
		[SerializeField]
		private GameObject _visualsRoot;

		[Networked]
		private FireData _data_Networked { get; set; }
		private FireData _data_Local;

		public FireData _data
		{
			get => Object.IsPredictedSpawn == true ? _data_Local : _data_Networked;
			set { if (Object.IsPredictedSpawn == true) _data_Local = value; else _data_Networked = value; }
		}

		private bool _isDestroyed;

		// PUBLIC METHODS

		public void Fire(Vector3 position, Vector3 direction)
		{
			var data = _data;

			// Save fire data
			data.FireTick = Runner.Tick;
			data.FirePosition = position;
			data.FireVelocity = direction * _speed;

			if (_lifeTime > 0f)
			{
				data.LifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTime);
			}

			_data = data;
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			// Transform properties are not synchronized over network so rotation
			// is set in Spawn and position is set in Render manually
			transform.rotation = Quaternion.LookRotation(_data.FireVelocity);
		}

		public override void FixedUpdateNetwork()
		{
			// When object is predictively spawned IsProxy returns true (= no authority assigned)
			// but we want predicted object be treated as object with InputAuthority
			bool isProxy = IsProxy == true && Object.IsPredictedSpawn == false;

			if (isProxy == true)
				return;

			var data = _data;

			if (data.LifeCooldown.IsRunning == true && data.LifeCooldown.Expired(Runner) == true)
			{
				Runner.Despawn(Object);
				return;
			}

			if (data.IsDestroyed == true)
				return;

			// Previous and next position is calculated based on the initial parameters.
			// There is no point in actually moving the object in FUN.
			var previousPosition = GetMovePosition(Runner.Tick - 1, ref data);
			var nextPosition = GetMovePosition(Runner.Tick, ref data);

			var direction = nextPosition - previousPosition;

			float distance = direction.magnitude;
			direction /= distance; // Normalize

			var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;
			if (Runner.LagCompensation.Raycast(previousPosition, direction, distance,
				    Object.InputAuthority, out var hit, _hitMask, hitOptions) == true)
			{
				data.IsDestroyed = true;
				data.LifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTimeAfterHit);

				// Save hit position so hit effects are at correct position on proxies
				data.HitPosition = hit.Point;

				// Unfortunately physics cannot be influenced while the object existence is still predicted because
				// FixedUpdateNetwork is called only from IPredictedSpawnBehaviour.PredictedSpawnUpdate
				// which is not called during resimulations (object does not have network state yet so there is no rollback)
				if (Object.IsPredictedSpawn == false && hit.Collider != null && hit.Collider.attachedRigidbody != null)
				{
					hit.Collider.attachedRigidbody.AddForce(direction * _hitImpulse, ForceMode.Impulse);
				}
			}

			_data = data;
		}

		public override void Render()
		{
			if (_isDestroyed == true)
				return;

			if (_data.IsDestroyed == true && _isDestroyed == false)
			{
				ShowDestroyEffect();
				return;
			}

			// When object is predictively spawned IsProxy returns true (= no authority assigned)
			// but we want predicted object be treated as object with InputAuthority
			bool isProxy = IsProxy == true && Object.IsPredictedSpawn == false;

			// For proxies we move projectiles in remote time frame, for input/state authority we use local time frame
			float renderTime = isProxy == true ? Runner.InterpolationRenderTime : Runner.SimulationRenderTime;
			float floatTick = renderTime / Runner.DeltaTime;

			var data = _data;

			// It is enough to move the object only in Render.
			// In FUN previous and next position can be calculated from initial parameters.
			transform.position = GetMovePosition(floatTick, ref data);
		}

		// IPredictedSpawnBehaviour INTERFACE

		void IPredictedSpawnBehaviour.PredictedSpawnSpawned()
		{
			Spawned();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnUpdate()
		{
			FixedUpdateNetwork();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnRender()
		{
			Render();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnFailed()
		{
			Despawned(Runner, false);

			// Get rid of the predictively spawned object
			Runner.Despawn(Object, true);
		}

		void IPredictedSpawnBehaviour.PredictedSpawnSuccess()
		{
			// Nothing special is needed
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			if (_hitEffect != null)
			{
				_hitEffect.SetActive(false);
			}
		}

		// PRIVATE METHODS

		private Vector3 GetMovePosition(float currentTick, ref FireData data)
		{
			float time = (currentTick - data.FireTick) * Runner.DeltaTime;

			if (time <= 0f)
				return data.FirePosition;

			return data.FirePosition + data.FireVelocity * time;
		}

		private void ShowDestroyEffect()
		{
			transform.position = _data.HitPosition;

			if (_hitEffect != null)
			{
				_hitEffect.SetActive(true);
			}

			// Hide projectile visual
			_visualsRoot.SetActive(false);
		}
	}
}
