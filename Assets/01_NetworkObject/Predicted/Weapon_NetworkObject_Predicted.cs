using Fusion;
using UnityEngine;

namespace Projectiles.NetworkObjectExample
{
	public class Weapon_NetworkObject_Predicted : WeaponBase
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private PhysicsProjectile_Predicted _projectilePrefab;

		[Networked]
		private int _fireCount { get; set; }
		[Networked, Capacity(4)]
		private NetworkArray<PhysicsProjectile_Predicted> _projectilesPool { get; }

		private int _visibleFireCount;

		// WeaponBase INTERFACE

		public override void Fire()
		{
			FireProjectile();

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

			// Prepare projectiles pool
			// Due to intricate physics interactions between predictively spawned objects (e.g. projectile dummies)
			// and already registered networked objects in the scene it is recommended to spawn small amount of physics projectiles
			// in advance and hide it until the projectile is fired instead of using spawn prediction. For proper spawn prediction
			// example check Example 2 - Predicted.
			for (int i = 0; i < _projectilesPool.Length; i++)
			{
				PrepareProjectile(i);
			}
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

		private void FireProjectile()
		{
			int index = _fireCount % _projectilesPool.Length;
			var projectile = _projectilesPool.Get(index);

			if (projectile != null && projectile.Object != null)
			{
				projectile.Fire(FireTransform.position, FireTransform.rotation);
			}

			// Replace projectile with a new one
			PrepareProjectile(index);
		}

		private void PrepareProjectile(int index)
		{
			var projectile = Runner.Spawn(_projectilePrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority);
			_projectilesPool.Set(index, projectile);
		}
	}
}
