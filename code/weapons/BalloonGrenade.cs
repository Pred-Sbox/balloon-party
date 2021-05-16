using balloonparty.entities;
using balloonparty.utils;
using Sandbox;
using System.IO;

namespace balloonparty.weapons
{
	[Library( "BalloonGrenade", Title = "Balloon Grenade" )]
	partial class BalloonGrenade : BaseDmWeapon
	{


		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
		public override float PrimaryRate => 15f;
		public override float SecondaryRate => 15f;
		static readonly SoundEvent AttackSound = new SoundEvent( "sounds/swoosh01.vsnd" );
		
		public TimeSince TimeSinceDischarge { get; set; }
		private float _primaryForce => 1000f;
		private float _secondaryForce => 400f;
		// TODO: Move this out. Don't want to create a new entity pooler for each WEAPOn..
		public override void Spawn()
		{
			base.Spawn();
			SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		}


		public override bool CanPrimaryAttack()
		{
			if ( !Owner.Input.Pressed( InputButton.Attack1 ) )
				return false;

			return base.CanPrimaryAttack();
		}

		public override bool CanSecondaryAttack()
		{
			if ( !Owner.Input.Pressed( InputButton.Attack2 ) )
				return false;
			return base.CanSecondaryAttack();
		}

		public override void Reload()
		{
			base.Reload();

			ViewModelEntity?.SetAnimParam( "reload", true );
		}

		public override void AttackPrimary()
		{
			TimeSincePrimaryAttack = -1;
			TimeSinceSecondaryAttack = -1;

			Shoot( Owner.EyePos, Owner.EyeRot.Forward, _primaryForce );
		}

		public override void AttackSecondary()
		{
			TimeSincePrimaryAttack = -1;
			TimeSinceSecondaryAttack = -1;

			Shoot( Owner.EyePos, Owner.EyeRot.Forward, _secondaryForce );
		}

		private void Shoot( Vector3 pos, Vector3 dir, Vector3 force )
		{
			//
			// Tell the clients to play the shoot effects
			//
			ShootEffects();

			if ( IsServer )
			{

				var ent = ((BalloonPartyPlayer)Owner).grenadePooler.GetPooledObject();
				using ( Prediction.Off() )
				{
					ent.WorldPos = Owner.EyePos + Owner.EyeRot.Forward * 50;
					ent.WorldRot = Owner.EyeRot;
					ent.AttachTrail();
					ent.RenderColor = Color.Random;
				}
				ent.PhysicsBody.GravityScale = ent.GravityScale;
				ent.EnableAllCollisions = true;
				ent.EnableDrawing = true;
				ent.Velocity = Owner.EyeRot.Forward * force;
				ent.Owner = Owner;

				ent.StartDestroy();
			}

			//
			// ShootBullet is coded in a way where we can have bullets pass through shit
			// or bounce off shit, in which case it'll return multiple results
			//
			//foreach ( var tr in TraceBullet( pos, pos + dir * 4000 ) )
			//{
			//	tr.Surface.DoBulletImpact( tr );

			//	if ( !IsServer ) continue;
			//	if ( !tr.Entity.IsValid() ) continue;

			//	//
			//	// We turn predictiuon off for this, so aany exploding effects
			//	//
			//	using ( Prediction.Off() )
			//	{
			//		var damage = DamageInfo.FromBullet( tr.EndPos, forward.Normal * 1000, 100 )
			//			.UsingTraceResult( tr )
			//			.WithAttacker( Owner )
			//			.WithWeapon( this );

			//		tr.Entity.TakeDamage( damage );
			//	}
			//}
		}



		protected override void OnPhysicsCollision( CollisionEventData eventData )
		{
			return;
		}

		
		public override void OnPlayerControlTick( Player owner )
		{
			base.OnPlayerControlTick( owner );

			//DebugTrace( owner );

			//if ( !NavMesh.IsLoaded )
			//	return;

			//var forward = owner.EyeRot.Forward * 2000;


			//var tr = Trace.Ray( owner.EyePos, owner.EyePos + forward )
			//				.Ignore( owner )
			//				.Run();

			//var closestPoint = NavMesh.GetClosestPoint( tr.EndPos );

			//DebugOverlay.Line( tr.EndPos, closestPoint, 0.1f );

			//DebugOverlay.Axis( closestPoint, Rotation.LookAt( tr.Normal ), 2.0f, Time.Delta * 2 );
			//DebugOverlay.Text( closestPoint, $"CLOSEST Walkable POINT", Time.Delta * 2 );

			//NavMesh.BuildPath( Owner.WorldPos, closestPoint );
		}

		//public void DebugTrace( Player player )
		//{
		//	for ( float x = -10; x < 10; x += 1.0f )
		//		for ( float y = -10; y < 10; y += 1.0f )
		//		{
		//			var tr = Trace.Ray( player.EyePos, player.EyePos + player.EyeRot.Forward * 4096 + player.EyeRot.Left * (x + Rand.Float( -1.6f, 1.6f )) * 100 + player.EyeRot.Up * (y + Rand.Float( -1.6f, 1.6f )) * 100 ).Ignore( player ).Run();

		//			if ( IsServer ) DebugOverlay.Line( tr.EndPos, tr.EndPos + tr.Normal, Color.Cyan, duration: 20 );
		//			else DebugOverlay.Line( tr.EndPos, tr.EndPos + tr.Normal, Color.Yellow, duration: 20 );
		//		}
		//}

		[ClientRpc]
		protected override void ShootEffects()
		{
			Host.AssertClient();

			var muzzle = EffectEntity.GetAttachment( "muzzle" );

			//bool InWater = Physics.TestPointContents( muzzle.Pos, CollisionLayer.Water );
			Sound.FromEntity( AttackSound.Name, this );

			Particles.Create( "particles/balloon_grenade_launcher_muzzle.vpcf", EffectEntity, "muzzle" );
			ViewModelEntity?.SetAnimParam( "fire", true );
			CrosshairPanel?.OnEvent( "onattack" );

			if ( Owner == Player.Local )
			{
				new Sandbox.ScreenShake.Perlin( 0.5f, 2.0f, 0.5f );
			}
		}
	}
}
