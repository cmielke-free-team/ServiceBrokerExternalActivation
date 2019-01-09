using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EmdatSSBEAService
{
	public static class XElementExtensions
	{
		public static Int32? AsInt32(this XElement xelement)
		{
			if (xelement == null) return null;
			if (string.IsNullOrEmpty(xelement.Value)) return null;
			return int.Parse((string)xelement);
		}

		public static Int16? AsInt16(this XElement xelement)
		{
			if (xelement == null) return null;
			if (string.IsNullOrEmpty(xelement.Value)) return null;
			return Int16.Parse((string)xelement);
		}

		public static string AsString(this XElement xelement)
		{
			if (xelement == null) return null;
			return (string)xelement;
		}

		public static DateTime? AsDateTime(this XElement xelement)
		{
			if (xelement == null) return null;
			if (string.IsNullOrEmpty(xelement.Value)) return null;
			return DateTime.Parse((string)xelement);
		}

		public static DateTimeOffset? AsDateTimeOffset(this XElement xelement)
		{
			if (xelement == null) return null;
			if (string.IsNullOrEmpty(xelement.Value)) return null;
			return DateTimeOffset.Parse((string)xelement);
		}

		public static Boolean? AsBoolean(this XElement xelement)
		{
			if (xelement == null) return null;
			if (string.IsNullOrEmpty(xelement.Value)) return null;
			return Boolean.Parse((string)xelement);
		}

		public static XElement LoadFromBytes(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				return XElement.Load(ms);
			}
		}
	}

}
