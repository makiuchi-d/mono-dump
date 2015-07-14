using System;
using System.Collections.Generic;

class Namespace
{
	List<Element> elements = new List<Element>();

	public string Name
	{
		get; set;
	}

	public Namespace(string s)
	{
		Name = s;
	}

	public void Add(Type t)
	{
		elements.Add(Element.NewElement(t));
	}

	public void Dump()
	{
		Console.WriteLine("namespace " + Name);
		Console.WriteLine("{");
		foreach(Element e in elements)
		{
			foreach(string s in e.Dump())
			{
				Console.WriteLine("\t"+s);
			}
			Console.WriteLine("");
		}
		Console.WriteLine("}");
	}
}

