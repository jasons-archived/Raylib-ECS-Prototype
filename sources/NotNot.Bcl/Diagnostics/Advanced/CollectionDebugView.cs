// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl.Collections.Advanced;

/// <summary>
/// can be applied as part of [<see cref="DebuggerTypeProxyAttribute"/>] to have a nice debug view of collections.
/// <para>
/// For Example:
/// <example>
/// [DebuggerTypeProxy(typeof(CollectionDebugView{}))]
/// [DebuggerDisplay("{ToString(),raw}")]
/// public sealed class MemoryOwner_Custom{T} : IMemoryOwner{T}
/// </example>
/// </para>
/// </summary>
public sealed class CollectionDebugView<T>
{
    public CollectionDebugView(IEnumerable<T>? collection)
    {
        this.Items = collection?.ToArray();
    }
    public CollectionDebugView(Mem<T>? collection)
    {
        this.Items = collection?.DangerousGetArray().ToArray();
    }
    public CollectionDebugView(ReadMem<T>? collection)
    {
        this.Items = collection?.DangerousGetArray().ToArray();
    }
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public T[]? Items { get; }
}
