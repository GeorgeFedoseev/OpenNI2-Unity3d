using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

public class Loom : MonoBehaviour
{
	public static int maxThreads = 8;
	static int numThreads;

	private static Loom _current;
	private int _count;
	public static Loom Current
	{
		get
		{
			Initialize();
			return _current;
		}
	}

	void Awake()
	{
		_current = this;
		initialized = true;
	}

	static bool initialized;

	public static void Initialize()
	{
		if (!initialized)
		{

			if(!Application.isPlaying)
				return;
			initialized = true;
			var g = new GameObject("Loom");
			DontDestroyOnLoad(g);
			_current = g.AddComponent<Loom>();
		}

	}

	private List<Action> _actions = new List<Action>();
	public struct DelayedQueueItem
	{
		public DateTime time;
		public Action action;
		public string name;
	}
	private List<DelayedQueueItem> _delayed = new  List<DelayedQueueItem>();

	List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

	List<DelayedQueueItem> _removeDelayed = new List<DelayedQueueItem>();

	public static void QueueOnMainThread(Action action)
	{
		QueueOnMainThread( action, 0f);
	}
	public static void QueueOnMainThread(Action action, float _time, string name = "")
	{
		var time = TimeSpan.FromSeconds (_time);

		if(_time != 0)
		{
			lock(Current._delayed)
			{
				//Debug.Log("Loom: add delayed "+name);
				Current._delayed.Add(new DelayedQueueItem { time = DateTime.Now + time, action = action, name = name});
			}
		}
		else
		{
			lock (Current._actions)
			{
				Current._actions.Add(action);
			}
		}
	}

	public static void removeAll(){
		lock(_current._delayed){
			_current._removeDelayed.Clear();
			_current._removeDelayed.AddRange(_current._delayed);
			foreach(var item in _current._removeDelayed){
				_current._delayed.Remove(item);
			}
		}
	}

	public static void removeByName(string name){
		lock(_current._delayed){

			//Debug.Log ("Loom: current delayed:");
			/*foreach(var item in _current._delayed){
				Debug.Log(item.name);
			}*/
			//Debug.Log ("Loom: remove delayed");
			_current._removeDelayed.Clear();
			_current._removeDelayed.AddRange(_current._delayed.Where(d=>d.name == name));
			foreach(var item in _current._removeDelayed){
				//Debug.Log ("Loom: remove delayed '"+name+"'");
				_current._delayed.Remove(item);
			}
		}
	}

	public static Thread RunAsync(Action a)
	{
		Initialize();
		while(numThreads >= maxThreads)
		{
			Thread.Sleep(1);
		}
		Interlocked.Increment(ref numThreads);
		ThreadPool.QueueUserWorkItem(RunAction, a);
		return null;
	}

	private static void RunAction(object action)
	{
		try
		{
			((Action)action)();
		}
		catch
		{
		}
		finally
		{
			Interlocked.Decrement(ref numThreads);
		}

	}


	void OnDisable()
	{
		if (_current == this)
		{

			_current = null;
		}
	}



	// Use this for initialization
	void Start()
	{

	}

	List<Action> _currentActions = new List<Action>();

	// Update is called once per frame
	void Update()
	{
		lock (_actions)
		{
			_currentActions.Clear();
			_currentActions.AddRange(_actions);
			_actions.Clear();
		}
		foreach(var a in _currentActions)
		{
			a();
		}
		lock(_delayed)
		{
			_currentDelayed.Clear();
			_currentDelayed.AddRange(_delayed.Where(d=>d.time <= DateTime.Now));
			foreach(var item in _currentDelayed)
				_delayed.Remove(item);
		}
		foreach(var delayed in _currentDelayed)
		{
			delayed.action();
		}



	}
}