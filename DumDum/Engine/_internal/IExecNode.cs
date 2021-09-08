using System.Collections.Generic;

namespace DumDum.Engine._internal
{
	public interface IExecNode
	{
		IEnumerable<IExecNode> UpdateAfter { get; }
		void ExecUpdate(ExecState state);

		void OnAdd(ExecManager execManager);
	}
}