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
using System.Net.NetworkInformation;

public class TCPServer : MonoBehaviour
{
    // External variables
    public static bool connected = false;
    public static bool communicating = false;
    public static bool[][] mask;

    // Communication thread
    internal static Thread connThread;
    public static Thread commThread;

    // TCP variables
    internal static string connectionIP = "127.0.0.1";
    internal static int port = 4350;
    internal static TcpListener listener;
    internal static TcpClient client = new TcpClient();
    internal static NetworkStream stream = null;

    // Setting true if mask received
    public static bool newMaskFlag = false;

    private static ImageGazeInput imggaze;
    public static bool firstIter = true;

    void Start()
    {
        if(!connected)
        {
            connectionIP = GetLocalIPAddress();
            
            ThreadStart ts = new ThreadStart(Connect);
            connThread = new Thread(ts);
            connThread.Start();
        }
        

        imggaze = GameObject.Find("/InputManager").GetComponent<ImageGazeInput>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Start server and listen to clients
    /// </summary>
    public static void Connect()
    {
        IPAddress ipadd = IPAddress.Parse(connectionIP);
        listener = new TcpListener(ipadd, port);
        listener.Start();
        client = listener.AcceptTcpClient();

        connected = true;
        stream = client.GetStream();

    }

    /// <summary>
    /// Communicaion pipeline
    /// </summary>
    public static void Communication()
    {
        // communication = true set in CamCapture

        byte[] img = imggaze.bytes;
        Vector2 coordsVec = imggaze.pixelPos;

        float[] coords = new float[] { coordsVec.x, coordsVec.y };

        // Send image and coordinates
        SendData(img,coords);

        Debug.Log("Start Receiving");

        // Receive mask
        ReceiveData();

        // Reset communication boolean
        communicating = false;
    }

    /// <summary>
    /// Send image and coords data
    /// </summary>
    /// <param name="image"></param>
    /// <param name="coords"></param>
    static void SendData(byte[] image, float[] coords)
    {

        Debug.Log("Start Sending");
        // Send image length
        Debug.Log(image.Length);
        byte[] imageSize;

        // Send coordinates
        byte[] vectorBytes = new byte[8];
        BitConverter.GetBytes(coords[0]).CopyTo(vectorBytes, 0);
        BitConverter.GetBytes(coords[1]).CopyTo(vectorBytes, 4);

        int totalLength = image.Length + vectorBytes.Length;

        byte[] fullData = new byte[totalLength];
        Array.Copy(image, 0, fullData, 0, image.Length);
        Array.Copy(vectorBytes, 0, fullData, image.Length, vectorBytes.Length);

        imageSize = BitConverter.GetBytes(totalLength);

        // Send full length
        stream.Write(imageSize, 0, imageSize.Length);

        // Send image and coordinates
        int pckSize = 4096;

        for(int i = 0; i < fullData.Length; i += pckSize)
        {
            int remainigBytes = Math.Min(pckSize, fullData.Length - i);
            byte[] pck = new byte[remainigBytes];
            Array.Copy(fullData, i,pck,0,remainigBytes);
            stream.Write(pck,0,pck.Length);
        }

        Debug.Log("Sending Completed");

    }

    /// <summary>
    /// Receive the boolean segmentation mask
    /// </summary>
    static void ReceiveData()
    {

        int bytesRead;
 
        // Since we now the dimension we want to receive, we can facilitate the process
        int rows = 2160;
        int cols = 2880;
        //Debug.Log(cols);

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

        newMaskFlag = true;
        // printMatrix(mask);
    }

    /// <summary>
    /// Get local IP address of ML
    /// </summary>
    /// <returns></returns>
    public static string GetLocalIPAddress()
    {
        try
        {
            connectionIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Last(f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            return connectionIP;
        }
        catch (Exception e)
        {
            Debug.LogError("Error obtaining local IP address: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Print Matrix for Debugging
    /// </summary>
    /// <param name="mat"></param>
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
        listener.Stop();
    }
}