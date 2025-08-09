using System;
using System.Runtime.InteropServices;
using OpcRcw.Comn;
using OpcRcw.Da;

namespace ComInteropOPC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OPC DA COM Interop Test ===");
            Console.WriteLine("Using COM Interop to read AO0_Speed directly\n");

            TestComInteropConnection();
        }

        static void TestComInteropConnection()
        {
            Console.WriteLine("Step 1: Testing COM Interop OPC Connection");
            Console.WriteLine("==========================================");

            try
            {
                // Method 1: ใช้ COM CLSID โดยตรง
                TestDirectCOMConnection();
                
                // Method 2: ใช้ ProgID
                TestProgIDConnection();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ COM Interop Test Failed: {ex.Message}");
            }
        }

        static void TestDirectCOMConnection()
        {
            Console.WriteLine("\nMethod 1: Direct COM CLSID Connection");
            Console.WriteLine("------------------------------------");

            try
            {
                // National Instruments OPC Server V5 CLSID (ถ้ามี)
                // ต้องหา CLSID จริงจาก registry
                
                Console.WriteLine("Searching for NI OPC Server CLSID in registry...");
                
                // ตรวจสอบ COM objects ที่มี
                string[] possibleCLSIDs = {
                    "{F8582CF2-88FB-11D0-B850-00C0F0104305}", // OPC Server example
                    "{13486D50-4821-11D2-A494-3CB306C10000}", // Another possible CLSID
                };

                foreach (string clsid in possibleCLSIDs)
                {
                    try
                    {
                        Console.WriteLine($"Testing CLSID: {clsid}");
                        
                        Type comType = Type.GetTypeFromCLSID(new Guid(clsid));
                        if (comType != null)
                        {
                            dynamic comObject = Activator.CreateInstance(comType);
                            Console.WriteLine($"✅ Successfully created COM object from CLSID: {clsid}");
                            
                            // ทดลองเรียกใช้ method
                            TryConnectAndRead(comObject, $"COM Object ({clsid})");
                            
                            return; // ถ้าสำเร็จ ออกจาก loop
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ CLSID {clsid}: {ex.Message}");
                    }
                }
                
                Console.WriteLine("❌ No working CLSID found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Direct COM method failed: {ex.Message}");
            }
        }

        static void TestProgIDConnection()
        {
            Console.WriteLine("\nMethod 2: ProgID Connection");
            Console.WriteLine("---------------------------");

            string[] progIDs = {
                "National Instruments.NIOPCServer.V5",
                "NationalInstruments.NIOPCServer",
                "NIOP.Server",
                "OPC.Server.1",
                "Matrikon.OPC.Server",
            };

            foreach (string progID in progIDs)
            {
                try
                {
                    Console.WriteLine($"Testing ProgID: {progID}");
                    
                    // ใช้ Marshal.GetActiveObject แทน CreateObject
                    try
                    {
                        dynamic activeObject = Marshal.GetActiveObject(progID);
                        Console.WriteLine($"✅ Found active object: {progID}");
                        TryConnectAndRead(activeObject, progID);
                        return;
                    }
                    catch (COMException)
                    {
                        // ถ้าไม่มี active object ลองสร้างใหม่
                        Type progType = Type.GetTypeFromProgID(progID);
                        if (progType != null)
                        {
                            dynamic newObject = Activator.CreateInstance(progType);
                            Console.WriteLine($"✅ Created new object: {progID}");
                            TryConnectAndRead(newObject, progID);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ProgID {progID}: {ex.Message}");
                }
            }
        }

        static void TryConnectAndRead(dynamic opcObject, string objectName)
        {
            try
            {
                Console.WriteLine($"   → Attempting to connect via {objectName}...");
                
                // ทดลองเรียกใช้ method ต่างๆ ที่อาจมี
                TryMethod(() => opcObject.Connect(""), "Connect()");
                TryMethod(() => opcObject.GetGroups(), "GetGroups()");
                TryMethod(() => opcObject.CreateGroup("TestGroup"), "CreateGroup()");
                
                // ถ้าเป็น OPC Server แบบ standard
                if (HasProperty(opcObject, "OPCGroups"))
                {
                    Console.WriteLine("   → Found OPCGroups property - this looks like an OPC Server!");
                    
                    dynamic groups = opcObject.OPCGroups;
                    dynamic group = groups.Add("RealDataGroup");
                    group.UpdateRate = 1000;
                    group.IsActive = true;
                    
                    dynamic items = group.OPCItems;
                    items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                    items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                    
                    object[] values = new object[2];
                    object[] errors = new object[2];
                    int[] handles = { 1, 2 };
                    
                    group.SyncRead((short)1, 2, handles, out values, out errors);
                    
                    Console.WriteLine($"   🎯 SUCCESS! AO0_Speed: {values[0]} RPM");
                    Console.WriteLine($"   ⚡ AO1_Torque: {values[1]} Nm");
                    
                    opcObject.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Connection attempt failed: {ex.Message}");
            }
        }

        static void TryMethod(Action method, string methodName)
        {
            try
            {
                method();
                Console.WriteLine($"   ✅ {methodName} - Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ {methodName} - {ex.Message}");
            }
        }

        static bool HasProperty(dynamic obj, string propertyName)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName);
                return prop != null;
            }
            catch
            {
                return false;
            }
        }
    }
}