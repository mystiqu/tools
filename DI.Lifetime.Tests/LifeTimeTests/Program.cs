using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace LifeTimeTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ThreadDisposeTest();
            
            //DILifeTimeTests();
        }

        private static async void ThreadDisposeTest()
        {
            AddMethodClass adder = new AddMethodClass();
            Calculator calc = new Calculator(adder);

            try
            {
                Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}] Starting");
                for (int i = 0; i < 100; i++)
                {
                    calc.Calculate(i, 1, 5);
                }
                
                Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}] Finished");

            } 
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                calc.Dispose();
                Console.WriteLine($"DISPOSED!!!");
            }

            //System.Environment.Exit(0);
            Console.WriteLine($"Total number of calls: {calc.Count}");
            Console.ReadKey();
        }

        private static void DILifeTimeTests()
        {
            OperationService bar1;
            OperationService bar2;
            OperationNested nestedOperation;
            Console.WriteLine("Hello World!");

            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddTransient<IOperationTransient, Operation>()
                .AddScoped<IOperationScoped, Operation>()
                .AddSingleton<IOperationSingleton, Operation>()
                .AddSingleton<OperationNested, OperationNested>()
                .AddSingleton<IOperationSingletonInstance>(new Operation(Guid.Empty))
                .AddTransient<OperationService, OperationService>()
                .BuildServiceProvider();

            bar1 = serviceProvider.GetService<OperationService>();

            Console.WriteLine("Scoped:          " + bar1.ScopedOperation.OperationId.ToString());
            Console.WriteLine("Transient:       " + bar1.TransientOperation.OperationId.ToString());
            Console.WriteLine("Singleton:       " + bar1.SingletonOperation.OperationId.ToString());
            Console.WriteLine("Singleton fixed: " + bar1.SingletonInstanceOperation.OperationId.ToString());
            Console.WriteLine();
            Console.ReadKey(true);

            bar2 = serviceProvider.GetService<OperationService>();
            Console.WriteLine("Scoped:          " + bar2.ScopedOperation.OperationId.ToString());
            Console.WriteLine("Transient:       " + bar2.TransientOperation.OperationId.ToString());
            Console.WriteLine("Singleton:       " + bar2.SingletonOperation.OperationId.ToString());
            Console.WriteLine("Singleton fixed: " + bar2.SingletonInstanceOperation.OperationId.ToString());
            Console.WriteLine();

            nestedOperation = serviceProvider.GetService<OperationNested>();
            Console.WriteLine("TESTING TESTIN");
            Console.WriteLine("Nested:          " + nestedOperation.OperationId.ToString());
            Console.ReadKey(true);

            nestedOperation = serviceProvider.GetService<OperationNested>();
            Console.WriteLine("Nested:          " + nestedOperation.OperationId.ToString());
            Console.ReadKey(true);

            nestedOperation = serviceProvider.GetService<OperationNested>();
            Console.WriteLine("Nested:          " + nestedOperation.OperationId.ToString());
            Console.ReadKey(true);

            nestedOperation = serviceProvider.GetService<OperationNested>();
            Console.WriteLine("Nested:          " + nestedOperation.OperationId.ToString());
            Console.ReadKey(true);

            nestedOperation = serviceProvider.GetService<OperationNested>();
            Console.WriteLine("Nested:          " + nestedOperation.OperationId.ToString());


            if (bar1.ScopedOperation.OperationId.Equals(bar2.ScopedOperation.OperationId))
                Console.WriteLine("Guid for scoped operation are IDENTICAL over requests and not changed.");

            if (!bar1.TransientOperation.OperationId.Equals(bar2.TransientOperation.OperationId))
                Console.WriteLine("Guid for transient operation are NOT IDENTICAL over requests");

            if (bar1.SingletonOperation.OperationId.Equals(bar2.SingletonOperation.OperationId))
                Console.WriteLine("Guid for singleton operation are IDENTICAL over timne");

            Console.ReadKey();
        }
        
    }

    internal class Calculator : IDisposable
    {
        AddMethodClass method;
        public int Count { get; set; }

        object lockObject = new object();
        public Calculator(AddMethodClass method)
        {
            this.method = method;   
        }

        public async Task<int> Calculate(int position, int a, int b)
        {
            lock (lockObject)
            {
                Count++;
            }
            
            return await method.Add(position, a, b);
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}] Disposing");
                    method.Dispose();
                    method = null;
                    Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}] Disposed");
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Calculator()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class AddMethodClass : IDisposable
    {
        Random r = new Random();
        public AddMethodClass()
        {

        }

        public async Task<int> Add(int position, int a, int b)
        {
            return await Task.Run(() => 
            {
                System.Threading.Thread.Sleep(r.Next(0, 1000));
                Console.WriteLine($" [{position.ToString().PadLeft(4, '0')}]");
                return a + b; 
            });
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Calculator()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
