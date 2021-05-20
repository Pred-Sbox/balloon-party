using Sandbox;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Library( "Balloon Grenade Entity" )]
partial class BalloonGrenadeEntity : ModelEntity
{
	[ServerVar( "balloon_grenade_debug" )]
	public static bool Debug { get; set; } = false;
	public int TimeToLive => 2;

	public float Damage => 2f;
	public float ExplosionRadius => 100f;
	public float ExplosionForce => 300f;
	public float GravityScale => 0.5f;

	// Maybe network this ?
	private TimeSince timeSince { get; set; }
	[NetPredicted]
	private float timeAlive { get; set; }
	[NetPredicted]
	private bool _isExploding { get; set; }
	[NetPredicted]
	private bool _timerStarted { get; set; }
	private Particles trail { get; set; }

	public bool IsActive => EnableDrawing;
	// List of entities we ignore and entities we explode
	static SoundEvent ExplodeSound = new( "sounds/balloon_pop_cute.vsnd" )
	{
		Volume = 1,
	};

	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/citizen_props/balloonregular01.vmdl" );
		LocalScale = 0.5f;
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		RenderColor = Color.Random.ToColor32();
		Transmit = TransmitType.Always;
		timeSince = new TimeSince();

	}
	public override void OnKilled()
	{
		ResetInterpolation();
		EnableDrawing = false;
		EnableAllCollisions = false;
		_isExploding = false;
		PlaySound( ExplodeSound.Name );
		DisposeTrail();
		if ( !PhysicsBody.IsValid() )
			return;

		ResetVelocity();
		PhysicsBody.GravityScale = 0;



	}

	private void ResetVelocity()
	{
		Velocity = 0;
		PhysicsBody.AngularVelocity = Vector3.Zero;
	}


	// We do not want to destroy this entity if it moves to fast
	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		return;
	}

	public void Explode()
	{
		if ( !IsServer || !EnableDrawing ) return;
		_timerStarted = false;
		_isExploding = true;
		timeAlive = 0;

		if ( Debug )
			DebugOverlay.Sphere( Position, ExplosionRadius, Color.Red, duration: 0.5f );

		DisposeTrail();
		ApplyForceToLocalEntities();
		ApplyForceToEntities();
		SpawnParticles();
		OnKilled();
	}

	private void ApplyForceToEntities()
	{
		if ( !IsServer ) return;
		using ( Prediction.Off() )
		{

			var hitEntities = Physics.GetEntitiesInSphere( Position, ExplosionRadius );
			foreach ( var entity in hitEntities )
			{
				if ( entity is BalloonGrenadeEntity bl )
				{
					if ( !bl.IsActive || bl._isExploding  ) continue;
					bl.Explode();
				}
				else if ( entity is Player player )
				{
					var dmgInfo = DamageInfo.Explosion( Position, 0, Damage ).WithAttacker( Owner ).WithWeapon( this );

					dmgInfo.HitboxIndex = 1;
					player.ApplyAbsoluteImpulse( (player.Position - Position).Normal * (ExplosionForce / 4) );
					player.TakeDamage( dmgInfo );
					var controller = player.Controller as WalkControllerBP;
					controller.AttachBalloons();
					AttachBalloon( player, RenderColor );
				}
				else if ( entity is Prop prop )
				{
					var direction = prop.Position - Position;
					prop.ApplyAbsoluteImpulse( direction.Normal * ExplosionForce );
				}
			}
		}
	}

	[ClientRpc]
	private void ApplyForceToLocalEntities()
	{
		Host.AssertClient();
		var hitEntities = Physics.GetEntitiesInSphere( Position, ExplosionRadius );
		foreach ( var entity in hitEntities )
		{
			if ( entity is ModelEntity me )
			{
				if ( me.PhysicsGroup != null )
				{
					var direction = me.PhysicsGroup.Pos - Position;
					me.PhysicsGroup.AddVelocity( direction.Normal * (ExplosionForce / 2) );
				}
			}
			else if ( entity is Prop prop )
			{
				var direction = prop.Position - Position;
				prop.ApplyAbsoluteImpulse( direction.Normal * ExplosionForce );
			}
		}
	}





	[ClientRpc]
	private void SpawnParticles()
	{
		Host.AssertClient();
		using ( Prediction.Off() )
		{
			var explodeParticle = Particles.Create( "particles/confetti_burst.vpcf" );
			var pos = Position;
			explodeParticle.SetPos( 0, pos );
			explodeParticle.Destroy( false );
		}
	}

	[ClientRpc]
	public void AttachTrail()
	{
		Host.AssertClient();
		using ( Prediction.Off() )
		{
			trail = Particles.Create( "particles/status_confetti.vpcf" );
			trail.SetEntity( 0, this );
		}
	}

	[ClientRpc]
	private void DisposeTrail()
	{
		Host.AssertClient();
		using ( Prediction.Off() )
		{
			if ( trail != null )
			{
				trail.Destroy( true );
				trail.Dispose();
				trail = null;
			}
		}
	}

	public void StartDestroy()
	{
		if ( !IsServer )
			return;
		_timerStarted = true;
		timeSince = 0;
		timeAlive = timeSince.Relative;
	}

	[Event( "server.tick" )]
	public void OnServerTick()
	{
		if ( _timerStarted && timeSince.Relative >= timeAlive + TimeToLive )
		{
			Explode();
		}
	}


	// TODO: Remove on player death
	private void AttachBalloon( Player player, Color32 color )
	{
		if ( !IsServer ) return;

		using ( Prediction.Off() )
		{
			var newZ = player.PhysicsBody.GetBounds().Maxs.z + 1;
			var pos = player.Position.WithZ( newZ );
			var ent = new BalloonEntity
			{
				Position = pos
			};
			ent.SetModel( "models/citizen_props/balloonregular01.vmdl" );
			ent.PhysicsBody.GravityScale = 0;
			ent.RenderColor = color;
			ent.attachedTo = player.Controller as WalkControllerBP;

			var rope = Particles.Create( "particles/rope.vpcf" );
			rope.SetEntity( 0, ent );
			var attachLocalPos = player.PhysicsBody.Transform.PointToLocal( pos );
			// TODO: WHy do I have 2 ? 
			rope.SetEntityBone( 1, player, -1, new Transform( attachLocalPos ) );
			rope.SetEntityAttachment( 1, player, "hat" );
			ent.AttachRope = rope;
			
			ent.AttachJoint = PhysicsJoint.Spring
				.From( ent.PhysicsBody )
				.To( player.PhysicsBody )
				.WithFrequency( 5.0f )
				.WithPivot( player.Position )
				.WithDampingRatio( 0.7f )
				.WithReferenceMass( 0 )
				.WithMinRestLength( 0 )
				.WithMaxRestLength( 100 )
				.WithCollisionsEnabled()
				.Create();
			ent.Owner = player;

		}
	}

	//public void OnPostPhysicsStep( float dt )
	//{
	//	throw new NotImplementedException();
	//}
}
