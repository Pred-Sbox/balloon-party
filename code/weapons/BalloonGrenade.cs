using Sandbox;

[Library( "dm_balloon_grenade", Title = "Balloon Grenade" )]
partial class BalloonGrenade : BaseDmWeapon
{


	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
	public override float PrimaryRate => 15f;
	public override float SecondaryRate => 15f;
	static readonly SoundEvent AttackSound = new SoundEvent( "sounds/swoosh01.vsnd" );
	public override int Bucket => 0;
	public override int ClipSize => 10;


	public TimeSince TimeSinceDischarge { get; set; }
	private float _primaryForce => 1000f;
	private float _secondaryForce => 400f;
	public override void Spawn()
	{
		base.Spawn();
		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		AmmoClip = 1;
	}


	public override bool CanPrimaryAttack()
	{
		if ( !Owner.Input.Down( InputButton.Attack1 ) || Owner.Health <= 0 )
			return false;

		return base.CanPrimaryAttack();
	}

	public override bool CanSecondaryAttack()
	{
		if ( !Owner.Input.Down( InputButton.Attack2 ) || Owner.Health <= 0 )
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
		TimeSincePrimaryAttack = -0.5f;
		TimeSinceSecondaryAttack = -0.5f;

		Shoot( Owner.EyePos, Owner.EyeRot.Forward, _primaryForce );
	}

	public override void AttackSecondary()
	{
		TimeSincePrimaryAttack = -0.5f;
		TimeSinceSecondaryAttack = -0.5f;

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

			var ent = ((BalloonPartyPawn)Owner).grenadePooler.GetPooledObject();
			using ( Prediction.Off() )
			{
				ent.ResetInterpolation();
				ent.Position = Owner.EyePos + (Owner.EyeRot.Forward * 50);
				ent.Rotation = Owner.EyeRot;
				ent.Velocity = Owner.EyeRot.Forward * force;
				ent.PhysicsBody.GravityScale = ent.GravityScale;
				ent.AttachTrail();
				ent.RenderColor = Color.Random;
				ent.EnableAllCollisions = true;
				ent.EnableDrawing = true;
				// TODO: Maybe owenr should be Owner?
				ent.Owner = Local.Pawn;
				ent.StartDestroy();
			}

		}
	}



	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		return;
	}

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

		if ( Owner == Local.Client )
		{
			new Sandbox.ScreenShake.Perlin( 0.5f, 2.0f, 0.5f );
		}
	}
}
