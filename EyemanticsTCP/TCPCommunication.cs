using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class TCPCommunication : MonoBehaviour
{
    // External variables
    public bool communicating = false;
    public float[] mask;

    // Communication thread
    internal Thread mthread;

    // TCP variables
    internal string connectionIP = "127.0.0.1";
    internal int port = 4350;
    internal TcpClient client = new TcpClient();
    internal NetworkStream stream = null;

    // Dummy variables for current use
    private byte[] img = File.ReadAllBytes(Application.dataPath + "/Images/world.png");
    private float[] coords = new float[] { 3.11f, 2.23f };

    // Start is called before the first frame update
    void Start()
    {
        IPAddress ipadd = IPAddress.Parse(connectionIP);
        client.Connect(ipadd, port);

        stream = client.GetStream();

        ThreadStart ts = new ThreadStart(Communication);
        mthread = new Thread(ts);
        mthread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Communication()
    {
        // Send image and coordinates
        SendData(img);

        // Receive mask

    }

    void SendData(byte[] image)
    {
        // Send image length
        Debug.Log(image.Length);
        byte[] imageSize = BitConverter.GetBytes(image.Length);
        stream.Write(imageSize, 0, imageSize.Length);

        // Send image
        int pckSize = 4096;

        for(int i = 0; i < image.Length; i += pckSize)
        {
            int remainigBytes = Math.Min(pckSize, image.Length - i);
            byte[] pck = new byte[remainigBytes];
            Array.Copy(image,i,pck,0,remainigBytes);
            stream.Write(pck,0,pck.Length);
        }


        // Receive response
        byte[] buff = new byte[1024];
        int bytesRead = stream.Read(buff, 0, buff.Length);
        Debug.Log(Encoding.ASCII.GetString(buff, 0, bytesRead));


        // Send coordinates
        byte[] vectorBytes = new byte[8];
        BitConverter.GetBytes(coords[0]).CopyTo(vectorBytes,0);
        BitConverter.GetBytes(coords[1]).CopyTo(vectorBytes,4);

        stream.Write(vectorBytes, 0, vectorBytes.Length);

    }

    void ReceiveData()
    {

    }

    void OnDestroy() 
    {
        stream.Close();
        client.Close();
    }
}
