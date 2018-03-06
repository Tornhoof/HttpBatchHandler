using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace HttpBatchHandler.Tests
{
    public static class RandomPortHelper
    {
        private static readonly object Lock = new object();
        private static readonly Random Random = new Random();

        public static int FindFreePort()
        {
            lock (Lock)
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
                var usedPorts = new HashSet<int>(tcpConnInfoArray.Select(t => t.LocalEndPoint.Port));
                int freePort;
                var counter = 0;
                while (usedPorts.Contains(freePort = GetRandomNumber(1025, 65535)))
                {
                    counter++;
                    if (counter > 1000)
                    {
                        throw new InvalidOperationException("Can't find port.");
                    }
                }

                return freePort;
            }
        }

        private static int GetRandomNumber(int minValue, int maxValue)
        {
            lock (Lock)
            {
                return Random.Next(minValue, maxValue);
            }
        }
    }
}