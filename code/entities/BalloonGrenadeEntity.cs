using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace balloonparty.entities
{
	partial class BalloonGrenadeEntity : ModelEntity
	{
		[ServerVar( "balloon_grenade_debug" )]
		public static bool Debug { get; set; } = false;
		public int TimeToLive => 2;

		public float Damage => 100f;
		public float ExplosionRadius => 100f;
		public float ExplosionForce => 300f;
		public float GravityScale => 0.5f;
		[Net]
		private TimeSince timeSince { get; set; }
		[Net]
		private float timeAlive { get; set; }
		[Net]
		private bool _isExploding { get; set; }
		[Net]
		private bool _timerStarted { get; set; }
		private Particles trail { get; set; }
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
			// Maybe this makes things worse? dunno
			Transmit = TransmitType.Always;
			timeSince = new TimeSince();

		}
		public override void OnKilled()
		{
			if ( !PhysicsBody.IsValid() )
				return;
			// TODO: FREEZE PROP IN PLACE WHEN DESPAWNING

			EnableDrawing = false;
			EnableAllCollisions = false;
			ResetVelocity();

			PhysicsBody.GravityScale = 0;
			_isExploding = false;
			PlaySound( ExplodeSound.Name );
			DisposeTrail();
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

		public async void Explode( int delay = 0 )
		{
			if ( !IsServer ) return;
			_isExploding = true;
			timeAlive = 0;
			if ( delay > 0 )
				await Task.Delay( delay );
			if ( Debug )
				DebugOverlay.Sphere( WorldPos, ExplosionRadius, Color.Red, duration: 0.5f );

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

				var hitEntities = Physics.GetEntitiesInSphere( WorldPos, ExplosionRadius );
				foreach ( var entity in hitEntities )
				{
					if ( entity is Player pl )
					{
						var dmgInfo = DamageInfo.Explosion( WorldPos, (ExplosionForce / 2), Damage );
						dmgInfo.Attacker = Owner;
						dmgInfo.HitboxIndex = 1;
						pl.ApplyAbsoluteImpulse( (pl.WorldPos - WorldPos).Normal * ExplosionForce );
						pl.TakeDamage( dmgInfo );
					}
					else if ( entity is BalloonGrenadeEntity bl )
					{
						if (bl._isExploding ) continue;
						bl.Explode();
					}
					else if ( entity is Prop prop )
					{
						var direction = prop.WorldPos - WorldPos;
						prop.ApplyAbsoluteImpulse( direction.Normal * ExplosionForce );
					}
				}
			}
		}

		[ClientRpc]
		private void ApplyForceToLocalEntities()
		{
			Host.AssertClient();
			var hitEntities = Physics.GetEntitiesInSphere( WorldPos, ExplosionRadius );
			foreach ( var entity in hitEntities )
			{
				if ( entity is ModelEntity me )
				{
					if ( me.PhysicsGroup != null )
					{
						var direction = me.PhysicsGroup.Pos - WorldPos;
						me.PhysicsGroup.AddVelocity( direction.Normal * (ExplosionForce / 2) );
					}
				}
				else if ( entity is Prop prop )
				{
					var direction = prop.WorldPos - WorldPos;
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
				// TODO: Object pool particles
				var explodeParticle = Particles.Create( "particles/confetti_burst.vpcf" );
				var pos = WorldPos;
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
			timeAlive = timeSince.Relative;
		}

		[Event( "server.tick" )]
		public void OnServerTick()
		{
			if ( _timerStarted && timeSince.Relative >= timeAlive + TimeToLive )
			{
				_timerStarted = false;
				Explode();
			}
		}

		//public void OnPostPhysicsStep( float dt )
		//{
		//	throw new NotImplementedException();
		//}
	}
}
