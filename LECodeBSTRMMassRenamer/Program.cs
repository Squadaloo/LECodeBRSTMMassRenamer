using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LECodeBRSTMMassRenamer
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);
            IConfiguration config = builder.Build();
            FileProcessor fileProcessor = new FileProcessor(config);
            fileProcessor.RunProcessor();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
