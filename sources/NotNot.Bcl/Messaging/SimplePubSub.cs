// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl.Messaging;

/// <summary>
/// Publish-Subscribe (Observer) Pattern.
/// 
/// <para>Designed for high performance messaging in games</para>
/// 
/// <list type="number">
/// <listheader>Design considerations:</listheader>
/// <item>Thread safe:  can be published and/or subscribed simultaniously on multiple threads</item>
/// <item> Memory-Leak proof: disposal of subscribers will not leak memory</item>
/// <item>Alloc free:  Attaching Publishers and Subscribers allocates, as does queue resizes, but sending messages does not.   If this matters, be sure to send structs.</item>
/// </list>
/// 
/// <para>How to use:  </para>
/// 
/// <para>Future work:  add performance tests,  change queue-per-subscriber to a view into a shared buffer.  (reduce memory footprint and message copying)</para>
///
/// <code>
/// //Publisher: <para/>
/// var channel = pubSub.GetChannel{int}("myKey"); //create/get a channel for sending and recieving messages. <para/>
/// channel.Publish(1); <para/>
/// <para/>
/// //Subscriber:<para/>
/// var channel = pubSub.GetChannel{int}("myKey"); //create/get a channel for sending and recieving messages. <para/>
/// var queue = new ConcurrentQueue{int}(); //create a thread safe queue that messages will be put in. <para/>
/// channel.Subscribe(queue); //now whenever a publish occurs, the queue will get the message <para/>
///  <para/>
/// while(queue.TryDequeue(out var message)){ Console.Writeline(message); }  //do work with the message <para/>
/// </code>
/// </summary>
public class SimplePubSub
{




	/// <summary>
	/// key = Pub key (anything).
	/// value = List{WeakReference{ConcurrentQueue{V}} (Subscribers)
	/// </summary>
	/// <returns></returns>
	private readonly ConcurrentDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();


	/// <summary>
	/// obtain a channel for sending and recieving messages.
	/// </summary>
	/// <typeparam name="TMessage">must be struct (to prevent GC allocations).  Pass a tuple if you NEED to pass an object</typeparam>
	/// <param name="key"></param>
	/// <returns></returns>
	public MessageChannel<TMessage> GetChannel<TMessage>(string key) where TMessage : struct
	{

		var channel = _storage.GetOrAdd(key, (_key) => (new MessageChannel<TMessage>(key)));

		return (MessageChannel<TMessage>)channel;

	}

}
