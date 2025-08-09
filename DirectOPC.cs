using System;
using System.Net.Sockets;
using System.Text;

namespace DirectOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Direct OPC Connection Test ===");
            Console.WriteLine("Bypassing OPC.Automation.1 - Using direct TCP connection\n");

            // ทดสอบการเชื่อมต่อโดยตรงกับ NI OPC Server
            TestDirectTCPConnection();
            TestAlternativeOPCMethods();
        }

        static void TestDirectTCPConnection()
        {
            Console.WriteLine("Method 1: Direct TCP Connection to Port 32405");
            Console.WriteLine("=============================================");
            
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;
                    
                    Console.WriteLine("Attempting to connect to localhost:32405...");
                    client.Connect("localhost", 32405);
                    
                    Console.WriteLine("✅ TCP Connection established!");
                    Console.WriteLine("NI OPC Server is running and listening on port 32405");
                    
                    var stream = client.GetStream();
                    
                    // ส่งคำขอข้อมูลพื้นฐาน
                    string request = "GET_DATA:ADAM5000TCP.ADAM-5024.AO0_Speed\n";
                    byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                    
                    Console.WriteLine($"Sending request: {request.Trim()}");
                    stream.Write(requestBytes, 0, requestBytes.Length);
                    
                    // อ่าน response
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"📡 Server Response ({bytesRead} bytes): {response}");
                        
                        // พยายามแปลงเป็นค่า AO0_Speed
                        if (TryParseSpeedData(response, out double speedValue))
                        {
                            Console.WriteLine($"🎯 AO0_Speed: {speedValue} RPM");
                        }
                    }
                    else
                    {
                        Console.WriteLine("📡 No response from server");
                    }
                }
            }
            catch (SocketException sockEx)
            {
                Console.WriteLine($"❌ TCP Connection Failed: {sockEx.Message}");
                if (sockEx.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    Console.WriteLine("   🔍 Server is not listening on port 32405");
                    Console.WriteLine("   💡 Check if NI OPC Server is running");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static void TestAlternativeOPCMethods()
        {
            Console.WriteLine("Method 2: Alternative OPC Connection Methods");
            Console.WriteLine("===========================================");
            
            // ทดสอบ COM objects อื่นๆ ที่อาจมีอยู่
            string[] comObjects = {
                "OPC.Automation",
                "Matrikon.OPC.Automation.1",
                "Graybox.OPC.DAAutoClient.1",
                "OPCEXPERT.OPCExpertApplication.1",
                "National_Instruments.NI_OPC_Server.V5"
            };

            foreach (string comObject in comObjects)
            {
                try
                {
                    Console.WriteLine($"Testing: {comObject}");
                    Type opcType = Type.GetTypeFromProgID(comObject);
                    
                    if (opcType != null)
                    {
                        Console.WriteLine($"✅ Found: {comObject}");
                        
                        // ทดสอบการสร้าง object
                        dynamic obj = Activator.CreateInstance(opcType);
                        Console.WriteLine($"✅ Created instance of {comObject}");
                        
                        // พยายามเชื่อมต่อ
                        try
                        {
                            obj.Connect("National Instruments.NIOPCServer.V5");
                            Console.WriteLine($"🎯 Successfully connected via {comObject}!");
                            
                            // ลองอ่านข้อมูล
                            ReadDataViaComObject(obj);
                            
                            obj.Disconnect();
                        }
                        catch (Exception connectEx)
                        {
                            Console.WriteLine($"   ❌ Connection failed: {connectEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Not found: {comObject}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error with {comObject}: {ex.Message}");
                }
            }
        }

        static void ReadDataViaComObject(dynamic opcServer)
        {
            try
            {
                // สร้าง group
                dynamic group = opcServer.OPCGroups.Add("TestGroup");
                group.UpdateRate = 1000;
                group.IsActive = true;
                
                // เพิ่ม items
                dynamic items = group.OPCItems;
                items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                
                // อ่านข้อมูล
                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };
                
                group.SyncRead((short)1, 2, handles, out values, out errors);
                
                Console.WriteLine($"   📊 AO0_Speed: {values[0]} RPM");
                Console.WriteLine($"   ⚡ AO1_Torque: {values[1]} Nm");
                Console.WriteLine($"   ❗ Errors: [{errors[0]}, {errors[1]}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Data read failed: {ex.Message}");
            }
        }

        static bool TryParseSpeedData(string response, out double speedValue)
        {
            speedValue = 0;
            
            try
            {
                // ลองแปลง response เป็นตัวเลข
                if (double.TryParse(response.Trim(), out speedValue))
                {
                    return true;
                }
                
                // ลองหาตัวเลขใน response
                var numbers = System.Text.RegularExpressions.Regex.Matches(response, @"-?\d+\.?\d*");
                if (numbers.Count > 0 && double.TryParse(numbers[0].Value, out speedValue))
                {
                    return true;
                }
            }
            catch
            {
                // ignore parse errors
            }
            
            return false;
        }
    }
}