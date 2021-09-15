using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Bcl;

public delegate ref T Func_Ref<T>();
public delegate void Action_Span<T>(Span<T> span);
public delegate void Action_ReadOnlySpan<T>(ReadOnlySpan<T> span);