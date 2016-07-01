using Constellation;
using Constellation.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackConnector
{
    public class Program : PackageBase
    {
        static void Main(string[] args)
        {
            PackageHost.Start<Program>(args);
        }

        [MessageCallback(IsHidden = true)]
        void SOModifier(string name, object value)
        {
            PackageHost.PushStateObject(name, value);
        }
    }
}
