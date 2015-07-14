using System;
using System.Reflection;
using System.Collections.Generic;

class MonoDump
{
	static void Main()
	{
		string[] argv = Environment.GetCommandLineArgs();

		if(argv.Length<=1)
		{
			Console.WriteLine("Usage: {0} DLLNAME", argv[0]);
			return;
		}

		string dllname = argv[1];

		Assembly asm;
		try
		{
			asm = Assembly.LoadFrom(dllname);
		}
		catch(Exception e)
		{
			Console.WriteLine(e.Message);
			return;
		}

		Console.WriteLine("// {0}", asm);
		DumpAsm(asm);
	}

	static void DumpAsm(Assembly asm)
	{
		Dictionary<string,Namespace> nss = new Dictionary<string,Namespace>();

		Console.WriteLine("using System;");
		Console.WriteLine("using System.Collections;");
		Console.WriteLine("using System.Collections.Generic;");
		Console.WriteLine("");

		foreach(var t in asm.GetTypes())
		{
			if(!t.IsVisible || t.IsNested)
			{
				continue;
			}

			if(!nss.ContainsKey(t.Namespace))
			{
				nss[t.Namespace] = new Namespace(t.Namespace);
			}
			nss[t.Namespace].Add(t);
		}

		foreach(Namespace ns in nss.Values)
		{
			ns.Dump();
		}
	}

}

