using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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

		body.GravityScale = GravityScale;
	}
}
