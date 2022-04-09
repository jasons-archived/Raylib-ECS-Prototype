// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;

namespace NotNot.SimPipeline;

//}

/// <summary>
/// cheap non-blocking way to track resource availabiltiy
/// <para>NOT thread safe.</para>
/// </summary>
[ThreadSafety(ThreadSituation.Never)]
public class ReadWriteCounter
{
	public int _writes;
	public int _reads;
	private int _version;


	public bool IsReadHeld { get { return _reads > 0; } }
	public bool IsWriteHeld { get { return _writes > 0; } }
	public bool IsAnyHeld { get => IsReadHeld || IsWriteHeld; }

	public void EnterWrite()
	{
		_version++;
		var ver = _version;
		__DEBUG.Throw(IsAnyHeld == false, "a lock already held");
		_writes++;
		__DEBUG.Throw(_writes == 1, "writes out of balance");
		__DEBUG.Assert(ver == _version);
	}
	public void ExitWrite()
	{
		_version++;
		var ver = _version;
		_writes--;
		__DEBUG.Throw(_writes == 0, "writes out of balance");
		__DEBUG.Assert(ver == _version);
	}
	public void EnterRead()
	{
		_version++;
		var ver = _version;
		__DEBUG.Throw(IsWriteHeld == false, "write lock already held");
		_reads++;
		__DEBUG.Throw(_reads > 0, "reads out of balance");
		__DEBUG.Assert(ver == _version);
	}
	public void ExitRead()
	{
		_version++;
		var ver = _version;
		_reads--;
		__DEBUG.Throw(_reads >= 0, "reads out of balance");
		__DEBUG.Assert(ver == _version);
	}

}
