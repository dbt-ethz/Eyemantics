using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

public class TCPCommunication : MonoBehaviour
{
    // External variables
    public bool communicating = false;
    public bool[][] mask;

    // Communication thread
    internal Thread mthread;

    // TCP variables
    internal string connectionIP = "127.0.0.1";
    internal int port = 4350;
    internal TcpClient client = new TcpClient();
    internal NetworkStream stream = null;

    // Dummy variables for current use
    private byte[] img = File.ReadAllBytes(Application.dataPath + "/Images/IMG_7555.jpg");
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
        ReceiveData();

        // Reset communication boolean
        communicating = false;
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
        // Receive number rows
        byte[] buff_rows = new byte[4];
        int bytesRead = stream.Read(buff_rows, 0, buff_rows.Length);
        int rows = BitConverter.ToInt32(buff_rows, 0);

        Debug.Log(rows);

        // Receive number cols
        byte[] buff_cols = new byte[4];
        bytesRead = stream.Read(buff_cols, 0, buff_cols.Length);
        int cols = BitConverter.ToInt32(buff_cols, 0);

        Debug.Log(cols);

        // Create List of mask to append easily
        List<bool[]> maskList = new List<bool[]>();;

        byte[] buff = new byte[cols];
        bool[] mask_row = new bool[cols];

        // Receive mask row by row and convert to boolean
        for(int i = 0; i < rows; ++i)
        {
            bytesRead = stream.Read(buff, 0, buff.Length);
            mask_row = buff.Select(b => Convert.ToBoolean(b)).ToArray();

            maskList.Add(mask_row);
        }

        Debug.Log("Received");

        mask = maskList.ToArray();

        // printMatrix(mask);
    }

    void printMatrix(bool[][] mat)
    {
        for (int i = 0; i < mat.Length; i++)
        {
            for (int j = 0; j < mat[0].Length; j++)
            {
                Debug.Log(mat[i][j]);
            }
            //System.Console.WriteLine();
            Debug.Log("End of Row");
        }

    }

    void OnDestroy() 
    {
        stream.Close();
        client.Close();
    }
}