using balloonparty.entities;
using balloonparty.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

partial class BalloonPartyPlayer : DeathmatchPlayer
{
	public EntityPooler<BalloonGrenadeEntity> grenadePooler { get; private set; }

	public BalloonPartyPlayer() : base()
	{
		grenadePooler = new EntityPooler<BalloonGrenadeEntity>(2);
	}
}
