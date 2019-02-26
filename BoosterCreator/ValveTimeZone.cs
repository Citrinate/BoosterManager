using System;

namespace BoosterCreator {
	static class ValveTimeZone {
		static public TimeZoneInfo GetTimeZoneInfo() {
			TimeZoneInfo.TransitionTime startTransition, endTransition;
			startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0),
																				  4, 1, DayOfWeek.Sunday);
			endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0),
																			10, 5, DayOfWeek.Sunday);
			// Define adjustment rule
			TimeSpan delta = new TimeSpan(1, 0, 0);
			TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
			// Create array for adjustment rules
			TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
			// Define other custom time zone arguments
			string displayName = "(GMT-08:00) Valve Time";
			string standardName = "Valve Standard Time";
			string daylightName = "Valve Daylight Time";
			TimeSpan offset = new TimeSpan(-8, 0, 0);
			// Create custom time zone without copying DST information
			TimeZoneInfo valveTime = TimeZoneInfo.CreateCustomTimeZone(standardName, offset, displayName, standardName, daylightName, adjustments, true);
			return valveTime;
		}
	}
}
