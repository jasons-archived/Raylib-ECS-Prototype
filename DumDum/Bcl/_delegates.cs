using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Bcl;

public delegate ref T Func_Ref<T>();
public delegate void Action_Span<T>(Span<T> span);
public delegate void Action_RoSpan<T>(ReadOnlySpan<T> span);




/// <summary>
/// action delegates that allow passing parameters by ref
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <param name="val1"></param>
public delegate void Action_Ref<T>(ref T arg);
public delegate void Action_Ref<T1, T2>(ref T1 arg1, ref T2 arg2);
public delegate void Action_Ref<T1, T2, T3>(ref T1 arg1, ref T2 arg2, ref T3 arg3);
