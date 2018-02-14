using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using static System.Math;

namespace Greesha
{

    class Robot
    {
        InputPort button = new InputPort(Pins.ONBOARD_BTN, true, Port.ResistorMode.PullUp);
        PWM leftFrontMotor = new PWM(PWMChannels.PWM_PIN_D11, 1000, 0, false);
        PWM leftBackMotor = new PWM(PWMChannels.PWM_PIN_D6, 1000, 0, false);
        PWM rightFrontMotor = new PWM(PWMChannels.PWM_PIN_D10, 1000, 0, false);
        PWM rightBackMotor = new PWM(PWMChannels.PWM_PIN_D9, 1000, 0, false);

		

		bool check = false;
        int north, south, west, east;
		int enemyDegree; 

        public Robot(bool calibration)
        {
			Compass.Start();

			if (calibration)
            {
                int[] orientation = AutoCalibration();

                north = orientation[0];
                south = orientation[1];
                west = orientation[2];
                east = orientation[3];

				Debug.Print("Robot is calibrated.");
				Debug.Print("minX (north): " + north);
				Debug.Print("maxX (south): " + south);
				Debug.Print("minY (west) : " + west);
				Debug.Print("maxY (east) : " + east);

			}
            else
            {
                north = 2570;
                south = 3452;
                west = 1150;
                east = 1730;
            }

		}



		public void Go(int degree, int time)
		{			
			enemyDegree = degree;

			Thread moving = new Thread(ThreadForGo);
			moving.Start();

			// ----------------------------- \\
			// ---Хорошее место для кода!--- \\
			// ----------------------------- \\

			Thread.Sleep(time * 1000);
			moving.Suspend();             // устаревший метод, замени как только сможешь      
			moving.Abort();
			Motors(0, 0); 
		}

		private void ThreadForGo()
		{
			double delta, turn, right, left, forsage;

			while (true)
			{
				delta = enemyDegree - CurrentDegree();
				turn = Tan((delta * PI / 180) / 2);

				if (Abs(turn) > 0.5) turn = 0.5 * Abs(turn) / turn;
				right = 0.5 - turn;
				left = 0.5 + turn;

				forsage = 1 - Max(left, right);
				left += forsage;
				right += forsage;

				Debug.Print(" место положения " + CurrentDegree());
				Debug.Print(" место назначения " + enemyDegree);
				Debug.Print("Left: " + left);
				Debug.Print("Right: " + right + "\n\n");

				Motors(left, right);
			} 
		}



		private double CurrentDegree()
        {
			int[] xyz = new int[3];
			double x, y, degree;

			xyz = Compass.Read();
			x = xyz[0];
			y = xyz[1];

            degree = 180 * (x - north) / (south - north);
            if (y < (west + east) / 2) degree = 360 - degree;

			//Debug.Print("X: " + x);
			//Debug.Print("Y: " + y + "\n");
			//Thread.Sleep(500);

			return degree;
        }



        public int[] AutoCalibration()
        {
            int[] xyz = new int[3];
			int x = 0, y = 0,
				minX = 66000, minY = 66000,
				maxX = 0, maxY = 0,
				minX0 =0, maxX0=0, minY0=0, maxY0=0;

			Motors(0, 1);

			int i = 0, j = 0;
			do
			{
				if (i > 10)
				{
					minX0 = minX;
					maxX0 = maxX;
					minY0 = minY;
					maxY0 = maxY;

					minX = Min(x, minX);
					maxX = Max(x, maxX);
					minY = Min(y, minY);
					maxY = Max(y, maxY);

					if (minX0 == minX &&
						maxX0 == maxX &&
						minY0 == minY &&
						maxY0 == maxY)
					{
						Thread.Sleep(j++);
					}
					else j = 0;
				}
				else Thread.Sleep(i);
													
				xyz = Compass.Read();
				x = xyz[0];
				y = xyz[1];

				i++;
			} while (j < 50);

            Motors(0, 0);

            int[] orientation = { minX, maxX, minY, maxY};
            return orientation;
        }



        public void Motors(double left, double right)
        {
			leftFrontMotor.DutyCycle = left;
			leftBackMotor.DutyCycle = left;
			rightFrontMotor.DutyCycle = right;
			rightBackMotor.DutyCycle = right;

			//if (button.Read())
			//{
			//	if (check == false)
			//	{
			        leftFrontMotor.Start();
                    leftBackMotor.Start();
                    rightFrontMotor.Start();
                    rightBackMotor.Start();
			       // check = true;
		        //}
          //      else
          //      {
          //          leftFrontMotor.Stop();
          //          leftBackMotor.Stop();
          //          rightFrontMotor.Stop();
          //          rightBackMotor.Stop();
          //          check = false;
          //      }
          //   }

        }

    }






    public static class Compass
    {
        static I2CDevice.Configuration config = new I2CDevice.Configuration(28, 100);
        static I2CDevice compass = new I2CDevice(config);


		public static void Start()
		{
			byte[] settings1 = new byte[] { 0x20, 125 };
			byte[] settings2 = new byte[] { 0x23, 12 };
			WriteToCompass(settings1);
			WriteToCompass(settings2);
		}

		public static int[] Read()
        {
            byte[] reboot = new byte[] { 0x21, 120 };    
            byte[] coordinates = new byte[] { 0x28 };
            byte[] inputXYZ = new byte[6];
            int x, y, z;           

            WriteToCompass(coordinates);
            inputXYZ = ReadFromCompass();

            x = inputXYZ[1] * 255 + inputXYZ[0];
            y = inputXYZ[3] * 255 + inputXYZ[2];
            z = inputXYZ[5] * 255 + inputXYZ[4];
            int[] outputXYZ = {x,y,z};

            WriteToCompass(reboot);

            return outputXYZ;            
        }
        

        private static void WriteToCompass(byte[] command)
        {                   
            I2CDevice.I2CWriteTransaction writeTransaction = I2CDevice.CreateWriteTransaction(command);
            I2CDevice.I2CTransaction[] transaction = new I2CDevice.I2CTransaction[] { writeTransaction };

            compass.Execute(transaction, 1000);
            compass.Execute(transaction, 1000);
        }


        private static byte[] ReadFromCompass()
        {
            byte[] inBuffer = new byte[6];
            I2CDevice.I2CReadTransaction readTransaction = I2CDevice.CreateReadTransaction(inBuffer);
            I2CDevice.I2CTransaction[] transaction = new I2CDevice.I2CTransaction[] { readTransaction };

            compass.Execute(transaction, 1000);
            return inBuffer;
        }
    }
}
