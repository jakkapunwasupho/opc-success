using System;
using System.Net.Sockets;

namespace RealOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Real NI OPC Server Connection Test ===");
            Console.WriteLine("Testing multiple connection methods to get AO0_Speed data\n");

            // ทดสอบหลายวิธี
            TestMethod1_DirectOPCDA();
            TestMethod2_TCPConnection();
            TestMethod3_OPCAutomation();
        }

        static void TestMethod1_DirectOPCDA()
        {
            Console.WriteLine("Method 1: Direct OPC DA Connection");
            Console.WriteLine("=======================================");
            
            try
            {
                Type opcServerType = Type.GetTypeFromProgID("OPC.Automation.1");
                if (opcServerType == null)
                {
                    Console.WriteLine("❌ OPC Automation not available");
                    Console.WriteLine("Need to install: OPC Core Components");
                    return;
                }

                dynamic opcServer = Activator.CreateInstance(opcServerType);
                opcServer.Connect("National Instruments.NIOPCServer.V5");
                
                Console.WriteLine("✅ Connected to NI OPC Server");
                
                // สร้าง Group และ Items
                dynamic opcGroup = opcServer.OPCGroups.Add("TestGroup");
                opcGroup.UpdateRate = 1000;
                opcGroup.IsActive = true;
                
                dynamic opcItems = opcGroup.OPCItems;
                opcItems.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                opcItems.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                
                // อ่านข้อมูล
                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };
                
                opcGroup.SyncRead((short)1, 2, handles, out values, out errors);
                
                Console.WriteLine($"📊 AO0_Speed: {values[0]} RPM");
                Console.WriteLine($"📊 AO1_Torque: {values[1]} Nm");
                
                opcServer.Disconnect();
                Console.WriteLine("✅ Successfully read real OPC data!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Method 1 Failed: {ex.Message}\n");
            }
        }

        static void TestMethod2_TCPConnection()
        {
            Console.WriteLine("Method 2: TCP Connection to Port 32405");
            Console.WriteLine("=====================================");
            
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;
                    
                    Console.WriteLine("Connecting to localhost:32405...");
                    client.Connect("localhost", 32405);
                    
                    Console.WriteLine("✅ TCP Connection successful!");
                    Console.WriteLine("Server is listening on port 32405");
                    
                    var stream = client.GetStream();
                    
                    // ส่ง OPC handshake (simplified)
                    byte[] handshake = { 0x01, 0x02, 0x03, 0x04 };
                    stream.Write(handshake, 0, handshake.Length);
                    
                    // อ่าน response
                    byte[] response = new byte[1024];
                    int bytesRead = stream.Read(response, 0, response.Length);
                    
                    Console.WriteLine($"📡 Received {bytesRead} bytes from server");
                    Console.WriteLine("✅ Basic communication established\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Method 2 Failed: {ex.Message}\n");
            }
        }

        static void TestMethod3_OPCAutomation()
        {
            Console.WriteLine("Method 3: Alternative OPC Server Names");
            Console.WriteLine("=====================================");
            
            string[] serverNames = {
                "National Instruments.NIOPCServer.V5",
                "NationalInstruments.NIOPCServer",
                "NI.OPCServer",
                "OPC.Server"
            };

            foreach (string serverName in serverNames)
            {
                try
                {
                    Console.WriteLine($"Trying: {serverName}");
                    
                    Type opcServerType = Type.GetTypeFromProgID("OPC.Automation.1");
                    if (opcServerType != null)
                    {
                        dynamic opcServer = Activator.CreateInstance(opcServerType);
                        opcServer.Connect(serverName);
                        Console.WriteLine($"✅ Connected to {serverName}");
                        opcServer.Disconnect();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {serverName}: {ex.Message}");
                }
            }
        }
    }
}