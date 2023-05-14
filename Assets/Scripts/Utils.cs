using System;
using States;
using States.Implementation;

namespace DefaultNamespace
{
	public static class Utils
	{
		public const float TEMPERATURE_HEATING_TO_100_IN_MINUTES = 500f;
		public const float TEMPERATURE_HEATING_TO_100_IN_SECOND = TEMPERATURE_HEATING_TO_100_IN_MINUTES / 60;

		public const float TEMPERATURE_COOLING_TO_100_IN_MINUTES = 500f;
		public const float TEMPERATURE_COOLING_TO_100_IN_SECOND = TEMPERATURE_COOLING_TO_100_IN_MINUTES / 60;

		public const float MAX_TEMPERATURE = 175;
		public const float MIN_TEMPERATURE = 21;
		public const float MIN_PREASURE = 0;
		public const float MAX_PREASURE = 0.8f;

		public static IState ToClientState(this State serverState)
		{
			IState state;
			switch (serverState.Type)
			{
				case StateType.Heating:
					state = new HeatingState(new StateConfiguration(serverState.Duration), serverState.Id);
					break;
				case StateType.Maintenance:
					state = new HeatingState(new StateConfiguration(serverState.Duration), serverState.Id);
					break;
				case StateType.Open:
					state = new OpenState(serverState.Id);
					break;
				case StateType.Close:
					state = new CloseState(serverState.Id);
					break;
				case StateType.PluggingIn:
					state = new PlugInState(serverState.Id);
					break;
				case StateType.PluggingOut:
					state = new PluginOut(serverState.Id);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return state;
		}
	}
}