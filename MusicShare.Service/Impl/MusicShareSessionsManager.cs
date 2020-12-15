﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Service.Impl
{
    class MusicShareSessionsManager : IMusicShareSessionsManager
    {
        public TimeSpan SessionCleanupTimeout { get; set; }

        readonly object _lock = new object();
        readonly Dictionary<Guid, MusicShareSessionContext> _sessionsById = new Dictionary<Guid, MusicShareSessionContext>();
        readonly Dictionary<long, LinkedList<MusicShareSessionContext>> _sessionsByUserId = new Dictionary<long, LinkedList<MusicShareSessionContext>>();
        readonly LinkedList<MusicShareSessionContext> _unboundSession = new LinkedList<MusicShareSessionContext>();

        public MusicShareSessionsManager(TimeSpan sessionCleanTimeout)
        {
            this.SessionCleanupTimeout = sessionCleanTimeout;
        }

        public void DeleteSession(Guid sessionId)
        {
            lock (_lock)
            {
                if (_sessionsById.TryGetValue(sessionId, out var session))
                {
                    _sessionsById.Remove(sessionId);
                    session.ListNode.List.Remove(session.ListNode);

                    if (session.UserId != 0 && _sessionsByUserId.TryGetValue(session.UserId, out var list) && list.Count == 0)
                         _sessionsByUserId.Remove(session.UserId);
                }
            }
        }

        public IMusicShareSessionContext CreateSession()
        {
            var session = new MusicShareSessionContext();
            session.OnUserContextChanging += newUserId =>
              {
                  lock (_lock)
                  {
                      session.ListNode.List.Remove(session.ListNode);

                      if (newUserId > 0)
                      {
                          if (!_sessionsByUserId.TryGetValue(newUserId, out var list))
                              _sessionsByUserId.Add(newUserId, list = new LinkedList<MusicShareSessionContext>());

                          list.AddLast(session.ListNode);
                      }
                      else
                      {
                          _unboundSession.AddLast(session.ListNode);
                      }
                  }
              };

            lock (_lock)
            {
                _sessionsById.Add(session.Id, session);
                _unboundSession.AddLast(session.ListNode);
            }

            return session;
        }

        public IMusicShareSessionContext GetSession(Guid id)
        {
            lock (_lock)
            {
                if (!_sessionsById.TryGetValue(id, out var session))
                    throw new ApplicationException("Session " + id + " does not exists");

                return session;
            }
        }

        public void DropUserSessions(long userId)
        {
            lock (_lock)
            {
                if (_sessionsByUserId.TryGetValue(userId, out var list))
                {
                    foreach (var session in list)
                        _sessionsById.Remove(session.Id);

                    _sessionsByUserId.Remove(userId);
                }
            }
        }

        public void CleanupSessions()
        {
            var threshold = DateTime.UtcNow - this.SessionCleanupTimeout;

            lock (_lock)
            {
                var sessionsToCleanup = _sessionsById.Values.ToArray()
                                                     .Where(s => s.LastActivity < threshold)
                                                     .ToArray();

                foreach (var session in sessionsToCleanup)
                {
                    _sessionsById.Remove(session.Id);
                    session.ListNode.List.Remove(session.ListNode);
                }
            }
        }

        public bool TryGetSession(Guid sessionId, out IMusicShareSessionContext session)
        {
            lock (_lock)
            {
                var ok = _sessionsById.TryGetValue(sessionId, out var session2);
                session = session2;
                return ok;
            }
        }

        public void Dispose()
        {
        }
    }
}
