using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace balloonparty.entities
{
	[Library( "Balloon Grenade Entity" )]
	partial class BalloonGrenadeEntity : ModelEntity
	{
		[ServerVar( "balloon_grenade_debug" )]
		public static bool Debug { get; set; } = false;
		public int TimeToLive => 2;

		public float Damage => 100f;
		public float ExplosionRadius => 100f;
		public float ExplosionForce => 300f;
		public float GravityScale => 0.5f;
		// Maybe network this ?
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

		public async void Explode( int delay = 0 )
		{
			if ( !IsServer ) return;
			_timerStarted = false;
			_isExploding = true;
			timeAlive = 0;
			if ( delay > 0 )
				await Task.Delay( delay );
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
					if ( entity is Player pl )
					{
						var dmgInfo = DamageInfo.Explosion( Position, (ExplosionForce / 2), Damage );
						dmgInfo.Attacker = Local.Pawn;
						dmgInfo.HitboxIndex = 1;
						dmgInfo.Weapon = this;
						pl.LastAttacker = Local.Pawn;
						pl.Owner.LastAttacker = Local.Pawn;
						pl.ApplyAbsoluteImpulse( (pl.Position - Position).Normal * ExplosionForce );
						pl.TakeDamage( dmgInfo );
					}
					else if ( entity is BalloonGrenadeEntity bl )
					{
						if ( bl._isExploding ) continue;
						bl.Explode();
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

		//public void OnPostPhysicsStep( float dt )
		//{
		//	throw new NotImplementedException();
		//}
	}
}
