using System;
using States;

namespace DefaultNamespace.UI
{
	public interface IUIStatesController
	{
		void AddState(IState state);
		void UpdateState(IState state);
		void DeleteState(Guid stateId);
	}
}