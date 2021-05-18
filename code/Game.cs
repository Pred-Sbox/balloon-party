using balloonparty;
using Sandbox;

/// <summary>
/// This is the heart of the gamemode. It's responsible
/// for creating the player and stuff.
/// </summary>
[Library( "balloon_party", Title = "Balloon Party" )]
partial class BalloonPartyGame : Game
{
	public BalloonPartyGame()
	{
		//
		// Create the HUD entity. This is always broadcast to all clients
		// and will create the UI panels clientside. It's accessible 
		// globally via Hud.Current, so we don't need to store it.
		//
		if ( IsServer )
		{
			new DeathmatchHud();
		}
	}

	public override void ClientJoined( Client cl )
	{
		base.ClientJoined( cl );
		var player = new BalloonPartyPlayer();
		player.Respawn();

		cl.Pawn = player;
	}

	/// <summary>
	/// Called when a player joins and wants a player entity. We create
	/// our own class so we can control what happens.
	/// </summary>

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		ItemRespawn.Init();
	}



	/// <summary>
	/// Called when a player has died, or been killed
	/// </summary>
		public override void OnKilled( Client client, Entity pawn )
		{
			Host.AssertServer();

			Log.Info( $"{client.Name} was killed" );

			if ( pawn.LastAttacker != null )
			{
				var attackerClient = pawn.LastAttacker.GetClientOwner();

				if ( attackerClient != null )
				{
					OnKilledMessage( attackerClient.SteamId, attackerClient.Name, client.SteamId, client.Name, pawn.LastAttackerWeapon?.ClassInfo?.Name );
				}
				else
				{
					OnKilledMessage( (ulong)pawn.LastAttacker.NetworkIdent, pawn.LastAttacker.ToString(), client.SteamId, client.Name, "killed" );
				}
			}
			else
			{
				OnKilledMessage( 0, "", client.SteamId, client.Name, "died" );
			}
		}
}
