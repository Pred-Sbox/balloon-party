using balloonparty.entities;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace balloonparty.utils
{
	class EntityPooler<T> where T : Entity, new()
	{
		private int _initialAmount = 5;
		private List<T> pooledObjects;

		public EntityPooler( int initialAmount = 5 )
		{
			_initialAmount = initialAmount;
			pooledObjects = new List<T>();
			for ( var i = 0; i < _initialAmount; i++ )
			{
				var t = new T();
				// Set the models as "inactive"
				if ( t is ModelEntity me )
				{
					me.EnableDrawing = false;
					me.EnableAllCollisions = false;
					me.Velocity = 0;
					me.PhysicsBody.AngularVelocity = Vector3.Zero;
					me.PhysicsBody.GravityScale = 0;
				}
				pooledObjects.Add( t );
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
			var ent = new T();
			pooledObjects.Add( ent );
			return ent;

		}
	}
}
