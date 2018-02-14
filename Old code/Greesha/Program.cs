using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Greesha
{
    public class Program
    {

		public static void Main()
        {

            Robot Klichko = new Robot(false);

			Klichko.Go(240, 10);
			Klichko.Go(60, 10);

			Thread.Sleep(Timeout.Infinite);

		}




    }
}
