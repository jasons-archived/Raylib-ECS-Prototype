using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl;


public class ThreadSafetyAttribute : Attribute
{
	public ThreadSafetyAttribute(params ThreadSituation[] safeSituations)
	{

	}
}

public enum ThreadSituation
{
	/// <summary>
	/// not thread safe
	/// </summary>
	Never,

	/// <summary>
	/// You can safely add while other activities are performed
	/// </summary>
	Add,
	/// <summary>
	/// You can safely remove while other activities are performed
	/// </summary>
	Remove,
	/// <summary>
	/// given a key that is known to exist, partially change the state contained.  
	/// This is applicable for structs only, as modifying a struct changes the state of the containing object.
	/// If you modify an object, the reference is still the same so the containing object state doesn't change.
	/// </summary>
	RefModify,
	/// <summary>
	/// given a key that is known to exist, read it's value.
	/// </summary>
	ReadExisting,
	/// <summary>
	/// query the object for existing objects and do work on them.
	/// </summary>
	Query,

	/// <summary>
	/// Overwrites may occur as long as they are done in an atomic fashion.
	/// </summary>
	Overwrite,

	/// <summary>
	/// everything is thread safe
	/// </summary>
	Always,

}