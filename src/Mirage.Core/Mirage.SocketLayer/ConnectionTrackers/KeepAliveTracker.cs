﻿using System;

namespace Mirage.SocketLayer.ConnectionTrackers
{
    internal class KeepAliveTracker
    {
        private double _lastSendTime = double.MinValue;
        private readonly Config _config;
        private readonly Time _time;

        public KeepAliveTracker(Config config, Time time)
        {
            _config = config;
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }


        public bool TimeToSend()
        {
            return _lastSendTime + _config.KeepAliveInterval < _time.Now;
        }

        public void SetSendTime()
        {
            _lastSendTime = _time.Now;
        }
    }

}
