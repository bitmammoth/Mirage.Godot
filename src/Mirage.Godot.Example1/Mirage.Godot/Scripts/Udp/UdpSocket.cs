using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Mirage.SocketLayer;


namespace Mirage.Godot.Scripts.Udp;

public class UdpSocket : ISocket
{
    private Socket _socket;
    private EndPointWrapper _endpoint;

    public void Bind(IEndPoint endPoint)
    {
        _endpoint = (EndPointWrapper)endPoint;

        _socket = CreateSocket(_endpoint.Inner);
        // todo add option to disable DualMode
        //      this is needing to run on platforms like nintendo switch
        _socket.DualMode = true;
        _socket.Bind(_endpoint.Inner);
    }

    private static Socket CreateSocket(EndPoint endPoint)
    {
        var ipEndPoint = (IPEndPoint)endPoint;
        var newSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            Blocking = false,
        };

        newSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        TrySetIOControl(newSocket);

        return newSocket;
    }

    private static void TrySetIOControl(Socket socket)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                // IOControl only seems to work on windows
                // gives "SocketException: The descriptor is not a socket" when running on github action on Linux
                // see https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L2763-L2765
                return;

            // stops "SocketException: Connection reset by peer"
            // this error seems to be caused by a failed send, resulting in the next polling being true, even those endpoint is closed
            // see https://stackoverflow.com/a/15232187/8479976

            // this IOControl sets the reporting of "unrealable" to false, stoping SocketException after a connection closes without sending disconnect message
            const uint iOC_IN = 0x80000000;
            const uint iOC_VENDOR = 0x18000000;
            const uint sIO_UDP_CONNRESET = iOC_IN | iOC_VENDOR | 12;
            var _false = new byte[] { 0, 0, 0, 0 };

            socket.IOControl(unchecked((int)sIO_UDP_CONNRESET), _false, null);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception setting IOControl");
            Console.WriteLine(e.ToString());
        }
    }

    public void Connect(IEndPoint endPoint)
    {
        _endpoint = (EndPointWrapper)endPoint;

        _socket = CreateSocket(_endpoint.Inner);
    }

    public void Close()
    {
        _socket.Close();
        _socket.Dispose();
    }

    /// <summary>
    /// Is message avaliable
    /// </summary>
    /// <returns>true if data to read</returns>
    public bool Poll()
    {
        return _socket.Poll(0, SelectMode.SelectRead);
    }

    public int Receive(byte[] buffer, out IEndPoint endPoint)
    {
        var c = _socket.ReceiveFrom(buffer, ref _endpoint.Inner);
        endPoint = _endpoint;
        return c;
    }

    public void Send(IEndPoint endPoint, byte[] packet, int length)
    {
        var netEndPoint = ((EndPointWrapper)endPoint).Inner;
        _socket.SendTo(packet, length, SocketFlags.None, netEndPoint);
    }
}
