using Fusion;
using UnityEngine;

namespace Projectiles.NetworkObjectExample
{
	public class Weapon_NetworkObject : WeaponBase
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private PhysicsProjectile _projectilePrefab;

		[Networked]
		private int _fireCount { get; set; }

		private int _visibleFireCount;

		// WeaponBase INTERFACE

		public override void Fire()
		{
			// Spawn the projectile
			// Since prediction key argument is not provided, projectile will be spawned
			// only on state authority. For predictive spawning see Example 02 - Predicted.
			// Check for authority is needed because without prediction key the Spawn method will
			// return null on input authority.
			if (HasStateAuthority == true)
			{
				var projectile = Runner.Spawn(_projectilePrefab, FireTransform.position, FireTransform.rotation, Object.InputAuthority);
				projectile.Fire();
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
	}
}
