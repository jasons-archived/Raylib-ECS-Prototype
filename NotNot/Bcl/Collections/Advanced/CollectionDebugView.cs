using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl.Collections.Advanced;

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