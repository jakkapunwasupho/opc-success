using System;

namespace TestOPC
{
    class TestOPCWithMock
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Testing OPC Connection with Mock Server ===");
            Console.WriteLine();

            TestMockOPCConnection();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void TestMockOPCConnection()
        {
            try
            {
                Console.WriteLine("1. Creating Mock OPC Server...");
                var mockServer = new MockOPCServer();

                Console.WriteLine("2. Connecting to Mock Server...");
                mockServer.Connect("National Instruments.NIOPCServer.V5");

                Console.WriteLine("3. Creating OPC Group...");
                var opcGroup = mockServer.OPCGroups.Add("TestGroup");
                opcGroup.UpdateRate = 1000;
                opcGroup.IsActive = true;
                opcGroup.IsSubscribed = true;

                Console.WriteLine("4. Adding OPC Items...");
                var item1 = opcGroup.OPCItems.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                var item2 = opcGroup.OPCItems.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);

                Console.WriteLine("5. Reading OPC Data...");
                object[] values;
                object[] errors;
                int[] handles = { 1, 2 };

                opcGroup.SyncRead(1, 2, handles, out values, out errors);

                Console.WriteLine("6. Displaying Results:");
                Console.WriteLine($"   AO0_Speed: {values[0]}");
                Console.WriteLine($"   AO1_Torque: {values[1]}");

                Console.WriteLine("7. Disconnecting...");
                mockServer.Disconnect();

                Console.WriteLine("\n✓ Mock OPC test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error during mock test: {ex.Message}");
            }
        }
    }
}