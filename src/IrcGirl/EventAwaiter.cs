using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IrcGirl
{
	internal class EventAwaiter
	{
		private Dictionary<string, SemaphoreSlim> _events;
		private Dictionary<string, Exception> _errors;

		internal EventAwaiter()
		{
			_events = new Dictionary<string, SemaphoreSlim>();
			_errors = new Dictionary<string, Exception>();
		}

		public void Start(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (!_events.ContainsKey(name))
				_events.Add(name, new SemaphoreSlim(0, 1));
		}

		public void Finish(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (!_events.ContainsKey(name))
				throw new Exception("No event found by that name");

			Console.WriteLine($"Finished {name}");
			_events[name].Release();
		}

		public void Error(string name, Exception exception)
		{
			Console.WriteLine($"Raising error for {name}");

			_errors[name] = exception;

			if (_events.ContainsKey(name))
			{
				Console.WriteLine($"Released {name} on error");
				_events[name].Release();
			}
		}

		public async Task Wait(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (_errors.ContainsKey(name))
			{
				Console.WriteLine($"Wait called after error raised for {name}");
				Exception ex = _errors[name];

				throw ex;
			}

			if (!_events.ContainsKey(name))
			{
				Console.WriteLine($"Starting new event {name}");
				Start(name);
			}
			
			await _events[name].WaitAsync();

			Console.WriteLine($"Finished waiting for {name}");

			if (_errors.ContainsKey(name))
			{
				Console.WriteLine($"{name} had error on exit");
				Exception ex = _errors[name];

				throw ex;
			}

			_errors.Remove(name);
			_events.Remove(name);
		}

		public async Task WaitAny(params string[] names)
        {
			Task[] tasks = new Task[names.Length];
			for (int i = 0; i < names.Length; i++)
            {
				tasks[i] = Wait(names[i]);
            }

			await Task.WhenAny(tasks);
        }

		public bool IsInProgress(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return _events.ContainsKey(name) && _events[name].CurrentCount > 0;
		}
	}
}
