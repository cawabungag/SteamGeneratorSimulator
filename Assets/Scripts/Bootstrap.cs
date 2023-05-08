using DefaultNamespace.StateMachine;
using UnityEngine;

namespace DefaultNamespace
{
	public class Bootstrap : Singleton<Bootstrap>
	{
		public OPCProvider LoginOpc;
		public IStateMachine StateMachine;

		protected override void Awake()
		{
			base.Awake();
			LoginOpc = new OPCProvider();
			StateMachine = new StateMachine.StateMachine();
			LoginOpc.Login();
		}

		private void Update()
		{
			StateMachine.Execute(Time.deltaTime);
		}
	}
}