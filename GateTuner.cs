﻿// This file is part of Hangfire.
// Copyright © 2019 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Logging;
using Hangfire.Server;

namespace ConsoleApp13
{
    public class GateTuner : IBackgroundProcess
    {
        private readonly ILog _logger = LogProvider.GetCurrentClassLogger();
        private readonly Gate _gate;
        private readonly HashSet<string> _queues;
        private readonly TimeSpan _delay;
        private readonly string _queueNames;

        public GateTuner(Gate gate, string[] queues, TimeSpan delay)
        {
            _gate = gate;
            _queues = new HashSet<string>(queues, StringComparer.OrdinalIgnoreCase);
            _queueNames = String.Join(", ", queues);
            _delay = delay;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var api = context.Storage.GetMonitoringApi();
            var queues = api.Queues();

            _logger.Trace("Checking queue length to decide whether to change gate levels...");

            if (queues.Any(queue => _queues.Contains(queue.Name) && queue.Length > 0))
            {
                if (_gate.TryIncreaseLevel(out var level))
                {
                    _logger.Debug($"Gate level for queues ({_queueNames}) increased to {level} / {_gate.MaxLevel}");
                }
            }
            else
            {
                if (_gate.TryDecreaseLevel(out var level))
                {
                    _logger.Debug($"Gate level for queues ({_queueNames}) decreased to {level} / {_gate.MaxLevel}");
                }
            }

            context.Wait(_delay);
        }
    }
}