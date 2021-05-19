using Sandbox;

[Library( "dm_crossbow", Title = "Crossbow" )]
partial class Crossbow : BaseDmWeapon
{
	public override string ViewModelPath => "weapons/rust_crossbow/v_rust_crossbow.vmdl";

	public override float PrimaryRate => 1;
	public override int Bucket => 2;
	public override AmmoType AmmoType => AmmoType.Crossbow;

	[Net]
	public bool Zoomed { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		AmmoClip = 400;
		SetModel( "weapons/rust_crossbow/rust_crossbow.vmdl" );
	}

	public override void AttackPrimary()
	{
		if ( !TakeAmmo( 1 ) )
		{
			DryFire();
			return;
		}

		ShootEffects();

		if ( IsServer )
			using ( Prediction.Off() )
			{
				var bolt = new CrossbowBolt();
				bolt.Position = Owner.EyePos;
				bolt.Rotation = Owner.EyeRot;
				bolt.Owner = Owner;
				bolt.Velocity = Owner.EyeRot.Forward * 100;
			}
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		Zoomed = Owner.Input.Down( InputButton.Attack2 );
	}

	public virtual void ModifyCamera( Camera cam )
	{
		if ( Zoomed )
		{
			cam.FieldOfView = 20;
		}
	}

	public override void BuildInput( InputBuilder input )
	{
		if ( Zoomed )
		{
			// maybe just viewangles? dunno
			input.ViewAngles = Angles.Lerp( input.OriginalViewAngles, input.ViewAngles, 0.2f );
		}
	}


	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		if ( Owner == Local.Client )
		{
			new Sandbox.ScreenShake.Perlin( 0.5f, 4.0f, 1.0f, 0.5f );
		}

		ViewModelEntity?.SetAnimParam( "fire", true );
		CrosshairPanel?.OnEvent( "fire" );
	}
}
