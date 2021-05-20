using Sandbox;
using System.Collections.Generic;

class ModelEntityPooler<T> where T : ModelEntity
{
	private int _initialAmount = 5;
	private List<T> pooledObjects;

	public ModelEntityPooler( int initialAmount = 5 )
	{
		_initialAmount = initialAmount;
		pooledObjects = new List<T>();
		for ( var i = 0; i < _initialAmount; i++ )
		{
			var me = Library.Create<T>(); 
			// Set the models as "inactive"

			me.EnableDrawing = false;
			me.EnableAllCollisions = false;
			me.Velocity = 0;
			me.PhysicsBody.AngularVelocity = Vector3.Zero;
			me.PhysicsBody.GravityScale = 0;

			pooledObjects.Add( me );
		}
	}

	public T GetPooledObject()
	{
		for ( var i = 0; i < pooledObjects.Count; i++ )
		{
			var current = pooledObjects[i];
			if ( !current.EnableDrawing )
			{
				return pooledObjects[i];
			}
		}
		var ent = Library.Create<T>();
		
		pooledObjects.Add( ent );
		return ent;
	}
}
