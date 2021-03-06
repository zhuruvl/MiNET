﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TestPlugin.Teams
{
	[DataContract]
	public class GameSettings
	{
		[DataMember]
		public string Name { get; set; }

		public Type LevelType { get; set; }

		public GameMode GameMode { get; set; }

		[DataMember]
		public string ArenaWorldPath { get; set; }

		[DataMember]
		public bool AllowRespwan { get; set; }

		[DataMember]
		public bool IsTimeLimited { get; set; }

		[DataMember]
		public long TimeLimit { get; set; }

		[DataMember]
		public int MaxPlayers { get; set; }

		[DataMember]
		public int MinPlayers { get; set; }

		[DataMember]
		public List<SpawnCoordinates> SpawnLocations { get; set; }
	}
}