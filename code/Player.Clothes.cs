using Sandbox;
using System;
using System.Linq;
public class ClothingEntity : ModelEntity
{

}

partial class DeathmatchPlayer
{
	ModelEntity shoes;
	ModelEntity hat;
	bool dressed = false;

	/// <summary>
	/// Bit of a hack to putr random clothes on the player
	/// </summary>
	public void Dress()
	{
		if ( dressed )
			return;
		dressed = true;

		shoes = new ClothingEntity();
		shoes.SetModel( "models/citizen_clothes/shoes/shoes.workboots.vmdl" );
		shoes.SetParent( this, true );
		shoes.EnableShadowInFirstPerson = true;
		shoes.EnableHideInFirstPerson = true;


		hat = new ClothingEntity();
		hat.SetModel( "models/person_clothes/hat/party_hat.vmdl" );
		hat.SetParent( this, "head" );
		hat.EnableShadowInFirstPerson = true;
		hat.EnableHideInFirstPerson = true;
		//hat.RemoveCollisionLayer( CollisionLayer.Player );
		//hat.EnableAllCollisions = false;
		//hat.EnableDrawing = true;
	}


	[ClientRpc]
	protected void CreatePhysicsAttachmentsOnClient( Vector3 force )
	{
		Host.AssertClient();

		var ent = new Prop();
		ent.Position = Position + new Vector3( 0, 0, 10 );
		ent.Rotation = Rotation;
		ent.SetModel( "models/person_clothes/hat/party_hat.vmdl" );
		ent.ApplyAbsoluteImpulse( force );
		//ent.EnableDrawing = true;
		ent.DeleteAsync( 20 );
	}
}
