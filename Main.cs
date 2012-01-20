using System;
using System.Dynamic;
using Newtonsoft.Json;
using System.Web;
using Mono.Options;
using ForgottenArts.RestClient;
using System.Configuration;
using System.Collections.Generic;

namespace Quantagrate
{
	class MainClass
	{
		static string baseServer, username, password, project, milestone;
		
		public static int Main (string[] args)
		{
			var settings = ConfigurationManager.AppSettings;
			baseServer = settings["baseServer"];
			username = settings["username"];
			password = settings["password"];
			project = settings["project"];
			milestone = null;
			bool showHelp = false;
			
			var p = new OptionSet () {
				{"s|server:", "jira server url (eg: http://jira.forgottenarts.com/)", v => baseServer = v},
				{"u|username:", "jira account", v => username = v},
				{"p|password:", "password for jira account", v => password = v},
				{"project:", "project to connect to (eg. RMX)", v => project = v},
				{"m|milestone=", "fix version (e.g. July)", v=> milestone = v},
				{"h|help", "show help", v => showHelp = v != null}
			};
			
			List<string> extras = null;
			try
			{
				extras = p.Parse (args);
			}
			catch
			{
				ShowHelp (p);
				return 1;
			}
			
			if (showHelp || extras.Count == 0)
			{
				ShowHelp(p);
				return 0;
			}
			
			switch (extras[0])
			{
			case "milestone":
				Milestone ();
				break;
			case "tickets":
				Tickets ();
				break;
			}

			return 0;
		}
		
		static void Milestone ()
		{
			var rc = getClient ();
			
			dynamic resp = rc.Get(string.Format("project/{0}/versions", project));
			
			DateTime soonest = DateTime.MaxValue;
			dynamic currentMilestone = null;
			foreach (dynamic m in resp)
			{
				if (m.releaseDate != null && DateTime.Parse(m.releaseDate) < soonest)
				{
					soonest = DateTime.Parse(m.releaseDate);
					currentMilestone = m;
				}
			}

			Console.WriteLine (resp);
			if (currentMilestone != null)
				Console.Write (currentMilestone.name);
			
		}

		static void Tickets ()
		{
			var rc = getClient ();
			
			string jql = HttpUtility.UrlEncode (string.Format 
				("project = {0} AND resolution = Fixed AND fixVersion = {1} AND status in (Resolved, Closed)", project, milestone));
			
			dynamic results = rc.Get ("search?maxResults=1000&jql=" + jql);
			
			foreach (var issue in results.issues)
			{
				Console.WriteLine (issue.key);
			}
		}

		static RestClient getClient ()
		{
			RestClient rc = new RestClient (baseServer + "rest/auth/latest/");
			dynamic response = rc.Post ("session", new {username=username, password=password});
			
			rc.Server = baseServer + "rest/api/latest/";
			return rc;
		}

		public static void ShowHelp (OptionSet p)
		{
			Console.WriteLine ("./Quantagrate [OPTIONS] cmd");
			Console.WriteLine ("Returns a list of resolved / closed tickets for a given milestone.");
			Console.WriteLine ();
			p.WriteOptionDescriptions (Console.Out);
			Console.WriteLine ();
			Console.WriteLine ("Commands:");
			Console.WriteLine ("\tmilestone -- get the current milestone");
			Console.WriteLine ("\ttickets -- return tickets that are resolved or closed for the current milestone");
		}
	}
}
