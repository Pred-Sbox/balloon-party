using Sandbox;


partial class BalloonEntity : Prop, IPhysicsUpdate
{
	static SoundEvent PopSound = new( "sounds/balloon_pop_cute.vsnd" )
	{
		Volume = 1,
		DistanceMax = 500.0f
	};

	public PhysicsJoint AttachJoint;
	public Particles AttachRope;
	public WalkControllerBP attachedTo;
	// The height at which the balloons will be destroyed
	private float MaxZHeight => 2200f;
	private static float GravityScale => -1;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen_props/balloonregular01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		PhysicsBody.GravityScale = GravityScale;
		PhysicsBody.AngularDrag = 200;
		PhysicsBody.LinearDrag = 200;
		RenderColor = Color.Random.ToColor32();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( AttachJoint.IsValid() )
		{
			AttachJoint.Remove();
		}

		if ( AttachRope != null )
		{
			AttachRope.Destroy( true );
		}
	}

	public override void OnKilled()
	{
		attachedTo.RemoveBalloons();
		base.OnKilled();
		PlaySound( PopSound.Name );

	}

	public void OnPostPhysicsStep( float dt )
	{
		if ( !this.IsValid() )
			return;

		var body = PhysicsBody;
		if ( !body.IsValid() )
			return;
		if(Position.z > MaxZHeight )
			TakeDamage(DamageInfo.Generic(100));

		body.GravityScale = GravityScale;
	}
}
