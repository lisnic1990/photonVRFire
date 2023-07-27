using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace Projectiles
{
	[RequireComponent(typeof(NetworkRunner))]
	public sealed class SimpleGameManager : SimulationBehaviour, IPlayerJoined, IPlayerLeft
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private NetworkObject _playerPrefab;
		[SerializeField]
		private Vector3 _playerOffset = new Vector3(2f, 0f, 0f);
		[SerializeField]
		private Vector3 _playerRotation = new Vector3(90f, 0f, 0f);

		private Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>(32);

		// IPlayerJoined INTERFACE

		void IPlayerJoined.PlayerJoined(PlayerRef playerRef)
		{
			if (Runner.IsServer == false)
				return;

			var position = _players.Count * _playerOffset;
			var rotation = Quaternion.Euler(_playerRotation);

			var player = Runner.Spawn(_playerPrefab, position, rotation, inputAuthority: playerRef);

			_players.Add(playerRef, player);

			Runner.SetPlayerObject(playerRef, player);
		}

		// IPlayerLeft INTERFACE

		void IPlayerLeft.PlayerLeft(PlayerRef playerRef)
		{
			if (Runner.IsServer == false)
				return;

			if (_players.TryGetValue(playerRef, out NetworkObject player) == false)
				return;

			Runner.Despawn(player);
			_players.Remove(playerRef);
		}
	}
}
