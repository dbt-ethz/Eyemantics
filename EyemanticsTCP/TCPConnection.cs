using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Android;

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Linq;

public class TCPConnection : MonoBehaviour
{

	// External variables
	public bool communicating = false;
	public double[] mask;

	// TCP variables
	internal string IPaddressString = "127.0.0.1";
	internal int port = 4350;
	internal TcpClient client = new TcpClient();
	internal NetworkStream stream;

	// Communicator Thread started after Camera Capture;
	public Thread communicatorThread;

	void Start()
	{
		// Connect to python server on PC
		IPAddress ipAdd = IPAddress.Parse(IPaddressString);
		client.Connect(ipAddress,port);

	}

	void Update()
	{
	}

	public void Communication()
	{

		// Get image byte array and 2D gaze coordinates
		byte[] img;
		float[] coords;

		// Send image and cooridnates
		SendData(img, coords);

		// Receive the resulting mask
		ReceiveData(img.Length);

		// Open ability for new Camera Capture
		communicating = false;

	}

	private void SendData(byte[] img, float[] coords)
	{

		stream = client.GetStream;

		// Send image in packages
		int pckSize = 4096;
		for(int i = 0; i < img.Length; i+= pckSize)
		{
			int remainingBytes = Math.Min(pckSize, img.Length - i);
			byte[] pck = new byte[remainingBytes];
			Array.Copy(img,i,pck,0,remainingBytes);
			stream.Write(pck,0,pck.Length);
		}


		// Send coordinates
		// float x = 8.423f;
		// float y = 3.421f;
		// byte[] vectorBytes = new byte[8];
		// BitConverter.GetBytes(x).CopyTo(vectorBytes,0);
		// BitConverter.GetBytes(y).CopyTo(vectorBytes,4);
		// stream.Write(vectorBytes, 0, vectorBytes.Length);
		
		stream.Close();
	}

	private void ReceiveData(int size)
	{
		stream = client.GetStream();

		// :3 because of the three channels in the original image
		// :4 because of the change of floats to bytes 
		int floatSize = (size/3)/4;
		float[] maskArray =  new float[]{};

		int pckSize = 1024;
		byte[] receivedData = new byte[pckSize];
		float[] floatData = new float[pckSize/4];

		while(True)
		{
			stream.Read(receivedData,0,receivedData.Length);

			if(receivedData == null)
			{
				break;
			}

			Buffer.BlockCopy(receivedData, 0, floatData, 0, pckSize);
			maskArray = maskArray.Concat(floatData).ToArray();
		}

		stream.Close();

	}

}
