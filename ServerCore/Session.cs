using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;
        //PacketSession를 상속해서 사용할때 OnRecv를 override를 막아놓는다
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;            

            while (true)
            {
                //최소한의 데이터는 왔는가(헤더)
                if (buffer.Count < HeaderSize)
                    break;

                //패킷이 전부 도착했는가?
                ushort dataSize =  BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                    break;

                //데이터가 왔다!
                //패킷크기만큼만 넘긴다
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset,dataSize));                

                processLen += dataSize;

                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }            

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        Socket _socket;        

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();        
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(65535);


        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();


        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnect(EndPoint endPoint);

        void Clear()
        {
            lock (_lock)
            {
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);            
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                    RegisterSend();                
            }
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if (sendBuffList.Count == 0)
                return;

            lock (_lock)
            {
                foreach(ArraySegment<byte> sendBuff in sendBuffList)
                    _sendQueue.Enqueue(sendBuff);

                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisconnect(_socket.RemoteEndPoint);

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();

            Clear();
        }

        void RegisterSend()
        {
            if (_disconnected == 1)
                return;

            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);                
            }

            _sendArgs.BufferList = _pendingList;


            try
            {
                bool pending = _socket.SendAsync(_sendArgs);
                if (pending == false)
                    OnSendCompleted(null, _sendArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Failed {e}");
            }
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)                        
                            RegisterSend();                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            if (_disconnected == 1)
                return;

            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false)
                    OnRecvCompleted(null, _recvArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterRecv Failed {e}");
            }
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            // 몇 byte를 받았는가
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    //Write 커서 이동
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        //버그체크
                        Disconnect();
                        return;
                    }

                    int processLen =  OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        //버그체크2
                        Disconnect();
                        return;
                    }

                    //Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        //버그체크3
                        Disconnect();
                        return;
                    }

                    //버그체크가 발생하지 않을 것 같지만 확인해봐야함.

                    RegisterRecv();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {ex}");
                }
            }
            else 
            {
                Disconnect();
            }
        }

    }
}
