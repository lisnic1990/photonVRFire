using Fusion;
using UnityEngine;

namespace Projectiles.NetworkObjectFireData
{
	public class Weapon_NetworkObjectFireData_Predicted : WeaponBase
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private NetworkObject _projectilePrefab;
		[SerializeField]
		private bool _useSpawnPrediction = true;

		[Networked]
		private int _fireCount { get; set; }

		private int _visibleFireCount;

		// WeaponBase INTERFACE

		public override void Fire()
		{
			// For testing purposes this weapon supports predicted spawning that can be turned on/off
			// (see more about spawning and predicted spawning in Spawning page in the Fusion Manual)
			if (HasStateAuthority == true || (HasInputAuthority == true && _useSpawnPrediction == true))
			{
				SpawnProjectile();
			}

			// Increase networked property fire count to know on all
			// clients that fire effects should be played
			_fireCount++;
		}

		public override void Spawned()
		{
			// In case of late join (and other scenarios) this object can be spawned
			// with fire count larger than zero. To prevent unwanted fire effects triggered in Render method
			// we consider all fire that happened before the Spawn as already visible.
			_visibleFireCount = _fireCount;
		}

		public override void Render()
		{
			if (_visibleFireCount < _fireCount)
			{
				PlayFireEffect();
			}

			_visibleFireCount = _fireCount;
		}

		// PRIVATE METHODS

		private void SpawnProjectile()
		{
			// Prediction key is only needed when predicting spawn, otherwise can be omitted
			NetworkObjectPredictionKey? key = null;

			if (_useSpawnPrediction == true)
			{
				// Prediction key is a unique identifier that is used to bind
				// temporary local object with the actual spawned one
				key = new NetworkObjectPredictionKey()
				{
					// Low number part is enough
					Byte0 = (byte)Runner.Tick,

					// Object.InputAuthority can be 0 for certain player so add 1
					// to not end up with invalid key when Byte0 is zero
					Byte1 = (byte)(Object.InputAuthority + 1),
				};
			}

			// Spawn the projectile
			Runner.Spawn(_projectilePrefab, FireTransform.position, FireTransform.rotation, Object.InputAuthority, BeforeProjectileSpawned, key);

			// Fire needs to be called before Spawn because in OnSpawned method of projectile
			// we are already reading networked property (FireVelocity)
			void BeforeProjectileSpawned(NetworkRunner runner, NetworkObject spawnedObject)
			{
				var projectile = spawnedObject.GetComponent<FireDataProjectile_Predicted>();
				projectile.Fire(FireTransform.position, FireTransform.forward);
			}
		}
	}
}
