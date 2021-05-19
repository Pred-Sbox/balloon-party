using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Library]
class WalkControllerBP : WalkController
{
	private int _attachedBalloons { get; set; } = 0;
	public int AttachedBalloons => _attachedBalloons;
	public float BalloonVelocity => 30f;


	bool IsTouchingLadder = false;
	Vector3 LadderNormal;

	public override void Simulate()
	{
		EyePosLocal = Vector3.Up * (EyeHeight * Pawn.Scale);
		UpdateBBox();

		EyePosLocal += TraceOffset;
		EyeRot = Input.Rotation;

		RestoreGroundPos();

		//Velocity += BaseVelocity * ( 1 + Time.Delta * 0.5f );
		//BaseVelocity = Vector3.Zero;

		//Rot = Rotation.LookAt( Input.Rotation.Forward.WithZ( 0 ), Vector3.Up );

		if ( Unstuck.TestAndFix() )
			return;

		// Check Stuck
		// Unstuck - or return if stuck

		// Set Ground Entity to null if  falling faster then 250

		// store water level to compare later

		// if not on ground, store fall velocity

		// player->UpdateStepSound( player->m_pSurfaceData, mv->GetAbsOrigin(), mv->m_vecVelocity )


		// RunLadderMode

		CheckLadder();
		Swimming = Pawn.WaterLevel.Fraction > 0.6f;

		//
		// Start Gravity
		//
		if ( !Swimming && !IsTouchingLadder )
		{
			Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;

			BaseVelocity = BaseVelocity.WithZ( 0 );
		}


		/*
		 if (player->m_flWaterJumpTime)
			{
				WaterJump();
				TryPlayerMove();
				// See if we are still in water?
				CheckWater();
				return;
			}
		*/

		// if ( underwater ) do underwater movement

		if ( AutoJump ? Input.Down( InputButton.Jump ) : Input.Pressed( InputButton.Jump ) )
		{
			CheckJumpButton();
		}

		// Fricion is handled before we add in any base velocity. That way, if we are on a conveyor, 
		//  we don't slow when standing still, relative to the conveyor.
		bool bStartOnGround = GroundEntity != null;
		//bool bDropSound = false;
		if ( bStartOnGround )
		{
			//if ( Velocity.z < FallSoundZ ) bDropSound = true;

			Velocity = Velocity.WithZ( 0 );
			//player->m_Local.m_flFallVelocity = 0.0f;

			if ( GroundEntity != null )
			{
				ApplyFriction( GroundFriction * SurfaceFriction );
			}
		}

		//
		// Work out wish velocity.. just take input, rotate it to view, clamp to -1, 1
		//
		WishVelocity = new Vector3( Input.Forward, Input.Left, 0 );
		var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
		WishVelocity *= Input.Rotation;

		if ( !Swimming && !IsTouchingLadder )
		{
			WishVelocity = WishVelocity.WithZ( 0 );
		}

		WishVelocity = WishVelocity.Normal * inSpeed;
		WishVelocity *= GetWishSpeed();

		Duck.PreTick();

		bool bStayOnGround = false;

		if ( AttachedBalloons > 0 )
		{
			ClearGroundEntity();
			Velocity = Velocity.WithZ( 1 * AttachedBalloons * BalloonVelocity );
			AirMove();
		}
		else if ( Swimming )
		{
			ApplyFriction( 1 );
			WaterMove();
		}
		else if ( IsTouchingLadder )
		{
			LadderMove();
		}
		else if ( GroundEntity != null )
		{
			bStayOnGround = true;
			WalkMove();
		}
		else
		{
			AirMove();
		}

		CategorizePosition( bStayOnGround );

		// FinishGravity
		if ( !Swimming && !IsTouchingLadder )
		{
			Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
		}


		if ( GroundEntity != null )
		{
			Velocity = Velocity.WithZ( 0 );
		}

		// CheckFalling(); // fall damage etc

		// Land Sound
		// Swim Sounds

		SaveGroundPos();

		if ( Debug )
		{
			DebugOverlay.Box( Position + TraceOffset, mins, maxs, Color.Red );
			DebugOverlay.Box( Position, mins, maxs, Color.Blue );

			var lineOffset = 0;
			if ( Host.IsServer ) lineOffset = 10;

			DebugOverlay.ScreenText( lineOffset + 0, $"        Position: {Position}" );
			DebugOverlay.ScreenText( lineOffset + 1, $"        Velocity: {Velocity}" );
			DebugOverlay.ScreenText( lineOffset + 2, $"    BaseVelocity: {BaseVelocity}" );
			DebugOverlay.ScreenText( lineOffset + 3, $"    GroundEntity: {GroundEntity} [{GroundEntity?.Velocity}]" );
			DebugOverlay.ScreenText( lineOffset + 4, $" SurfaceFriction: {SurfaceFriction}" );
			DebugOverlay.ScreenText( lineOffset + 5, $"    WishVelocity: {WishVelocity}" );
		}

	}

	public void AttachBalloons( int amount = 1 )
	{
		_attachedBalloons += amount;
	}

	public void RemoveBalloons( int amount = 1 )
	{
		var newCount = _attachedBalloons - amount;
		_attachedBalloons = newCount < 0 ? 0 : newCount;
	}
	void WalkMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * wishspeed;

		Velocity = Velocity.WithZ( 0 );
		Accelerate( wishdir, wishspeed, 0, Acceleration );
		Velocity = Velocity.WithZ( 0 );

		//   Player.SetAnimParam( "forward", Input.Forward );
		//   Player.SetAnimParam( "sideward", Input.Right );
		//   Player.SetAnimParam( "wishspeed", wishspeed );
		//    Player.SetAnimParam( "walkspeed_scale", 2.0f / 190.0f );
		//   Player.SetAnimParam( "runspeed_scale", 2.0f / 320.0f );

		//  DebugOverlay.Text( 0, Pos + Vector3.Up * 100, $"forward: {Input.Forward}\nsideward: {Input.Right}" );

		// Add in any base velocity to the current velocity.
		Velocity += BaseVelocity;

		try
		{
			if ( Velocity.Length < 1.0f )
			{
				Velocity = Vector3.Zero;
				return;
			}

			// first try just moving to the destination	
			var dest = (Position + Velocity * Time.Delta).WithZ( Position.z );

			var pm = TraceBBox( Position, dest );

			if ( pm.Fraction == 1 )
			{
				Position = pm.EndPos;
				StayOnGround();
				return;
			}

			StepMove();
		}
		finally
		{

			// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
			Velocity -= BaseVelocity;
		}

		StayOnGround();
	}

	private void StepMove()
	{
		var startPos = Position;
		var startVel = Velocity;

		//
		// First try walking straight to where they want to go.
		//
		TryPlayerMove();

		//
		// mv now contains where they ended up if they tried to walk straight there.
		// Save those results for use later.
		//	
		var withoutStepPos = Position;
		var withoutStepVel = Velocity;

		//
		// Try again, this time step up and move across
		//
		Position = startPos;
		Velocity = startVel;
		var trace = TraceBBox( Position, Position + Vector3.Up * (StepSize + DistEpsilon) );
		if ( !trace.StartedSolid ) Position = trace.EndPos;
		TryPlayerMove();

		//
		// If we move down from here, did we land on ground?
		//
		trace = TraceBBox( Position, Position + Vector3.Down * (StepSize + DistEpsilon * 2) );
		if ( !trace.Hit || Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle )
		{
			// didn't step on ground, so just use the original attempt without stepping
			Position = withoutStepPos;
			Velocity = withoutStepVel;
			return;
		}


		if ( !trace.StartedSolid )
			Position = trace.EndPos;

		var withStepPos = Position;

		float withoutStep = (withoutStepPos - startPos).WithZ( 0 ).Length;
		float withStep = (withStepPos - startPos).WithZ( 0 ).Length;

		//
		// We went further without the step, so lets use that
		//
		if ( withoutStep > withStep )
		{
			Position = withoutStepPos;
			Velocity = withoutStepVel;
			return;
		}
	}
	void CheckJumpButton()
	{
		//if ( !player->CanJump() )
		//    return false;


		/*
		if ( player->m_flWaterJumpTime )
		{
			player->m_flWaterJumpTime -= gpGlobals->frametime();
			if ( player->m_flWaterJumpTime < 0 )
				player->m_flWaterJumpTime = 0;

			return false;
		}*/



		// If we are in the water most of the way...
		if ( Swimming )
		{
			// swimming, not jumping
			ClearGroundEntity();

			Velocity = Velocity.WithZ( 100 );

			// play swimming sound
			//  if ( player->m_flSwimSoundTime <= 0 )
			{
				// Don't play sound again for 1 second
				//   player->m_flSwimSoundTime = 1000;
				//   PlaySwimSound();
			}

			return;
		}

		if ( GroundEntity == null )
			return;

		/*
		if ( player->m_Local.m_bDucking && (player->GetFlags() & FL_DUCKING) )
			return false;
		*/

		/*
		// Still updating the eye position.
		if ( player->m_Local.m_nDuckJumpTimeMsecs > 0u )
			return false;
		*/

		ClearGroundEntity();

		// player->PlayStepSound( (Vector &)mv->GetAbsOrigin(), player->m_pSurfaceData, 1.0, true );

		// MoveHelper()->PlayerSetAnimation( PLAYER_JUMP );

		float flGroundFactor = 1.0f;
		//if ( player->m_pSurfaceData )
		{
			//   flGroundFactor = g_pPhysicsQuery->GetGameSurfaceproperties( player->m_pSurfaceData )->m_flJumpFactor;
		}

		float flMul = 268.3281572999747f * 1.2f;

		float startz = Velocity.z;

		if ( Duck.IsActive )
			flMul *= 0.8f;

		Velocity = Velocity.WithZ( startz + flMul * flGroundFactor );

		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		// mv->m_outJumpVel.z += mv->m_vecVelocity[2] - startz;
		// mv->m_outStepHeight += 0.15f;

		// don't jump again until released
		//mv->m_nOldButtons |= IN_JUMP;

		AddEvent( "jump" );

	}
	void CategorizePosition( bool bStayOnGround )
	{
		SurfaceFriction = 1.0f;

		// Doing this before we move may introduce a potential latency in water detection, but
		// doing it after can get us stuck on the bottom in water if the amount we move up
		// is less than the 1 pixel 'threshold' we're about to snap to.	Also, we'll call
		// this several times per frame, so we really need to avoid sticking to the bottom of
		// water on each call, and the converse case will correct itself if called twice.
		//CheckWater();

		var point = Position - Vector3.Up * 2;
		var vBumpOrigin = Position;

		//
		//  Shooting up really fast.  Definitely not on ground trimed until ladder shit
		//
		bool bMovingUpRapidly = Velocity.z > MaxNonJumpVelocity;
		bool bMovingUp = Velocity.z > 0;

		bool bMoveToEndPos = false;

		if ( GroundEntity != null ) // and not underwater
		{
			bMoveToEndPos = true;
			point.z -= StepSize;
		}
		else if ( bStayOnGround )
		{
			bMoveToEndPos = true;
			point.z -= StepSize;
		}

		if ( bMovingUpRapidly || Swimming ) // or ladder and moving up
		{
			ClearGroundEntity();
			return;
		}

		var pm = TraceBBox( vBumpOrigin, point, 4.0f );

		if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
		{
			ClearGroundEntity();
			bMoveToEndPos = false;

			if ( Velocity.z > 0 )
				SurfaceFriction = 0.25f;
		}
		else
		{
			UpdateGroundEntity( pm );
		}

		if ( bMoveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
		{
			Position = pm.EndPos;
		}

	}
	void RestoreGroundPos()
	{
		if ( GroundEntity == null || GroundEntity.IsWorld )
			return;

		//var Position = GroundEntity.Transform.ToWorld( GroundTransform );
		//Pos = Position.Position;
	}

	void SaveGroundPos()
	{
		if ( GroundEntity == null || GroundEntity.IsWorld )
			return;

		//GroundTransform = GroundEntity.Transform.ToLocal( new Transform( Pos, Rot ) );
	}
}

