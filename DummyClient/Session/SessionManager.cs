using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient.Session
{
    class SessionManager
    {
        public static SessionManager Instance { get; } = new SessionManager();

        HashSet<ServerSession> _sessions = new HashSet<ServerSession>();
        object _lock = new object();

        int _dummyId = 1;       

        public ServerSession Generate()
        {
            lock (_lock)
            {
                ServerSession session = new ServerSession();
                session.DummyId = _dummyId++;

                _sessions.Add(session);
                Console.WriteLine($"Connected ({_sessions.Count})");
                return session;
            }
        }

        public void Remove(ServerSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
                Console.WriteLine($"Removed ({_sessions.Count})");
            }
        }

    }
}
