using Sandbox;
using System;
using System.Linq;

partial class DeathmatchPlayer
{
	protected ModelEntity hat;
	protected bool dressed { get; set; } = false;

	/// <summary>
	/// Bit of a hack to putr random clothes on the player
	/// </summary>
	public void Dress()
	{
		if ( dressed )
			return;
		dressed = true;

		hat = new ModelEntity();
		hat.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		hat.SetModel( "models/person_clothes/hat/party_hat.vmdl" );
		hat.SetParent( this, "head" );
		// Do I need all these? probably not
		hat.EnableShadowInFirstPerson = true;
		hat.EnableHideInFirstPerson = true;
		hat.RemoveCollisionLayer( CollisionLayer.Player );
		hat.EnableAllCollisions = false;
		hat.EnableDrawing = true;
		hat.Owner = this;
	}


	[ClientRpc]
	protected void CreatePhysicsAttachmentsOnClient( Vector3 force )
	{
		Host.AssertClient();

		var ent = new Prop();
		ent.WorldPos = WorldPos + new Vector3( 0, 0, 10 );
		ent.WorldRot = WorldRot;
		ent.SetModel( "models/person_clothes/hat/party_hat.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
		ent.ApplyAbsoluteImpulse( force );
		//ent.EnableDrawing = true;
		ent.DeleteAsync( 20 );

	}
}
