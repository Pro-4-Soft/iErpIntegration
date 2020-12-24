using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class WebEventListener
    {
        private static readonly object _syncObject = new object();

        private HubConnection _hub;
        private IHubProxy _proxy;

        private readonly ConcurrentDictionary<string, List<Action<dynamic>>> _subscribers = new ConcurrentDictionary<string, List<Action<dynamic>>>();

        public void Subscribe(string eventName, Action<dynamic> callback)
        {
            if (!_subscribers.ContainsKey(eventName))
            {
                _subscribers[eventName] = new List<Action<dynamic>>();
                _proxy?.On(eventName, payload =>
                {
                    if (!_subscribers.TryGetValue(eventName, out var list))
                        return;
                    foreach (var action in list)
                        action.Invoke(payload);
                });
            }

            if (!_subscribers[eventName].Contains(callback))
                _subscribers[eventName].Add(callback);
        }

        public void Start()
        {
            lock (_syncObject)
            {
                if (_hub == null)
                {
                    _hub = new HubConnection(Singleton<EntryPoint>.Instance.CloudUrl, true)
                    {
                        TraceLevel = TraceLevels.StateChanges,
                        TraceWriter = Singleton<LogTraceWriter>.Instance
                    };
                }

                if (_proxy == null)
                {
                    _proxy = _hub.CreateHubProxy("SubscriberHub") as HubProxy;
                    foreach (var key in _subscribers.Keys)
                        _proxy.On(key, payload =>
                        {
                            if (_subscribers.TryGetValue(key, out var list))
                                list.ForEach(c => c.Invoke(payload));
                        });
                }

                _hub.Closed += () =>
                {
                    _hub = null;
                    _proxy = null;
                    Start();
                };

                _hub.StateChanged += s =>
                {
                    if (s.NewState != ConnectionState.Connected)
                        return;
                    _proxy.Invoke("JoinGroup", Singleton<EntryPoint>.Instance.TenantId);
                };

                if (_hub.State == ConnectionState.Disconnected)
                {
                    try
                    {
                        _hub.Start();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public void Stop()
        {
            _hub?.Stop(TimeSpan.FromSeconds(5));
        }
    }

    public class LogTraceWriter : System.IO.TextWriter
    {
        public override void WriteLine(string value)
        {
            Console.Out.WriteLine(value);
        }

        public override Encoding Encoding => Encoding.Default;
    }
}
