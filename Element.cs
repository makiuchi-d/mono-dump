using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class Element
{
	protected Type self;
	protected List<Element> childs = new List<Element>();

	public static Element NewElement(Type t)
	{
		if(t.IsClass)
		{
			return new ClassElement(t);
		}
		if(t.IsEnum)
		{
			return new EnumElement(t);
		}
		if(t.IsInterface)
		{
			return new InterfaceElement(t);
		}
		return new Element(t);
	}

	public Element(Type t)
	{
		self = t;
		foreach(Type s in t.GetNestedTypes())
		{
			childs.Add(NewElement(s));
		}
	}

	public virtual List<string> Dump()
	{
		Console.WriteLine("// WARNING: {0}",self.FullName);
		return new List<string>();
	}

	protected void DumpChilds(ref List<string> s)
	{
		foreach(Element e in childs)
		{
			List<string> es = e.Dump();
			foreach(string ss in es)
			{
				s.Add("\t" + ss);
			}
			s.Add("");
		}
	}

	protected virtual string Modifires()
	{
		string s = "";
		if(self.IsPublic || self.IsNestedPublic)
		{
			s += "public ";
		}
		if(self.IsNestedFamily)
		{
			s += "protected ";
		}
		if(self.IsAbstract)
		{
			s += "abstract ";
		}
		return s;
	}

	protected string TypeNameString(Type t)
	{
		string n = t.FullName;
		if(t.IsNested)
		{
			return TypeNameString(t.DeclaringType)+"."+t.Name;
		}

		if(t.Namespace == "System"
		   || t.Namespace == "System.Collections"
		   || t.Namespace == "System.Collections.Generic"
		   || t.Namespace == self.Namespace)
		{
			n = t.Name;
		}
		if(t.IsGenericType)
		{
			int pos = n.IndexOf("`");
			if(pos>0)
			{
				n = n.Substring(0, n.IndexOf("`"));
				List<string> arg = new List<string>();
				foreach(var gt in t.GetGenericArguments())
				{
					arg.Add(TypeNameString(gt));
				}
				n += "<" + String.Join(", ", arg.ToArray()) + ">";
			}
		}
		return n;
	}

	protected string SafeName(string name)
	{

		switch(name)
		{
			case "abstract":
			case "as":
			case "async":
			case "await":
			case "base":
			case "bool":
			case "break":
			case "byte":
			case "case":
			case "catch":
			case "char":
			case "checked":
			case "class":
			case "const":
			case "continue":
			case "decimal":
			case "default":
			case "delegate":
			case "do":
			case "double":
			case "else":
			case "enum":
			case "event":
			case "explicity":
			case "extern":
			case "false":
			case "finally":
			case "fixed":
			case "float":
			case "for":
			case "foreach":
			case "goto":
			case "if":
			case "implicit":
			case "in":
			case "int":
			case "interface":
			case "internal":
			case "is":
			case "lock":
			case "long":
			case "namespace":
			case "new":
			case "null":
			case "object":
			case "operator":
			case "out":
			case "override":
			case "params":
			case "private":
			case "protected":
			case "public":
			case "readonly":
			case "ref":
			case "return":
			case "sbyte":
			case "sealed":
			case "short":
			case "sizeof":
			case "stackalloc":
			case "static":
			case "string":
			case "struct":
			case "switch":
			case "this":
			case "throw":
			case "true":
			case "try":
			case "typeof":
			case "uint":
			case "ulong":
			case "unchecked":
			case "unsafe":
			case "ushort":
			case "using":
			case "virtual":
			case "volatile":
			case "void":
			case "while":
				return "@" + name;
		}
		return name;
	}
}

/*
 * Enum Element
 */
class EnumElement : Element
{
	public EnumElement(Type t) : base(t){}

	public override List<string> Dump()
	{
		List<string> s = new List<string>();
		s.Add(Modifires() + "enum " + SafeName(self.Name) + BaseType());
		s.Add("{");

		Type bt = Enum.GetUnderlyingType(self);

		foreach(string n in Enum.GetNames(self))
		{
			var v = Convert.ChangeType(Enum.Parse(self, n), bt);
			if(v is string)
			{
				v = "\"" + v + "\"";
			}
			s.Add("\t" + n + " = " + v + ",");
		}
		s.Add("}");
		return s;
	}

	string BaseType()
	{
		TypeCode tc = Type.GetTypeCode(self);
		switch(tc)
		{
			case TypeCode.Byte:
				return " : byte";
			case TypeCode.SByte:
				return " : sbyte";
			case TypeCode.Int16:
				return " : short";
			case TypeCode.UInt16:
				return " : ushort";
			case TypeCode.UInt32:
				return " : uint";
			case TypeCode.Int64:
				return " : long";
			case TypeCode.UInt64:
				return " : ulong";
			default:
				return "";
		}
	}
}

/*
 * Class Element
 */
class ClassElement : Element
{
	public ClassElement(Type t) : base(t){}

	public override List<string> Dump()
	{
		List<string> s = new List<string>();

		s.Add(Modifires() + "class " + SafeName(self.Name) + BaseTypes());
		s.Add("{");

		DumpChilds(ref s);
		DumpFields(ref s);
		DumpProperties(ref s);
		DumpConstructors(ref s);
		DumpMethods(ref s);

		s.Add("}");
		return s;
	}

	protected string BaseTypes()
	{
		List<string> types = new List<string>();
		if(self.BaseType != null
		   && self.BaseType != typeof(Object)
		   && self.BaseType != typeof(MulticastDelegate))
		{
			types.Add(TypeNameString(self.BaseType));
		}

		foreach(Type i in self.GetInterfaces())
		{
			if(i.DeclaringType == self)
			{
				types.Add(TypeNameString(i));
			}
		}
		if(types.Count==0)
		{
			return "";
		}
		return " : " + String.Join(", ", types.ToArray());
	}

	void DumpFields(ref List<string> s)
	{
		DumpFields(ref s, BindingFlags.Instance|BindingFlags.Public);
		DumpFields(ref s, BindingFlags.Instance|BindingFlags.NonPublic);
		DumpFields(ref s, BindingFlags.Static|BindingFlags.Public);
		DumpFields(ref s, BindingFlags.Static|BindingFlags.NonPublic);
	}

	void DumpFields(ref List<string> s, BindingFlags binding)
	{
		foreach(FieldInfo f in self.GetFields(binding))
		{
			if(f.DeclaringType != self)
			{
				continue;
			}

			FieldAttributes attr = f.Attributes;
			if(ContainAttr(attr, FieldAttributes.Private))
			{
				continue;
			}

			string ss = "\t" + AccessorString(attr) + " ";

			if(ContainAttr(attr, FieldAttributes.Static)
			   && !ContainAttr(attr, FieldAttributes.Literal))
			{
				ss += "static ";
			}
			if(ContainAttr(attr, FieldAttributes.InitOnly))
			{
				ss += "readonly ";
			}
			if(ContainAttr(attr, FieldAttributes.Literal))
			{
				ss += "const ";
			}

			ss += TypeNameString(f.FieldType) + " " + SafeName(f.Name);

			if(ContainAttr(attr, FieldAttributes.HasDefault))
			{
				ss += " = " + DefaultValueString(f.FieldType);
			}
			s.Add(ss + ";");
		}
	}

	void DumpProperties(ref List<string> s)
	{
		DumpProperties(ref s, BindingFlags.Instance|BindingFlags.Public);
		DumpProperties(ref s, BindingFlags.Instance|BindingFlags.NonPublic);
		DumpProperties(ref s, BindingFlags.Static|BindingFlags.Public);
		DumpProperties(ref s, BindingFlags.Static|BindingFlags.NonPublic);
	}

	void DumpProperties(ref List<string> s, BindingFlags binding)
	{
		foreach(PropertyInfo p in self.GetProperties(binding))
		{
			if(p.DeclaringType != self)
			{
				continue;
			}

			MethodInfo getmi = p.GetGetMethod(true);
			MethodInfo setmi = p.GetSetMethod(true);
			MethodInfo mi = (getmi!=null)? getmi: setmi;
			if(!mi.IsPublic && !mi.IsFamily)
			{
				continue;
			}

			string ss = "\t";
			ss += AccessorString(mi) + " ";
			if(mi.IsStatic)
			{
				ss += "static ";
			}
			ss += TypeNameString(p.PropertyType) + " " + SafeName(p.Name) + " { ";

			if(p.CanRead)
			{
				ss += "get { return "+DefaultValueString(p.PropertyType)+"; } ";
			}
			if(p.CanWrite)
			{
				string ac = AccessorString(setmi);
				if(ac!="public")
				{
					ss += ac + " ";
				}
				ss += "set {} ";
			}
			ss += "}";
			s.Add(ss);
		}
	}

	void DumpConstructors(ref List<string> s)
	{
		DumpConstructors(ref s, BindingFlags.Instance|BindingFlags.Public);
	}
	void DumpConstructors(ref List<string> s, BindingFlags binding)
	{
		foreach(ConstructorInfo ci in self.GetConstructors(binding))
		{
			if(ci.DeclaringType != self)
			{
				continue;
			}
			string ss = "\t" + MethodModifires(ci);
			string clsname = self.Name;
			if(clsname.IndexOf("`")>0)
			{
				clsname = clsname.Substring(0,clsname.IndexOf("`"));
			}
			ss += clsname;

			string[] prm = MethodParams(ci);
			ss += "(" + String.Join(", ", prm) + ")" + BaseConstructor(ci);
			ss += "{}";
			s.Add(ss);
		}
	}
	string BaseConstructor(ConstructorInfo ci)
	{
		ParameterInfo[] pis = ci.GetParameters();
		if(pis.Length==0)
		{
			return "";
		}

		if(self.BaseType == typeof(object)
		   || self.BaseType == typeof(MulticastDelegate))
		{
			return "";
		}

		foreach(ConstructorInfo bci in self.BaseType.GetConstructors())
		{
			ParameterInfo[] bpi = bci.GetParameters();
			if(bpi.Length==0)
			{
				return "";
			}
		}

		// fixme: ベースクラスのコンストラクタ引数が違ったら死ぬ
		List<string> pnames = new List<string>();
		foreach(ParameterInfo pi in pis)
		{
			pnames.Add(SafeName(pi.Name));
		}
		return ":base(" + String.Join(", ", pnames.ToArray()) + ")";
	}

	void DumpMethods(ref List<string> s)
	{
		DumpMethods(ref s, BindingFlags.Instance|BindingFlags.Public);
		DumpMethods(ref s, BindingFlags.Instance|BindingFlags.NonPublic);
		DumpMethods(ref s, BindingFlags.Static|BindingFlags.Public);
		DumpMethods(ref s, BindingFlags.Static|BindingFlags.NonPublic);
	}

	void DumpMethods(ref List<string> s, BindingFlags binding)
	{
		foreach(MethodInfo mi in self.GetMethods(binding))
		{
			if(mi.DeclaringType != self)
			{
				continue;
			}
			if(mi.IsPrivate || mi.IsAssembly)
			{
				continue;
			}
			if(ContainAttr(mi.Attributes, MethodAttributes.SpecialName))
			{
				continue;
			}

			string ss = "\t" + MethodModifires(mi);
			ss += TypeNameString(mi.ReturnType) + " ";

			string[] prm = MethodParams(mi);
			ss += SafeName(mi.Name) + "(" + String.Join(", ", prm) + ")";
			ss += MethodBody(mi);

			s.Add(ss);
		}
	}


	bool ContainAttr(FieldAttributes attr, FieldAttributes target)
	{
		return ((attr&target)==target);
	}
	bool ContainAttr(PropertyAttributes attr, PropertyAttributes target)
	{
		return ((attr&target)==target);
	}
	bool ContainAttr(MethodAttributes attr, MethodAttributes target)
	{
		return ((attr&target)==target);
	}

	string AccessorString(FieldAttributes attr)
	{
		if(ContainAttr(attr, FieldAttributes.Public))
		{
			return "public";
		}
		else if(ContainAttr(attr, FieldAttributes.Private))
		{
			return "private";
		}
		else
		{
			return "protected";
		}
	}
	string AccessorString(MethodBase mi)
	{
		if(mi.IsPrivate)
		{
			return "private";
		}
		if(mi.IsFamily)
		{
			return "protected";
		}
		if(mi.IsAssembly)
		{
			return "internal";
		}
		if(mi.IsFamilyOrAssembly)
		{
			return "protected internal";
		}
		return "public";
	}

	string MethodModifires(MethodBase mi)
	{
		string ss = AccessorString(mi) + " ";
		if(mi.IsAbstract)
		{
			ss += "abstract ";
		}
		if(mi.IsStatic)
		{
			ss += "static ";
		}

		if(mi.IsVirtual
		   && !mi.IsAbstract
		   && !mi.IsSpecialName
		   && !mi.IsFinal)
		{
			if(ContainAttr(mi.Attributes, MethodAttributes.VtableLayoutMask))
			{
				ss += "virtual ";
			}
			else
			{
				ss += "override ";
			}
		}

		return ss;
	}

	protected string[] MethodParams(MethodBase mi)
	{
		List<string> a = new List<string>();
		foreach(ParameterInfo pi in mi.GetParameters())
		{
			string s = "";
			string t = TypeNameString(pi.ParameterType);

			if(pi.IsOut)
			{
				s += "out ";
			}
			if(t.IndexOf("&")>=0)
			{
				if(!pi.IsOut)
				{
					s += "ref ";
				}
				t = t.Substring(0,t.IndexOf("&"));
			}
			s += t + " " + SafeName(pi.Name);

			if(pi.IsOptional)
			{
				s += " = ";
				if(pi.DefaultValue is string)
				{
					s += "\"" + ((string)pi.DefaultValue) + "\"";
				}
				else
				{
					s += pi.DefaultValue.ToString();
				}
			}
			a.Add(s);
		}
		return a.ToArray();
	}

	string MethodBody(MethodInfo mi)
	{
		if(mi.IsAbstract)
		{
			return ";";
		}
		string body = "{ ";

		foreach(ParameterInfo pi in mi.GetParameters())
		{
			if(pi.IsOut)
			{
				body += pi.Name + "=" + DefaultValueString(pi.ParameterType.GetElementType())+"; ";
			}
		}

		if(mi.ReturnType != typeof(void))
		{
			body += "return " + DefaultValueString(mi.ReturnType) + "; ";
		}
		body += "}";
		return body;
	}

	string DefaultValueString(Type t)
	{
		if(t == typeof(Boolean))
		{
			return "false";
		}
		if(t == typeof(Byte)
		   || t == typeof(SByte)
		   || t == typeof(Int16)
		   || t == typeof(Int32)
		   || t == typeof(Int64)
		   || t == typeof(UInt16)
		   || t == typeof(UInt32)
		   || t == typeof(UInt64))
		{
			return "0";
		}
		if(t == typeof(Char))
		{
			return "'\\0'";
		}
		if(t == typeof(Double))
		{
			return "0.0";
		}
		if(t == typeof(Single))
		{
			return "0.0f";
		}
		if(t == typeof(String))
		{
			return "\"\"";
		}
		if(t.IsEnum)
		{
			string[] names = Enum.GetNames(t);
			return TypeNameString(t) + "." + names[0];
		}
		if(t == typeof(DictionaryEntry))
		{
			return "new DictionaryEntry()";
		}
		return "null";
	}
}

/*
 * Interface Element
 */
class InterfaceElement : ClassElement
{
	public InterfaceElement(Type t) : base(t){}

	public override List<string> Dump()
	{
		List<string> s = new List<string>();

		s.Add(Modifires() + "interface " + SafeName(self.Name) + BaseTypes());
		s.Add("{");

		DumpProperties(ref s);
		DumpMethods(ref s);

		s.Add("}");
		return s;
	}

	protected override string Modifires()
	{
		string s = "";
		if(self.IsPublic)
		{
			s += "public ";
		}
		return s;
	}

	void DumpProperties(ref List<string> s)
	{
		foreach(PropertyInfo p in self.GetProperties())
		{
			s.Add("prop: "+p.Name);
		}
	}


	void DumpMethods(ref List<string> s)
	{
		foreach(MethodInfo mi in self.GetMethods())
		{
			if(mi.DeclaringType != self)
			{
				continue;
			}
			string ss = "\t" + TypeNameString(mi.ReturnType) + " ";
			string[] prm = MethodParams(mi);
			ss += SafeName(mi.Name) + "(" + String.Join(", ", prm) + ");";
			s.Add(ss);
		}
	}

}
