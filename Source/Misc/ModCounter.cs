﻿using System;

namespace RimBattle
{
	public class ModCounter
	{
		// a very simple and gdpr friendly mod launch counter.
		// no personal information is transfered and firebase
		// doesn't store the IP or any other traceable information

		static readonly string baseUrl = "http://us-central1-brrainz-mod-stats.cloudfunctions.net/ping?";
		public static void Trigger()
		{
			try
			{
				var uri = new Uri(baseUrl + "RimBattle");
				var client = new System.Net.WebClient();
				client.DownloadStringAsync(uri);
			}
			catch (Exception)
			{
			}
		}
	}
}