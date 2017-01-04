using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using SharpYaml.Serialization;

namespace GameLogWatcher
{
	class DynamicYaml : DynamicObject
	{
		public YamlNode Node { get; }

		public DynamicYaml(YamlNode node) =>
			Node = node;

		public static DynamicYaml Parse(string yaml)
		{
			using (var sr = new StringReader(yaml))
			{
				var stream = new YamlStream();

				stream.Load(sr);

				return new DynamicYaml(stream.First().RootNode);
			}
		}

		public override bool TryConvert(ConvertBinder binder, out object result) =>
			TryConvert(binder.Type, Node, out result);

		static bool TryConvert(Type targetType, YamlNode node, out object result)
		{
			if (node is null)
				return SetResult(out result, null);
			if (targetType == typeof(object) ||
				targetType == typeof(DynamicYaml))
				return SetResult(out result, new DynamicYaml(node));
			if (targetType == typeof(string))
				return SetResult(out result, node.ToString());
			if (node is YamlScalarNode scalar)
				return TryConvertScalar(targetType, scalar, out result);

			if (node is YamlSequenceNode seq)
				if (targetType.IsArray ||
					targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					var elementType = targetType.IsArray ? targetType.GetElementType() : targetType.GetGenericArguments()[0];
					var rt = Array.CreateInstance(elementType, seq.Children.Count);

					for (var i = 0; i < rt.Length; i++)
						if (TryConvert(elementType, seq.Children[i], out var element))
							rt.SetValue(element, i);
						else
						{
							result = null;

							return false;
						}

					return SetResult(out result, rt);
				}
				else if (targetType == typeof(IEnumerable))
					return SetResult(out result, seq.Select(i => new DynamicYaml(i)));

			if (node is YamlMappingNode map)
				if (targetType == typeof(IEnumerable) ||
					targetType == typeof(IEnumerable<KeyValuePair<string, DynamicYaml>>) ||
					targetType == typeof(Dictionary<string, DynamicYaml>) ||
					targetType == typeof(IDictionary<string, DynamicYaml>))
					return SetResult(out result, map.ToDictionary(i => (string)(YamlScalarNode)i.Key, i => new DynamicYaml(i.Value)));

			result = null;

			return false;
		}

		static bool TryConvertScalar(Type targetType, YamlScalarNode scalar, out object result)
		{
			switch (Type.GetTypeCode(targetType))
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					return SetResult(out result, null);
				case TypeCode.Boolean:
					return SetResult(out result, bool.Parse(scalar.Value));
				case TypeCode.Char:
					return SetResult(out result, char.Parse(scalar.Value));
				case TypeCode.SByte:
					return SetResult(out result, sbyte.Parse(scalar.Value));
				case TypeCode.Byte:
					return SetResult(out result, byte.Parse(scalar.Value));
				case TypeCode.Int16:
					return SetResult(out result, short.Parse(scalar.Value));
				case TypeCode.UInt16:
					return SetResult(out result, ushort.Parse(scalar.Value));
				case TypeCode.Int32:
					return SetResult(out result, int.Parse(scalar.Value));
				case TypeCode.UInt32:
					return SetResult(out result, uint.Parse(scalar.Value));
				case TypeCode.Int64:
					return SetResult(out result, long.Parse(scalar.Value));
				case TypeCode.UInt64:
					return SetResult(out result, ulong.Parse(scalar.Value));
				case TypeCode.Single:
					return SetResult(out result, float.Parse(scalar.Value));
				case TypeCode.Double:
					return SetResult(out result, double.Parse(scalar.Value));
				case TypeCode.Decimal:
					return SetResult(out result, decimal.Parse(scalar.Value));
				case TypeCode.DateTime:
					return SetResult(out result, DateTime.Parse(scalar.Value));
				case TypeCode.String:
					return SetResult(out result, scalar.Value);
				default:
					if (targetType == typeof(DynamicYaml) ||
						targetType == typeof(object))
						return SetResult(out result, new DynamicYaml(scalar));

					result = null;

					return false;
			}
		}

		public override IEnumerable<string> GetDynamicMemberNames()
		{
			if (Node is YamlSequenceNode seq)
				return Enumerable.Range(0, seq.Children.Count).Select(i => $"[{i}]");
			if (Node is YamlMappingNode map)
				return map.Select(i => (string)(YamlScalarNode)i.Key);

			return base.GetDynamicMemberNames();
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			if (indexes.Length == 1 &&
				Node is YamlSequenceNode seq)
				return SetResult(out result, new DynamicYaml(seq.Children[(int)indexes[0]]));
			if (indexes.Length == 1 &&
				Node is YamlMappingNode map)
			{
				var key = (YamlScalarNode)(string)indexes[0];

				return SetResult(out result, map.Children.ContainsKey(key) ? new DynamicYaml(map.Children[key]) : null);
			}

			result = null;

			return false;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (Node is YamlSequenceNode seq)
				return SetResult(out result, new DynamicYaml(seq.Children[int.Parse(binder.Name.Trim('[', ']'))]));
			if (Node is YamlMappingNode map)
			{
				var key = (YamlScalarNode)binder.Name;

				return SetResult(out result, map.Children.ContainsKey(key) ? new DynamicYaml(map.Children[key]) : null);
			}

			result = null;

			return false;
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			if (!args.Any() &&
				Node is YamlMappingNode map)
			{
				var key = (YamlScalarNode)binder.Name;

				return SetResult(out result, map.Children.ContainsKey(key));
			}

			result = null;

			return false;
		}

		public override string ToString() =>
			Node.ToString();

		static bool SetResult(out object result, object obj)
		{
			result = obj;

			return true;
		}
	}
}
